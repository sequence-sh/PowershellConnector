using Reductech.EDR.Core;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using Reductech.EDR.Core.Util;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Reductech.EDR.Connectors.Pwsh.Tests
{
    public class RunPowerShellTests : StepTestBase<PwshRunScript, EntityStream>
    {
        /// <inheritdoc />
        public RunPowerShellTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        /// <inheritdoc />
        protected override IEnumerable<StepCase> StepCases
        {
            get
            {
                yield return new StepCase("Run PowerShell script",
                    new EntityForEach()
                    {
                        EntityStream = new PwshRunScript
                        {
                            Script = Constant(@"Write-Output 'hello!'")
                        },
                        Action = new Print<Entity>
                        {
                            Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                        }
                    },
                    Unit.Default,
                    $"({Entity.PrimitiveKey}: \"hello!\")"
                );
            }
        }
    }
}
