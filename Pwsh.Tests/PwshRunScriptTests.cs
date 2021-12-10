namespace Reductech.EDR.Connectors.Pwsh.Tests;

public partial class PwshRunScriptTests : StepTestBase<PwshRunScript, Unit>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "Run PowerShell script",
                new PwshRunScript
                {
                    Script = Constant(@"Write-Output output; Write-Warning 'hello!'")
                },
                Unit.Default,
                "hello!"
            );

            yield return new StepCase(
                "Run PowerShell passing a variable set to it",
                new PwshRunScript
                {
                    Script = Constant(@"$var1, $var2 | % { Write-Information $_ }"),
                    Variables = Constant(
                        Entity.Create(
                            ("var1", "ABC"),
                            ("var2", "DEF")
                        )
                    )
                },
                Unit.Default,
                "ABC",
                "DEF"
            );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<DeserializeCase> DeserializeCases
    {
        get
        {
            yield return new DeserializeCase(
                "Run script with no input or output",
                @"- PwshRunScript 'Write-Information Hello!'",
                Unit.Default,
                "Hello!"
            );
        }
    }
}
