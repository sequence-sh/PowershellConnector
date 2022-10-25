using Reductech.Sequence.Core.Entities;
using Reductech.Sequence.Core.Steps;

namespace Reductech.Sequence.Connectors.Pwsh.Tests;

public partial class PwshRunScriptAsyncTests : StepTestBase<PwshRunScriptAsync, Array<Entity>>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "Run PowerShell script that returns a string",
                new ForEach<Entity>
                {
                    Array =
                        new PwshRunScriptAsync { Script = Constant(@"Write-Output 'hello!'") },
                    Action = new LambdaFunction<Entity, Unit>(
                        null,
                        new Log { Value = GetEntityVariable }
                    )
                },
                Unit.Default,
                $"('{EntityKey.PrimitiveKey}': \"hello!\")"
            );

            yield return new StepCase(
                "Run PowerShell script that returns nothing but emits a warning",
                new ForEach<Entity>
                {
                    Array =
                        new PwshRunScriptAsync { Script = Constant(@"Write-Warning 'warning'") },
                    Action = new LambdaFunction<Entity, Unit>(
                        null,
                        new Log { Value = GetEntityVariable }
                    )
                },
                Unit.Default,
                "warning"
            );

            yield return new StepCase(
                "Run PowerShell script that returns a stream of ints",
                new ForEach<Entity>
                {
                    Array =
                        new PwshRunScriptAsync { Script = Constant(@"1..3 | Write-Output") },
                    Action = new LambdaFunction<Entity, Unit>(
                        null,
                        new Log { Value = GetEntityVariable }
                    )
                },
                Unit.Default,
                $"('{EntityKey.PrimitiveKey}': 1)",
                $"('{EntityKey.PrimitiveKey}': 2)",
                $"('{EntityKey.PrimitiveKey}': 3)"
            );

            yield return new StepCase(
                "Run PowerShell script that returns a PSObject",
                new ForEach<Entity>
                {
                    Array = new PwshRunScriptAsync
                    {
                        Script = Constant(
                            @"[pscustomobject]@{ prop1 = 'one' ; prop2 = 2 } | Write-Output"
                        )
                    },
                    Action = new LambdaFunction<Entity, Unit>(
                        null,
                        new Log { Value = GetEntityVariable }
                    )
                },
                Unit.Default,
                "('prop1': \"one\" 'prop2': 2)"
            );

            yield return new StepCase(
                "Run PowerShell passing a variable set to it",
                new ForEach<Entity>
                {
                    Array = new PwshRunScriptAsync
                    {
                        Script = Constant(@"$var1, $var2 | Write-Output"),
                        Variables = Constant(
                            Entity.Create(
                                ("var1", "ABC"),
                                ("var2", "DEF")
                            )
                        )
                    },
                    Action = new LambdaFunction<Entity, Unit>(
                        null,
                        new Log { Value = GetEntityVariable }
                    )
                },
                Unit.Default,
                "('value': \"ABC\")",
                "('value': \"DEF\")"
            );

            yield return new StepCase(
                "Run PowerShell with an Input",
                new ForEach<Entity>
                {
                    Array = new PwshRunScriptAsync
                    {
                        Script = Constant(@"$Input | ForEach-Object { Write-Output $_ }"),
                        Input = Array(
                            Entity.Create(("key1", 1), ("key2", "two")),
                            Entity.Create(("key3", 3), ("key4", new[] { "four", "forty" }))
                        )
                    },
                    Action = new LambdaFunction<Entity, Unit>(
                        null,
                        new Log { Value = GetEntityVariable }
                    )
                },
                Unit.Default,
                "('key1': 1 'key2': \"two\")",
                "('key3': 3 'key4': [\"four\", \"forty\"])"
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
    Array: (PwshRunScriptAsync Script: ""@( [pscustomobject]@{ prop1 = 'one'; prop2 = 2 }, [pscustomobject]@{ prop1 = 'three'; prop2 = 4 }) | Write-Output"")
    Action: (Log <>)",
                Unit.Default,
                "('prop1': \"one\" 'prop2': 2)",
                "('prop1': \"three\" 'prop2': 4)"
            );

            yield return new DeserializeCase(
                "Run script that takes an entity array as input and prints results",
                @"
- <Input> = [
    (prop1: ""value1"" prop2: 2),
    (prop1: ""value3"" prop2: 4)
  ]
- ForEach
    Array: (PwshRunScriptAsync
        Script: ""$input | ForEach-Object { Write-Output $_ }""
        Input: <Input>
    )
    Action: (Log <>)",
                Unit.Default,
                "('prop1': \"value1\" 'prop2': 2)",
                "('prop1': \"value3\" 'prop2': 4)"
            ) { IgnoreFinalState = true };
        }
    }
}
