﻿using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Sequence.Core.Entities;

namespace Sequence.Connectors.Pwsh;

/// <summary>
/// Collection of methods to run powershell scripts
/// </summary>
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

    private static PowerShell CreateRunspace(
        string script,
        ILogger logger,
        Entity? variables = null)
    {
        var iss = InitialSessionState.CreateDefault();

        if (variables != null)
        {
            var vars = variables.Value.Select(
                v => new SessionStateVariableEntry(v.Key.Inner, v.Value, string.Empty)
            );

            iss.Variables.Add(vars);
        }

        var ps = PowerShell.Create(iss);

        ps.Streams.Information.DataAdded += (sender, ev) => ProcessData<InformationRecord>(
            sender,
            ev.Index,
            pso => logger.LogInformation(pso.MessageData.ToString())
        );

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

        return ps;
    }

    /// <summary>
    /// Run a powershell script
    /// </summary>
    public static async Task<List<PSObject>> RunScript(
        string script,
        ILogger logger,
        Entity? variables = null,
        PSDataCollection<PSObject>? input = null)
    {
        using var ps = CreateRunspace(script, logger, variables);

        var result = await (input == null ? ps.InvokeAsync() : ps.InvokeAsync(input));

        return result.ToList();
    }

    /// <summary>
    /// Run a powershell script
    /// </summary>
    public static async IAsyncEnumerable<PSObject> RunScriptAsync(
        string script,
        ILogger logger,
        Entity? variables = null,
        PSDataCollection<PSObject>? input = null)
    {
        using var ps = CreateRunspace(script, logger, variables);

        var output = new PSDataCollection<PSObject>();
        var buffer = new BufferBlock<PSObject>();

        output.DataAdded += (sender, ev) =>
            ProcessData<PSObject>(sender, ev.Index, pso => buffer.Post(pso));

        var psTask = Task.Factory.FromAsync(
            ps.BeginInvoke(input, output),
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

    /// <summary>
    /// Run a script and return the result as an async Enumerable.
    /// </summary>
    public static async IAsyncEnumerable<Entity> GetEntityEnumerable(
        string script,
        ILogger logger,
        Entity? variables = null,
        PSDataCollection<PSObject>? input = null)
    {
        await foreach (var pso in RunScriptAsync(script, logger, variables, input))
            yield return EntityFromPSObject(pso);
    }

    /// <summary>
    /// Convert a PSObject to an Entity
    /// </summary>
    public static Entity EntityFromPSObject(PSObject pso)
    {
        Entity entity;

        if (pso == null)
            throw new NullReferenceException($"{nameof(pso)} cannot be null");

        switch (pso.BaseObject)
        {
            case PSObject _:
            case PSCustomObject _:
            {
                entity = Entity.Create(pso.Properties.Select(p => (p.Name, p.Value)).ToArray()!);
                break;
            }
            case Hashtable ht:
            {
                var list = new List<(string, object?)>();

                // TODO: warning if value is not a primitive
                foreach (var key in ht.Keys)
                    list.Add((key.ToString()!, ht[key]));

                entity = Entity.Create(list.ToArray());
                break;
            }
            default:
                entity = Entity.Create((EntityKey.PrimitiveKey, pso.BaseObject));
                break;
        }

        return entity;
    }

    /// <summary>
    /// Convert an Entity to a PSObject
    /// </summary>
    public static PSObject PSObjectFromEntity(Entity entity)
    {
        var single = entity.TryGetValue(EntityKey.PrimitiveKey);

        if (single.HasValue)
            return new PSObject(GetValue(single.Value));

        var pso = new PSObject();

        static object? GetValue(ISCLObject ev) => ev switch
        {
            Entity nestedEntity       => PSObjectFromEntity(nestedEntity),
            ISCLEnum sclEnum          => sclEnum.EnumValue,
            ISCLOneOf oneOf           => GetValue(oneOf.Value),
            SCLBool sclBool           => sclBool.Value,
            SCLDateTime sclDateTime   => sclDateTime.Value,
            SCLDouble sclDouble       => sclDouble.Value,
            SCLInt sclInt             => sclInt.Value,
            SCLNull                   => null,
            StringStream stringStream => stringStream.GetString(),
            Unit                      => null,
            IArray list               => list.ListIfEvaluated().Value.Select(GetValue).ToList(),
            _                         => null
        };

        foreach (var e in entity)
            pso.Properties.Add(new PSNoteProperty(e.Key.Inner, GetValue(e.Value)));

        return pso;
    }
}
