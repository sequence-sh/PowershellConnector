[CmdletBinding()]
Param (
    # New name for the project. e.g. ANewProject.
    [Parameter(Mandatory=$true,
               Position=1)]
    [string]$Name,

    # New namespace for the project. e.g. 'Reductech.Utilities'.
    [Parameter(Mandatory=$true,
               Position=2)]
    [string]$Namespace,

    # The url of the new project. By default this is generated from the
    # `git remote get-url origin` command.
    [Parameter(Mandatory=$false)]
    [string]$NewUrl,

    # The template name. Default is' DotNetLibrary'.
    [Parameter(Mandatory=$false)]
    [string]$TemplateName = 'DotNetLibrary',

    # The template namespace. Default is 'Reductech.Templates'.
    [Parameter(Mandatory=$false)]
    [string]$TemplateNamespace = 'Reductech.Templates',

    # The template url. Default is 'https://gitlab.com/reductech/templates/dotnetlibrary'.
    [Parameter(Mandatory=$false)]
    [string]$TemplateUrl = 'https://gitlab.com/reductech/templates/dotnetlibrary',

    # The gitter chatroom. Default is 'reductech/community'.
    [Parameter(Mandatory=$false)]
    [string]$GitterRoom = 'reductech/community',

    # Do not remove content from the readme.
    [Parameter(Mandatory=$false)]
    [switch]$SkipReadme
)

if (!$NewUrl) {
    $gitUrl = git remote get-url origin
    $NewUrl = $gitUrl -replace '\.git$' -replace 'git@gitlab\.com:', 'https://gitlab.com/'
}

$encoding = [System.Text.UTF8Encoding]::new($false)

$templateFullName = $TemplateNamespace + '.' + $TemplateName
$fullName = $Namespace + '.' + $Name

@(
    "$TemplateName/$TemplateName.csproj"
    "$TemplateName/MessageWriter.cs"
    "$TemplateName.Tests/$TemplateName.Tests.csproj"
    "$TemplateName.Tests/MessageWriterTests.cs"
) | ForEach-Object {
    $path = Join-Path (Get-Location).Path $_
    $content = Get-Content $path -Raw
    $newContent = $content -replace [regex]::Escape($TemplateUrl), $NewUrl `
                           -replace [regex]::Escape($templateFullName), $fullName `
                           -replace [regex]::Escape($TemplateName), $Name
    if ($_ -match 'csproj$') {
        $newContent = $newContent -replace '<Title>.*?</Title>', '<Title></Title>' `
                                  -replace '<Description>.*?</Description>', '<Description></Description>' `
                                  -replace '<Product>.*?</Product>', '<Product></Product>' `
                                  -replace '<PackageTags>.*?</PackageTags>', '<PackageTags></PackageTags>'
    }
    [System.IO.File]::WriteAllText($path, $newContent, $encoding)
}

if (!$SkipReadme) {
    $readmePath = Join-Path (Get-Location).Path 'README.md'
    $readme = Get-Content $readmePath -Raw
    $rxOpt = [System.Text.RegularExpressions.RegexOptions]::Singleline
    $newReadme = [regex]::Replace($readme, '# How to use this template.*', '', $rxOpt)
    $newReadme = $newReadme -replace [regex]::Escape($TemplateUrl), $NewUrl `
                            -replace ".NET Core 3.1 Library Template", $Name `
                            -replace [regex]::Escape("[![Gitter](https://badges.gitter.im/reductech/dotnetlibrary.svg)](https://gitter.im/reductech/dotnetlibrary?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)"),
                                     "[![Gitter](https://badges.gitter.im/${GitterRoom}.svg)](https://gitter.im/${GitterRoom}?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)"
    [System.IO.File]::WriteAllText($readmePath, $newReadme, $encoding)
}

Rename-Item -Path "./$TemplateName/$TemplateName.csproj" -NewName "$Name.csproj"
Rename-Item -Path "./$TemplateName" -NewName $Name
Rename-Item -Path "./$TemplateName.Tests/$TemplateName.Tests.csproj" -NewName "$Name.Tests.csproj"
Rename-Item -Path "./$TemplateName.Tests" -NewName "$Name.Tests"

Remove-Item CHANGELOG.md

Remove-Item dotnetlibrary.sln
dotnet new sln -n $Name
dotnet sln add "$Name/$Name.csproj"
dotnet sln add "$Name.Tests/$Name.Tests.csproj"
