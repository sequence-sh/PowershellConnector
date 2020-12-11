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
                yield return new StepCase("Run PowerShell script that returns a string",
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
                
                yield return new StepCase("Run PowerShell script that returns nothing but emits a warning",
                    new EntityForEach()
                    {
                        EntityStream = new PwshRunScript
                        {
                            Script = Constant(@"Write-Warning 'warning'")
                        },
                        Action = new Print<Entity>
                        {
                            Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                        }
                    },
                    Unit.Default,
                    "warning"
                );
                
                yield return new StepCase("Run PowerShell script that returns a stream of ints",
                    new EntityForEach()
                    {
                        EntityStream = new PwshRunScript
                        {
                            Script = Constant(@"1..3 | Write-Output")
                        },
                        Action = new Print<Entity>
                        {
                            Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                        }
                    },
                    Unit.Default,
                    $"({Entity.PrimitiveKey}: 1)",
                    $"({Entity.PrimitiveKey}: 2)",
                    $"({Entity.PrimitiveKey}: 3)"
                );
                
                yield return new StepCase("Run PowerShell script that returns a PSObject",
                    new EntityForEach()
                    {
                        EntityStream = new PwshRunScript
                        {
                            Script = Constant(@"[pscustomobject]@{ prop1 = 'one' ; prop2 = 2 } | Write-Output")
                        },
                        Action = new Print<Entity>
                        {
                            Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                        }
                    },
                    Unit.Default,
                    "(prop1: \"one\" prop2: 2)"
                );
            }
        }
    }
}
