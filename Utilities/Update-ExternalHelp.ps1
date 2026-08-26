<#
.SYNOPSIS
    Regenerates the external help of the ReverseDSC module.

.DESCRIPTION
    Converts the markdown under docs into the MAML file the binary module serves to Get-Help.
    The markdown is the source of truth: a binary module carries no comment based help, so the
    files under docs are edited by hand and this script only compiles them.

.PARAMETER RepositoryRoot
    Root of the repository. Defaults to the parent of the folder holding this script.

.EXAMPLE
    ./Utilities/Update-ExternalHelp.ps1
#>
[CmdletBinding()]
param
(
    [Parameter()]
    [System.String]
    $RepositoryRoot = (Join-Path -Path $PSScriptRoot -ChildPath '..' -Resolve)
)

$ErrorActionPreference = 'Stop'

if ($null -eq (Get-Module -Name platyPS -ListAvailable))
{
    throw 'platyPS is required to regenerate the external help. Install it with Install-PSResource -Name platyPS.'
}

Import-Module -Name platyPS -Force

$docsPath = Join-Path -Path $RepositoryRoot -ChildPath 'docs'
$helpPath = Join-Path -Path $RepositoryRoot -ChildPath 'Modules\ReverseDSC\en-US'

$null = New-ExternalHelp -Path $docsPath -OutputPath $helpPath -Force

Write-Host -Object "Generated the external help at $helpPath"
