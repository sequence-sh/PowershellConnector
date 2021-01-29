[![pipeline status](https://gitlab.com/reductech/edr/connectors/pwsh/badges/master/pipeline.svg)](https://gitlab.com/reductech/edr/connectors/pwsh/-/commits/master)
[![coverage report](https://gitlab.com/reductech/edr/connectors/pwsh/badges/master/coverage.svg)](https://gitlab.com/reductech/edr/connectors/pwsh/-/commits/master)
[![Gitter](https://badges.gitter.im/reductech/community.svg)](https://gitter.im/reductech/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

# EDR PowerShell Connector

[Reductech EDR](https://gitlab.com/reductech/edr) is a collection of
libraries that automates cross-application e-discovery and forensic workflows.

The PowerShell connector containes Steps for executing inline PowerShell scripts in
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

### [Try PowerShell Connector](https://gitlab.com/reductech/edr/edr/-/releases)

Using [EDR](https://gitlab.com/reductech/edr/edr),
the command line tool for running Sequences.

## Documentation

- Documentation is available here: https://docs.reductech.io

## E-discovery Reduct

The PowerShell Connector is part of a group of projects called
[E-discovery Reduct](https://gitlab.com/reductech/edr)
which consists of a collection of [Connectors](https://gitlab.com/reductech/edr/connectors)
and a command-line application for running Sequences, called
[EDR](https://gitlab.com/reductech/edr/edr/-/releases).

# Releases

Can be downloaded from the [Releases page](https://gitlab.com/reductech/edr/connectors/pwsh/-/releases).

# NuGet Packages

Are available in the [Reductech Nuget feed](https://gitlab.com/reductech/nuget/-/packages).
