using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Util;

namespace Reductech.EDR.Connectors.Pwsh
{

using Reductech.EDR.Core;
using Reductech.EDR.Core.Attributes;

/// <summary>
/// Executes a powershell script.
/// </summary>
[Alias("PwshRun")]
[Alias("PowerShellRun")]
[Alias("PowerShellRunScript")]
public sealed class PwshRunScript : CompoundStep<Unit>
{
    /// <inheritdoc />
    protected override async Task<Result<Unit, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        var script = await Script.Run(stateMonad, cancellationToken).Map(x => x.GetStringAsync());

        if (script.IsFailure)
            return script.ConvertFailure<Unit>();

        Entity? vars = null;

        if (Variables != null)
        {
            var variables = await Variables.Run(stateMonad, cancellationToken);

            if (variables.IsFailure)
                return variables.ConvertFailure<Unit>();

            vars = variables.Value;
        }

        await PwshRunner.RunScript(script.Value, stateMonad.Logger, vars);

        return Unit.Default;
    }

    /// <summary>
    /// The script to run
    /// </summary>
    [StepProperty(1)]
    [Required]
    public IStep<StringStream> Script { get; set; } = null!;

    /// <summary>
    /// List of input variables and corresponding values.
    /// </summary>
    [StepProperty(2)]
    [DefaultValueExplanation("No variables passed to the script")]
    [Alias("SetVariables")]
    [Alias("WithVariables")]
    public IStep<Entity>? Variables { get; set; } = null;

    /// <inheritdoc />
    public override IStepFactory StepFactory { get; } =
        new SimpleStepFactory<PwshRunScript, Unit>();
}

}
