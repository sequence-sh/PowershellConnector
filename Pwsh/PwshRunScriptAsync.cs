using System.ComponentModel.DataAnnotations;
using System.Management.Automation;
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
/// Executes a powershell script and returns any results written to the pipeline
/// as an entity stream.
/// Running a script asynchronously allows for input streaming.
/// Note that the script only runs when the output of this Step is read.
/// </summary>
[Alias("PwshRunAsync")]
[Alias("PowerShellRunAsync")]
[Alias("PowerShellRunScriptAsync")]
public sealed class PwshRunScriptAsync : CompoundStep<Array<Entity>>
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

        PSDataCollection<PSObject>? input = null;

        #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async ValueTask<Result<Unit, IError>> AddObject(Entity x, CancellationToken ct)
        {
            input.Add(PwshRunner.PSObjectFromEntity(x));
            return Unit.Default;
        }
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        if (Input != null)
        {
            var inputStream = await Input.Run(stateMonad, cancellationToken);

            if (inputStream.IsFailure)
                return inputStream.ConvertFailure<Array<Entity>>();

            input = new PSDataCollection<PSObject>();

            _ = inputStream.Value.ForEach(AddObject, cancellationToken)
                .ContinueWith(_ => input.Complete(), cancellationToken);
        }

        var stream = PwshRunner.GetEntityEnumerable(script.Value, stateMonad.Logger, vars, input)
            .ToSCLArray();

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
    [Alias("SetVariables")]
    [Alias("WithVariables")]
    public IStep<Entity>? Variables { get; set; } = null;

    /// <summary>
    /// Input stream, used to pipeline data to the Script.
    /// This stream is exposed via the automatic variable $input.
    /// </summary>
    [StepProperty(order: 3)]
    [DefaultValueExplanation("No input stream")]
    [Alias("InputStream")]
    public IStep<Array<Entity>>? Input { get; set; } = null;

    /// <inheritdoc />
    public override IStepFactory StepFactory { get; } =
        new SimpleStepFactory<PwshRunScriptAsync, Array<Entity>>();
}

}
