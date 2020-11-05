# .NET Core 3.1 Library Template

[![pipeline status](https://gitlab.com/reductech/templates/dotnetlibrary/badges/master/pipeline.svg)](https://gitlab.com/reductech/templates/dotnetlibrary/-/commits/master)
[![coverage report](https://gitlab.com/reductech/templates/dotnetlibrary/badges/master/coverage.svg)](https://gitlab.com/reductech/templates/dotnetlibrary/-/commits/master)
[![Gitter](https://badges.gitter.im/reductech/dotnetlibrary.svg)](https://gitter.im/reductech/dotnetlibrary?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

An example of a .NET Core library that uses:

- [xUnit](https://xunit.github.io/) for testing
- [Stryker](https://stryker-mutator.io/) for mutation testing
- [Coverlet](https://github.com/tonerdo/coverlet) for code coverage
- and [GitLab CI](https://docs.gitlab.com/ee/ci/README.html) for... CI.

# Releases

Can be downloaded from the [Releases page](https://gitlab.com/reductech/templates/dotnetlibrary/-/releases).

# NuGet Packages

Are available for download from the [Releases page](https://gitlab.com/reductech/templates/dotnetlibrary/-/releases)
or from the `package nuget` jobs of the CI pipelines. They're also available in:

- [Reductech Nuget feed](https://gitlab.com/reductech/nuget/-/packages) for releases
- [Reductech Nuget-dev feed](https://gitlab.com/reductech/nuget-dev/-/packages) for releases, master branch and dev builds

# How to use this template

## When creating a new project in GitLab

1. Go to *Create from template* tab
2. Select *Group*
3. Select *Reductech / templates*
4. And finally, select *DotNetLibrary*

## Once you've cloned the project

### 1. Run the handy script to rename the project and solution

```powershell
.\Rename-Template.ps1 -Name 'NewProjectName' -Namespace 'Reductech.Utilities'
```

### 2. Add a couple of things

- *Project/Project.csproj*:
   1. Check that the *RootNamespace*, *AssemblyName* and *PackageId* have been correctly updated
   2. Fill in these properties
      - Title
      - Description
      - Product
      - PackageTags - this needs to be a semicolon-separated list
   3. Check that the new urls have been correctly updated

- *Project.Tests/Project.Tests.csproj*:
   1. Check that the *RepositoryUrl* has been correctly updated

- *.gitlab-ci.yml*
   1. Update *PACKAGE_NAME_NUGET* and *PACKAGE_NAME_DLL* if you would like
   the downloadable artifacts to have a name other than the project name.

- *.gitlab/issue_templates/Feature.md* and *.gitlab/issue_templates/Bug.md*
   1. Update the epic in the template to the one that tracks your project issues.
   2. Optional. Update the labels to better suit your project.
   The `area::core` label should be changed to the area the project mostly works on.

### 3. Commit and clean-up

```
Remove-Item -Path .\Rename-Template.ps1
git commit -a -m "Rename template"
git clean -fdx
dotnet restore
```

And you're ready.

# Versioning and the CI Pipeline

For more information on the CI pipeline and how versioning is done,
please see the [readme](https://gitlab.com/reductech/templates/cicd/dotnet/-/blob/master/README.md)
of reductech/templates/cicd/dotnet>.

# Library / Test default project properties

Default project properties for a library and test project.

## Library

```xml
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <RootNamespace>Reductech.Templates.DotNetLibrary</RootNamespace>
    <AssemblyName>Reductech.Templates.DotNetLibrary</AssemblyName>
    <Version>0.1.0$(VersionSuffix)</Version>
  </PropertyGroup>

  <PropertyGroup>
    <PackageId>Reductech.Templates.DotNetLibrary</PackageId>
    <Title>.NET Core Library Template</Title>
    <Description>An example of a .NET Core library that uses NUnit for testing, Coverlet for code coverage, and GitLab CI.</Description>
    <Product>Templates</Product>
    <PackageTags>template;dotnet;csharp;cicd;gitlab</PackageTags>

    <PackageProjectUrl>https://gitlab.com/reductech/templates/dotnetlibrary</PackageProjectUrl>
    <RepositoryUrl>https://gitlab.com/reductech/templates/dotnetlibrary</RepositoryUrl>
    <PackageReleaseNotes>Please see https://gitlab.com/reductech/templates/dotnetlibrary/-/releases</PackageReleaseNotes>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
    
    <Authors>reductech</Authors>
    <Company>Reductech</Company>
    <Copyright>Copyright (c) 2020 Reductech Ltd</Copyright>
  </PropertyGroup>
```

The properties that need to be updated:

- RootNamespace
- AssemblyName
- Version - **DO NOT** remove the `VersionSuffix` block
- PackageId
- Title
- Description
- Product
- PackageProjectUrl
- RepositoryUrl
- PackageReleaseNotes

## Test Project

```xml
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsPublishable>false</IsPublishable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <PropertyGroup>
    <RepositoryUrl>https://gitlab.com/reductech/templates/dotnetlibrary</RepositoryUrl>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
    <Authors>reductech</Authors>
    <Company>Reductech</Company>
    <Copyright>Copyright (c) 2020 Reductech Ltd</Copyright>
  </PropertyGroup>
```

The properties that need to be updated:

- RepositoryUrl

> :notepad_spiral: `IsTestProject` is explicitly set because of issues with cross-platform builds.

# Creating new releases

See the [Release](.gitlab/issue_templates/Release.md) issue template for more details.

A script is available to auto-generate the changelog based on
the git merges and associated issues:

reductech/pwsh/New-Changelog>

See example [CHANGELOG.md](CHANGELOG.md) for this project.
