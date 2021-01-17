using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Connectors.Pwsh
{

using Reductech.EDR.Core;

/// <summary>
/// 
/// </summary>
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

        var stream = PwshRunner.GetEntityEnumerable(script.Value, stateMonad.Logger, vars)
            .ToSequence();

        return stream;
    }

    /// <summary>
    /// The script to run
    /// </summary>
    [StepProperty(order: 1)]
    [Required]
    public IStep<StringStream> Script { get; set; } = null!;

    /// <summary>
    /// List of input variables and corresponding values.
    /// </summary>
    [StepProperty(order: 2)]
    [DefaultValueExplanation("No variables passed to the script")]
    public IStep<Entity>? Variables { get; set; } = null;

    ///// <summary>
    ///// 
    ///// </summary>
    //[StepProperty]
    //[DefaultValueExplanation("")]
    //public IStep<Array<Entity>> InputStream { get; set; } = null!;

    /// <inheritdoc />
    public override IStepFactory StepFactory => PwshRunScriptStepFactory.Instance;
}

/// <summary>
/// Executes a powershell script
/// </summary>
public sealed class PwshRunScriptStepFactory : SimpleStepFactory<PwshRunScript, Array<Entity>>
{
    private PwshRunScriptStepFactory() { }

    /// <summary>
    /// The instance.
    /// </summary>
    public static SimpleStepFactory<PwshRunScript, Array<Entity>> Instance { get; } =
        new PwshRunScriptStepFactory();
}

}
