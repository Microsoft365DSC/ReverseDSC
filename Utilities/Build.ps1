<#
.SYNOPSIS
    Builds the ReverseDSC assembly and stages the PowerShell module.

.DESCRIPTION
    Builds every shipping project under src, then copies the resulting assembly, the module
    manifest and the external help into the ReverseDSC folder at the root of the repository.
    That folder is what gets imported by the tests and published to the PowerShell Gallery.

.PARAMETER Configuration
    Build configuration to use.

.PARAMETER RepositoryRoot
    Root of the repository. Defaults to the parent of the folder holding this script.

.PARAMETER SkipClean
    Skips the dotnet clean that runs before the build.

.EXAMPLE
    ./Utilities/Build.ps1 -Configuration Release
#>
[CmdletBinding()]
param
(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [System.String]
    $Configuration = 'Release',

    [Parameter()]
    [System.String]
    $RepositoryRoot = (Join-Path -Path $PSScriptRoot -ChildPath '..' -Resolve),

    [Parameter()]
    [Switch]
    $SkipClean
)

$ErrorActionPreference = 'Stop'

if ($null -eq (Get-Command -Name 'dotnet' -ErrorAction SilentlyContinue))
{
    throw 'The .NET SDK is required to build ReverseDSC but dotnet was not found on the PATH.'
}

$sourcePath = Join-Path -Path $RepositoryRoot -ChildPath 'src'
$targetPath = Join-Path -Path $RepositoryRoot -ChildPath 'ReverseDSC'
$modulePath = Join-Path -Path $RepositoryRoot -ChildPath 'Modules\ReverseDSC'

# Only the netstandard2.0 shipping projects are packaged into the module. The test project targets
# a different framework and is built by its own tooling.
$projects = Get-ChildItem -Path $sourcePath -Filter '*.csproj' -Recurse |
    Where-Object -FilterScript { $_.Name -notlike '*.Tests.csproj' }

if (-not (Test-Path -Path $targetPath))
{
    $null = New-Item -Path $targetPath -ItemType Directory -Force
}

foreach ($project in $projects)
{
    if (-not $SkipClean.IsPresent)
    {
        Write-Host -Object "Cleaning $($project.BaseName)"
        & dotnet clean $project.FullName -c $Configuration --nologo | Out-Null
    }

    Write-Host -Object "Building $($project.BaseName) ($Configuration)"
    & dotnet build $project.FullName -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0)
    {
        throw "Building $($project.Name) failed with exit code $LASTEXITCODE."
    }

    $outputPath = Join-Path -Path $project.DirectoryName -ChildPath "bin\$Configuration\netstandard2.0"
    foreach ($extension in @('dll', 'pdb', 'xml'))
    {
        $artifact = Join-Path -Path $outputPath -ChildPath "$($project.BaseName).$extension"
        if (Test-Path -Path $artifact)
        {
            Copy-Item -Path $artifact -Destination $targetPath -Force
        }
    }
}

Copy-Item -Path (Join-Path -Path $modulePath -ChildPath 'ReverseDSC.psd1') -Destination $targetPath -Force

$helpSourcePath = Join-Path -Path $modulePath -ChildPath 'en-US'
if (Test-Path -Path $helpSourcePath)
{
    Copy-Item -Path $helpSourcePath -Destination $targetPath -Recurse -Force
}

Write-Host -Object "Staged the ReverseDSC module at $targetPath"
