using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Connectors.Pwsh
{
    using Reductech.EDR.Core;
    
    /// <summary>
    /// 
    /// </summary>
    public sealed class PwshRunScript : CompoundStep<EntityStream>
    {

        /// <inheritdoc />
        public override async Task<Result<EntityStream, IError>> Run(IStateMonad stateMonad, CancellationToken cancellationToken)
        {
            var script = await Script.Run(stateMonad, cancellationToken);

            if (script.IsFailure)
                return script.ConvertFailure<EntityStream>();

            Entity? vars = null;
            
            if (Variables != null)
            {
                var variables = await Variables.Run(stateMonad, cancellationToken);
                if (variables.IsFailure)
                    return variables.ConvertFailure<EntityStream>();
                vars = variables.Value;
            }

            var stream = PwshRunner.GetEntityEnumerable(script.Value, stateMonad.Logger, vars);
            var entityStream = new EntityStream(stream);

            return entityStream;
        }

        /// <summary>
        /// The script to run
        /// </summary>
        [StepProperty(order: 1)]
        [Required]
        public IStep<string> Script { get; set; } = null!;

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
        //public IStep<EntityStream> InputStream { get; set; } = null!;

        /// <inheritdoc />
        public override IStepFactory StepFactory => PwshRunScriptStepFactory.Instance;
    }

    /// <summary>
    /// Executes a powershell script
    /// </summary>
    public sealed class PwshRunScriptStepFactory : SimpleStepFactory<PwshRunScript, EntityStream>
    {
        private PwshRunScriptStepFactory() { }

        /// <summary>
        /// The instance.
        /// </summary>
        public static SimpleStepFactory<PwshRunScript, EntityStream> Instance { get; } = new PwshRunScriptStepFactory();
    }
}
