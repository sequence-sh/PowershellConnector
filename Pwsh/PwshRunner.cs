using Microsoft.Extensions.Logging;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Reductech.EDR.Core.Internal;

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

        private static EntitySingleValue GetEntitySingleValue(object o) =>
            GetEntitySingleValue(o, CultureInfo.InvariantCulture);
        
        private static EntitySingleValue GetEntitySingleValue(object o, IFormatProvider cultureInfo)
        {
            EntitySingleValue value;
            switch (o)
            {
                case string s:
                    value = new EntitySingleValue(s, s);
                    break;
                case int i:
                    value = new EntitySingleValue(i, i.ToString());
                    break;
                case double d:
                    value = new EntitySingleValue(d, d.ToString(cultureInfo));
                    break;
                case bool b:
                    value = new EntitySingleValue(b, b.ToString());
                    break;
                case Enumeration e:
                    value = new EntitySingleValue(e, e.Value);
                    break;
                case DateTime dt:
                    value = new EntitySingleValue(dt, dt.ToString(cultureInfo));
                    break;
                case Entity ent:
                    value = new EntitySingleValue(ent, string.Empty);
                    break;
                default:
                    var str = o.ToString() ?? string.Empty;
                    value = new EntitySingleValue(str, str);
                    break;
            }
            return value;
        }
        
        public static Entity EntityFromPSObject(PSObject pso)
        {
            Entity? entity;
            switch (pso.BaseObject)
            {
                case PSObject _:
                case PSCustomObject _:
                {
                    var list = pso.Properties.Select(p => new KeyValuePair<string, EntityValue>(
                        p.Name, new EntityValue(GetEntitySingleValue(p.Value)))).ToImmutableList();
                    entity = new Entity(list);
                    break;
                }
                case Hashtable ht:
                {
                    var list = new List<KeyValuePair<string, EntityValue>>();
                    foreach (var key in ht.Keys)
                    {
                        var val = GetEntitySingleValue(ht[key]!);
                        list.Add(new KeyValuePair<string, EntityValue>(key.ToString()!, new EntityValue(val)));
                    }
                    entity = new Entity(list.ToImmutableList());
                    break;
                }
                case object[] arr:
                    var values = arr.Select(GetEntitySingleValue).ToImmutableList();
                    entity = new Entity(new KeyValuePair<string, EntityValue>(Entity.PrimitiveKey,
                        new EntityValue(values)));
                    break;
                default:
                    entity = new Entity(new KeyValuePair<string, EntityValue>(
                        Entity.PrimitiveKey, new EntityValue(GetEntitySingleValue(pso.BaseObject))));
                    break;
            }
            return entity;
        }
    }
}
