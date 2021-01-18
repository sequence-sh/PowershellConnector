using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;
using Reductech.EDR.Core.Util;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Reductech.EDR.Connectors.Pwsh.Tests
{

public class PwshRunScriptTests : StepTestBase<PwshRunScript, Array<Entity>>
{
    /// <inheritdoc />
    public PwshRunScriptTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "Run PowerShell script that returns a string",
                new ForEach<Entity>()
                {
                    Array = new PwshRunScript { Script = Constant(@"Write-Output 'hello!'") },
                    Action = new Print<Entity>
                    {
                        Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                    }
                },
                Unit.Default,
                $"({Entity.PrimitiveKey}: \"hello!\")"
            );

            yield return new StepCase(
                "Run PowerShell script that returns nothing but emits a warning",
                new ForEach<Entity>()
                {
                    Array = new PwshRunScript { Script = Constant(@"Write-Warning 'warning'") },
                    Action = new Print<Entity>
                    {
                        Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                    }
                },
                Unit.Default,
                "warning"
            );

            yield return new StepCase(
                "Run PowerShell script that returns a stream of ints",
                new ForEach<Entity>()
                {
                    Array = new PwshRunScript { Script = Constant(@"1..3 | Write-Output") },
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

            yield return new StepCase(
                "Run PowerShell script that returns a PSObject",
                new ForEach<Entity>()
                {
                    Array = new PwshRunScript
                    {
                        Script = Constant(
                            @"[pscustomobject]@{ prop1 = 'one' ; prop2 = 2 } | Write-Output"
                        )
                    },
                    Action = new Print<Entity>
                    {
                        Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                    }
                },
                Unit.Default,
                "(prop1: \"one\" prop2: 2)"
            );

            yield return new StepCase(
                "Run PowerShell passing a variable set to it",
                new ForEach<Entity>()
                {
                    Array = new PwshRunScript
                    {
                        Script = Constant(@"$var1, $var2 | Write-Output"),
                        Variables = Constant(
                            CreateEntity(
                                ("var1", "ABC"),
                                ("var2", "DEF")
                            )
                        )
                    },
                    Action = new Print<Entity>
                    {
                        Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                    }
                },
                Unit.Default,
                "(value: \"ABC\")",
                "(value: \"DEF\")"
            );

            yield return new StepCase(
                "Run PowerShell with an Input",
                new ForEach<Entity>()
                {
                    Array = new PwshRunScript
                    {
                        Script = Constant(@"$Input | ForEach-Object { Write-Output $_ }"),
                        Input = Array(
                            CreateEntity(("key1", 1), ("key2", "two")),
                            CreateEntity(("key3", 3), ("key4", new[] { "four", "forty" }))
                        )
                    },
                    Action = new Print<Entity>
                    {
                        Value = new GetVariable<Entity> { Variable = VariableName.Entity }
                    }
                },
                Unit.Default,
                "(key1: 1 key2: \"two\")",
                "(key3: 3 key4: [\"four\", \"forty\"])"
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<DeserializeCase> DeserializeCases
    {
        get
        {
            yield return new DeserializeCase(
                "Run script that returns two PSObjects and print results",
                @"
- ForEach
    Array: (PwshRunScript Script: ""@( [pscustomobject]@{ prop1 = 'one'; prop2 = 2 }, [pscustomobject]@{ prop1 = 'three'; prop2 = 4 }) | Write-Output"")
    Action: (Print (GetVariable <entity>))",
                Unit.Default,
                "(prop1: \"one\" prop2: 2)",
                "(prop1: \"three\" prop2: 4)"
            );

            yield return new DeserializeCase(
                "Run script that takes an entity array as input and prints results",
                @"
- <Input> = [
    (prop1: ""value1"" prop2: 2),
    (prop1: ""value3"" prop2: 4)
  ]
- ForEach
    Array: (PwshRunScript
        Script: ""$input | ForEach-Object { Write-Output $_ }""
        Input: <Input>
    )
    Action: (Print (GetVariable <entity>))",
                Unit.Default,
                "(prop1: \"value1\" prop2: 2)",
                "(prop1: \"value3\" prop2: 4)"
            ) { IgnoreFinalState = true };
        }
    }
}

}
