# Sequence® PowerShell Connector

[Sequence®](https://gitlab.com/reductech/sequence) is a collection of
libraries that automates cross-application e-discovery and forensic workflows.

The PowerShell connector contains Steps for executing inline PowerShell scripts in
Sequences.

## Examples

Run an inline script that returns two `PSObjects` and prints them:

```powershell
- ForEach
    Array: (PwshRunScript Script: "@( [pscustomobject]@{ prop1 = 'one'; prop2 = 2 }, [pscustomobject]@{ prop1 = 'three'; prop2 = 4 }) | Write-Output")
    Action: (Print (GetVariable <entity>))
```

Run a script that receives input as an Entity stream:

```powershell
- <Input> = [
    (prop1: "value1" prop2: 2),
    (prop1: "value3" prop2: 4)
  ]
- ForEach
    Array: (PwshRunScript
        Script: "$input | ForEach-Object { Write-Output $_ }"
        Input: <Input>
    )
    Action: (Print (GetVariable <entity>))
```

# Releases

Can be downloaded from the [Releases page](https://gitlab.com/reductech/sequence/connectors/pwsh/-/releases).

# NuGet Packages

Are available in the [Reductech Nuget feed](https://gitlab.com/reductech/nuget/-/packages).
