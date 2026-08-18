function Invoke-TestHarness
{
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [System.String]
        $TestResultsFile,

        [Parameter()]
        [System.String]
        $DscTestsPath,

        [Parameter()]
        [Switch]
        $IgnoreCodeCoverage
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    Write-Host -Object 'Running all ReverseDSC Unit Tests'

    $repoDir = Join-Path -Path $PSScriptRoot -ChildPath '..\' -Resolve

    $oldModPath = $env:PSModulePath
    $env:PSModulePath = $env:PSModulePath + [System.IO.Path]::PathSeparator + $ModuleDirectory

    $testCoverageFiles = @()
    if ($IgnoreCodeCoverage.IsPresent -eq $false)
    {
        $testsDir = Join-Path -Path $repoDir -ChildPath 'Tests'
        Get-ChildItem -Path $repoDir -Include '*.psm1', '*.ps1' -Recurse |
            Where-Object -FilterScript { -not $_.FullName.StartsWith($testsDir, [System.StringComparison]::OrdinalIgnoreCase) } |
            ForEach-Object {
                $testCoverageFiles += $_.FullName
            }
    }

    Import-Module -Name "$repoDir/ReverseDSC.psd1"
    $testsToRun = @()

    # Run Unit Tests

    # ReverseDSC Common Tests
    $getChildItemParameters = @{
        Path    = (Join-Path -Path $repoDir -ChildPath '\Tests')
        Recurse = $true
        Filter  = '*.Tests.ps1'
    }

    # Get all tests '*.Tests.ps1'.
    $commonTestFiles = Get-ChildItem @getChildItemParameters

    $testsToRun += @( $commonTestFiles.FullName )

    $filesToExecute = @()
    if ($DscTestsPath -ne '')
    {
        $filesToExecute += $DscTestsPath
    }
    else
    {
        foreach ($testToRun in $testsToRun)
        {
            $filesToExecute += $testToRun
        }
    }

    $Params = [ordered]@{
        Path = $filesToExecute
    }

    $Container = New-PesterContainer @Params

    $Configuration = [PesterConfiguration]@{
        Run    = @{
            Container = $Container
            PassThru  = $true
        }
        Output = @{
            Verbosity = 'Normal'
        }
        Should = @{
            ErrorAction = 'Continue'
        }
    }

    if ([String]::IsNullOrEmpty($TestResultsFile) -eq $false)
    {
        $Configuration.Output.Enabled = $true
        $Configuration.Output.OutputFormat = 'NUnitXml'
        $Configuration.Output.OutputFile = $TestResultsFile
    }

    if ($IgnoreCodeCoverage.IsPresent -eq $false)
    {
        $Configuration.CodeCoverage.Enabled = $true
        $Configuration.CodeCoverage.Path = $testCoverageFiles
        $Configuration.CodeCoverage.OutputPath = 'CodeCov.xml'
        $Configuration.CodeCoverage.OutputFormat = 'JaCoCo'
        $Configuration.CodeCoverage.UseBreakpoints = $false
    }

    $results = Invoke-Pester -Configuration $Configuration

    $message = 'Running the tests took {0} hours, {1} minutes, {2} seconds' -f $sw.Elapsed.Hours, $sw.Elapsed.Minutes, $sw.Elapsed.Seconds
    Write-Host -Object $message

    $env:PSModulePath = $oldModPath
    Write-Host -Object 'Completed running all ReverseDSC Unit Tests'

    Write-TestHarnessSummary -Result $results

    return $results
}

<#
.SYNOPSIS
    Renders the results of Invoke-TestHarness as a report.

.DESCRIPTION
    Writes the test counts and the per file code coverage of the run. Without a path the report
    goes to the console, with a path it is appended as GitHub flavoured markdown, which makes it
    usable as a GitHub Actions step summary.

.PARAMETER Result
    The object returned by Invoke-TestHarness.

.PARAMETER Path
    File to append the markdown report to.

.EXAMPLE
    Write-TestHarnessSummary -Result $results -Path $env:GITHUB_STEP_SUMMARY
#>
function Write-TestHarnessSummary
{
    [CmdletBinding()]
    [OutputType([System.Void])]
    param
    (
        [Parameter(Mandatory = $true)]
        [PSCustomObject]
        $Result,

        [Parameter()]
        [System.String]
        $Path
    )

    $lines = [System.Collections.Generic.List[System.String]]::new()

    $lines.Add('## Unit Test Results')
    $lines.Add('')
    $lines.Add('| Passed | Failed | Skipped |')
    $lines.Add('| ---: | ---: | ---: |')
    $lines.Add("| $($Result.PassedCount) | $($Result.FailedCount) | $($Result.SkippedCount) |")
    $lines.Add('')

    $coverage = $Result.CodeCoverage
    if ($null -ne $coverage)
    {
        $lines.Add('## Code Coverage')
        $lines.Add('')
        $lines.Add("**$([System.Math]::Round($coverage.CoveragePercent, 2))%** of $($coverage.CommandsAnalyzedCount) commands covered.")
        $lines.Add('')
        $lines.Add('| File | Covered | Missed |')
        $lines.Add('| :--- | ---: | ---: |')

        $perFile = @{}
        foreach ($command in @($coverage.CommandsExecuted) + @($coverage.CommandsMissed))
        {
            if ($null -eq $command)
            {
                continue
            }

            if (-not $perFile.ContainsKey($command.File))
            {
                $perFile[$command.File] = @{ Analyzed = 0; Missed = 0 }
            }

            $perFile[$command.File].Analyzed++
        }

        foreach ($command in @($coverage.CommandsMissed))
        {
            if ($null -eq $command)
            {
                continue
            }

            $perFile[$command.File].Missed++
        }

        foreach ($file in ($perFile.Keys | Sort-Object))
        {
            $analyzed = $perFile[$file].Analyzed
            $missed = $perFile[$file].Missed
            $percentage = [System.Math]::Round(($analyzed - $missed) / $analyzed * 100, 2)
            $lines.Add("| $(Split-Path -Path $file -Leaf) | $percentage% | $missed |")
        }

        $lines.Add('')
    }

    if ([System.String]::IsNullOrEmpty($Path))
    {
        $lines | ForEach-Object { Write-Host -Object $_ }
    }
    else
    {
        $lines | Out-File -FilePath $Path -Append -Encoding utf8
    }
}

Export-ModuleMember -Function Invoke-TestHarness, Write-TestHarnessSummary
