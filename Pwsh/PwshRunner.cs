using Microsoft.Extensions.Logging;
using Reductech.EDR.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Reductech.EDR.Connectors.Pwsh
{

public class PwshRunner
{
    internal static void ProcessData<T>(object? sender, int index, Action<T> action)
    {
        if (sender is PSDataCollection<T> dc)
        {
            var pso = dc[index];
            action.Invoke(pso);
            dc.RemoveAt(index);
        }
        else
        {
            throw new ArgumentException(
                "Sender must be of type " + nameof(PSDataCollection<T>),
                nameof(sender)
            );
        }
    }

    public static async IAsyncEnumerable<PSObject> RunScript(
        string script,
        ILogger logger,
        Entity? variables = null)
    {
        var iss = InitialSessionState.CreateDefault();

        if (variables != null)
        {
            var vars = variables.Select(
                v => new SessionStateVariableEntry(v.Name, v.BestValue, string.Empty)
            );

            iss.Variables.Add(vars);
        }

        using var ps = PowerShell.Create(iss);

        var output = new PSDataCollection<PSObject>();
        var buffer = new BufferBlock<PSObject>();

        output.DataAdded += (sender, ev) =>
            ProcessData<PSObject>(sender, ev.Index, pso => buffer.Post(pso));

        ps.Streams.Error.DataAdded += (sender, ev) => ProcessData<ErrorRecord>(
            sender,
            ev.Index,
            pso => logger.LogError(pso.Exception.Message)
        );

        ps.Streams.Warning.DataAdded += (sender, ev) => ProcessData<WarningRecord>(
            sender,
            ev.Index,
            pso => logger.LogWarning(pso.Message)
        );

        ps.AddScript(script);

        var psTask = Task.Factory.FromAsync(
            ps.BeginInvoke<PSObject, PSObject>(null, output),
            end =>
            {
                ps.EndInvoke(end);
                buffer.Complete();
            }
        );

        while (await buffer.OutputAvailableAsync())
            yield return await buffer.ReceiveAsync();

        await psTask;
    }

    public static async IAsyncEnumerable<Entity> GetEntityEnumerable(
        string script,
        ILogger logger,
        Entity? variables = null)
    {
        await foreach (var pso in RunScript(script, logger, variables))
            yield return EntityFromPSObject(pso);
    }

    public static Entity EntityFromPSObject(PSObject pso)
    {
        Entity? entity;

        if (pso == null)
            throw new NullReferenceException($"{nameof(pso)} cannot be null");

        switch (pso.BaseObject)
        {
            case PSObject _:
            case PSCustomObject _:
            {
                entity = Entity.Create(pso.Properties.Select(p => (p.Name, p.Value)));
                break;
            }
            case Hashtable ht:
            {
                var list = new List<(string, object)>();

                foreach (var key in ht.Keys)
                    list.Add((key.ToString()!, ht[key]!));

                entity = Entity.Create(list);
                break;
            }
            case object[] arr:
                entity = Entity.Create((Entity.PrimitiveKey, arr));
                break;
            default:
                entity = Entity.Create((Entity.PrimitiveKey, pso.BaseObject));
                break;
        }

        return entity;
    }
}

}
