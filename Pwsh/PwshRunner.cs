using Microsoft.Extensions.Logging;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Reductech.EDR.Connectors.Pwsh
{
    public class PwshRunner
    {
        public const string SingleValuePropertyName = "data";
        
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
                throw new ArgumentException("Sender must be of type " + nameof(PSDataCollection<T>), nameof(sender));
            }
        }

        public static async IAsyncEnumerable<PSObject> RunScript(string script, ILogger logger)
        {

            //logger.LogDebug("Starting PowerShell");

            using var ps = PowerShell.Create();

            var output = new PSDataCollection<PSObject>();
            var buffer = new BufferBlock<PSObject>();

            output.DataAdded += (sender, ev) =>
                ProcessData<PSObject>(sender, ev.Index, pso => buffer.Post(pso));

            ps.Streams.Error.DataAdded += (sender, ev) =>
                ProcessData<ErrorRecord>(sender, ev.Index, pso => logger.LogError(pso.Exception.Message));

            ps.Streams.Warning.DataAdded += (sender, ev) =>
                ProcessData<WarningRecord>(sender, ev.Index, pso => logger.LogWarning(pso.Message));

            ps.AddScript(script);

            var psTask = Task.Factory.FromAsync(ps.BeginInvoke<PSObject, PSObject>(null, output), end =>
            {
                ps.EndInvoke(end);
                //if (ps.EndInvoke(end).Count > 0)
                //    throw new InvalidPowerShellStateException("Pipeline not empty");
                buffer.Complete();
            });

            while (await buffer.OutputAvailableAsync())
                yield return await buffer.ReceiveAsync();

            await psTask;
        }

        public static async IAsyncEnumerable<Entity> GetEntityEnumerable(string script, ILogger logger)
        {
            await foreach (var pso in RunScript(script, logger))
                yield return EntityFromPSObject(pso);
        }

        public static Entity EntityFromPSObject(PSObject pso)
        {
            Entity? entity;
            if (pso.BaseObject is PSObject || pso.BaseObject is PSCustomObject)
            {
                var list = pso.Properties.Select(p => new KeyValuePair<string, EntityValue>(
                    p.Name, EntityValue.Create((string)p.Value))).ToImmutableList();
                entity = new Entity(list);
            }
            else
            {
                entity = new Entity(new KeyValuePair<string, EntityValue>(
                    SingleValuePropertyName, EntityValue.Create(pso.BaseObject.ToString())));
            }
            return entity;
        }
    }
}
