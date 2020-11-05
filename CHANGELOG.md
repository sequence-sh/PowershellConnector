## v0.3.0 (2020-11-05)

CI/CD configuration has now been moved to:
https://gitlab.com/reductech/templates/cicd/dotnet

### New Features

- Create new project for CI/CD templates and include them in the .gitlab-ci.yml #49

## v0.2.0 (2020-11-04)

### New Features

- Split CI file and use includes #38
- Increase timeout for build stage artifacts, so that developers can re-run dependent jobs without rerunning the whole pipeline #40
- Add issue template for release prep #46
- Use first instead of last project if more than one project is found #47

### Documentation

- Add changelog details to the readme #44

## v0.1.0 (2020-10-27)

### New Features

- Add mutation testing as a ci job #23
- Push releases to the dev nuget #34
- Automatically increment version on the master branch #32
- Convert test project to xunit #22
- Standardise and add new nuget properties to the csproj files #28
- Release pipeline should check for project version and tag match #21
- Use gitlab ci template for code_quality stage #19
- Use script to get code coverage from coverlet report #17
- Use semantic versioning for release tags #16

### Bug Fixes

- Rename-Template.ps1 should update project links in the readme.md file #42
- Update line terminators #41
- Fix mutation testing job should contain root level report #29
- Coverlet cobertura reports should be displayed in merge requests #18
- Should add reductech as a package source on each stage of the build script #5

### Maintenance

- Use date instead of epoch for build versions #45
- Append the type of package to job artifacts, so that devs can more easily differentiate what they download #36
- Test versioning #33
- Clean up build and package stages #31
- Set up DAG for pipelines #30
- Move stryker report to root directory #27
- Master branch should not include build version #26
- Integrate Get-Coverage.ps1 script into job and remove the file #25
- Update coverage regex and expand parameter names #24
- Use separate push stage for dev and release #20

### Documentation

- Add documentation on how to use the template and the CI script #7

