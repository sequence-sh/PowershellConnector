using System.Reflection;

namespace Reductech.EDR.Connectors.Pwsh.Tests;

public class MetaTests : MetaTestsBase
{
    /// <inheritdoc />
    public override Assembly StepAssembly { get; } = Assembly.GetAssembly(typeof(PwshRunScript))!;

    /// <inheritdoc />
    public override Assembly TestAssembly { get; } = Assembly.GetAssembly(typeof(MetaTests))!;
}
