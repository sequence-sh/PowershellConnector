# EDR PowerShell Connector

[![pipeline status](https://gitlab.com/reductech/edr/connectors/pwsh/badges/master/pipeline.svg)](https://gitlab.com/reductech/edr/connectors/pwsh/-/commits/master)
[![coverage report](https://gitlab.com/reductech/edr/connectors/pwsh/badges/master/coverage.svg)](https://gitlab.com/reductech/edr/connectors/pwsh/-/commits/master)
[![Gitter](https://badges.gitter.im/reductech/community.svg)](https://gitter.im/reductech/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

A class library for executing inline PowerShell scripts as Steps.

# How to Use

The following Sequence runs an inline script that returns two `PSObjects` and prints them:

```
- EntityForEach
    EntityStream: (PwshRunScript Script: "@( [pscustomobject]@{ prop1 = 'one'; prop2 = 2 }, [pscustomobject]@{ prop1 = 'three'; prop2 = 4 }) | Write-Output")
    Action: (Print (GetVariable <entity>))
```

# Releases

Can be downloaded from the [Releases page](https://gitlab.com/reductech/edr/connectors/pwsh/-/releases).

# NuGet Packages

Are available for download from the [Releases page](https://gitlab.com/reductech/edr/connectors/pwsh/-/releases)
or from the `package nuget` jobs of the [CI pipelines](https://gitlab.com/reductech/edr/connectors/pwsh/-/pipelines). They're also available in:

- [Reductech Nuget feed](https://gitlab.com/reductech/nuget/-/packages) for releases
- [Reductech Nuget-dev feed](https://gitlab.com/reductech/nuget-dev/-/packages) for releases, master branch and dev builds
