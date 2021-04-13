using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Connectors.Pwsh
{

using Reductech.EDR.Core;
using Reductech.EDR.Core.Attributes;

/// <summary>
/// Executes a powershell script and returns any results written to the pipeline
/// as an array of entities.
/// </summary>
[Alias("PwshRun")]
[Alias("PowerShellRun")]
[Alias("PowerShellRunScript")]
public sealed class PwshRunScript : CompoundStep<Array<Entity>>
{
    /// <inheritdoc />
    protected override async Task<Result<Array<Entity>, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        var script = await Script.Run(stateMonad, cancellationToken).Map(x => x.GetStringAsync());

        if (script.IsFailure)
            return script.ConvertFailure<Array<Entity>>();

        Entity? vars = null;

        if (Variables != null)
        {
            var variables = await Variables.Run(stateMonad, cancellationToken);

            if (variables.IsFailure)
                return variables.ConvertFailure<Array<Entity>>();

            vars = variables.Value;
        }

        var result = await PwshRunner.RunScript(script.Value, stateMonad.Logger, vars);

        var elements = await result.Select(PwshRunner.EntityFromPSObject)
            .ToSCLArray()
            .GetElementsAsync(cancellationToken);

        return elements.IsFailure
            ? elements.ConvertFailure<Array<Entity>>()
            : elements.Value.ToSCLArray();
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
        new SimpleStepFactory<PwshRunScript, Array<Entity>>();
}

}
