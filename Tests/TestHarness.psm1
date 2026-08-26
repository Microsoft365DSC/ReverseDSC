<#
.SYNOPSIS
    Builds ReverseDSC and runs both test suites.

.DESCRIPTION
    Builds the solution, stages the PowerShell module, runs the xUnit suite that covers the engine
    and then the Pester suite that covers the exported cmdlets. Returns the Pester result object
    with the xUnit counts attached, so a caller can decide what to do with failures.

.PARAMETER TestResultsFile
    File the Pester results are written to, in NUnit format.

.PARAMETER DscTestsPath
    Single test file to run instead of everything under Tests.

.PARAMETER Configuration
    Build configuration to use.

.PARAMETER SkipBuild
    Runs the tests against whatever is already built and staged.

.EXAMPLE
    Invoke-TestHarness -Configuration Release
#>
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
        [ValidateSet('Debug', 'Release')]
        [System.String]
        $Configuration = 'Release',

        [Parameter()]
        [Switch]
        $SkipBuild
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    Write-Host -Object 'Running all ReverseDSC Unit Tests'

    $repoDir = Join-Path -Path $PSScriptRoot -ChildPath '..\' -Resolve
    $resultsDir = Join-Path -Path $repoDir -ChildPath 'TestResults'

    if (-not $SkipBuild.IsPresent)
    {
        & (Join-Path -Path $repoDir -ChildPath 'Utilities\Build.ps1') -Configuration $Configuration -SkipClean
    }

    $unitTestResult = Invoke-CSharpTestSuite -RepositoryRoot $repoDir -Configuration $Configuration -ResultsDirectory $resultsDir -SkipBuild:$SkipBuild

    $modulePath = Join-Path -Path $repoDir -ChildPath 'ReverseDSC\ReverseDSC.psd1'
    if (-not (Test-Path -Path $modulePath))
    {
        throw "The ReverseDSC module has not been staged at '$modulePath'. Run Utilities\Build.ps1 first."
    }

    Import-Module -Name $modulePath -Force

    if ([System.String]::IsNullOrEmpty($DscTestsPath))
    {
        $getChildItemParameters = @{
            Path    = (Join-Path -Path $repoDir -ChildPath 'Tests')
            Recurse = $true
            Filter  = '*.Tests.ps1'
        }
        $filesToExecute = @((Get-ChildItem @getChildItemParameters).FullName)
    }
    else
    {
        $filesToExecute = @($DscTestsPath)
    }

    $Container = New-PesterContainer -Path $filesToExecute

    $pesterConfiguration = [PesterConfiguration]@{
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

    if ([System.String]::IsNullOrEmpty($TestResultsFile) -eq $false)
    {
        $pesterConfiguration.TestResult.Enabled = $true
        $pesterConfiguration.TestResult.OutputFormat = 'NUnitXml'
        $pesterConfiguration.TestResult.OutputPath = $TestResultsFile
    }

    $results = Invoke-Pester -Configuration $pesterConfiguration

    Add-Member -InputObject $results -MemberType NoteProperty -Name 'CSharpResult' -Value $unitTestResult -Force

    $message = 'Running the tests took {0} hours, {1} minutes, {2} seconds' -f $sw.Elapsed.Hours, $sw.Elapsed.Minutes, $sw.Elapsed.Seconds
    Write-Host -Object $message
    Write-Host -Object 'Completed running all ReverseDSC Unit Tests'

    Write-TestHarnessSummary -Result $results

    return $results
}

<#
.SYNOPSIS
    Runs the xUnit suite that covers the engine.

.DESCRIPTION
    Runs the compiled test executable through the Microsoft Testing Platform, producing a trx
    report and a Cobertura coverage report in the results directory. Returns an object holding the
    exit code and the two report paths.

.PARAMETER RepositoryRoot
    Root of the repository.

.PARAMETER Configuration
    Build configuration the test project was built in.

.PARAMETER ResultsDirectory
    Directory the reports are written to.

.PARAMETER SkipBuild
    Runs the executable that is already built instead of building it first.

.EXAMPLE
    Invoke-CSharpTestSuite -RepositoryRoot 'D:\ReverseDSC' -Configuration Release -ResultsDirectory 'D:\ReverseDSC\TestResults'
#>
function Invoke-CSharpTestSuite
{
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $RepositoryRoot,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Debug', 'Release')]
        [System.String]
        $Configuration,

        [Parameter(Mandatory = $true)]
        [System.String]
        $ResultsDirectory,

        [Parameter()]
        [Switch]
        $SkipBuild
    )

    $projectPath = Join-Path -Path $RepositoryRoot -ChildPath 'src\ReverseDSC.Tests\ReverseDSC.Tests.csproj'
    if (-not $SkipBuild.IsPresent)
    {
        & dotnet build $projectPath -c $Configuration --nologo
        if ($LASTEXITCODE -ne 0)
        {
            throw "Building the C# test project failed with exit code $LASTEXITCODE."
        }
    }

    $executable = Join-Path -Path $RepositoryRoot -ChildPath "src\ReverseDSC.Tests\bin\$Configuration\net10.0\ReverseDSC.Tests.exe"
    if (-not (Test-Path -Path $executable))
    {
        throw "The C# test executable was not found at '$executable'."
    }

    $arguments = @(
        '--results-directory', $ResultsDirectory
        '--report-trx', '--report-trx-filename', 'ReverseDSC.Tests.trx'
        '--coverage', '--coverage-output-format', 'cobertura', '--coverage-output', 'coverage.cobertura.xml'
    )

    Write-Host -Object 'Running the C# unit tests'
    & $executable @arguments
    $exitCode = $LASTEXITCODE

    return [PSCustomObject]@{
        ExitCode     = $exitCode
        TrxPath      = Join-Path -Path $ResultsDirectory -ChildPath 'ReverseDSC.Tests.trx'
        CoveragePath = Join-Path -Path $ResultsDirectory -ChildPath 'coverage.cobertura.xml'
    }
}

<#
.SYNOPSIS
    Renders the results of Invoke-TestHarness as a report.

.DESCRIPTION
    Writes the Pester test counts, the outcome of the C# suite and the line coverage of the
    Cobertura report. Without a path the report goes to the console, with a path it is appended as
    GitHub flavoured markdown, which makes it usable as a GitHub Actions step summary.

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
    $lines.Add('| Suite | Passed | Failed | Skipped |')
    $lines.Add('| :--- | ---: | ---: | ---: |')
    $lines.Add("| Pester | $($Result.PassedCount) | $($Result.FailedCount) | $($Result.SkippedCount) |")

    $cSharpResult = $Result.CSharpResult
    if ($null -ne $cSharpResult -and (Test-Path -Path $cSharpResult.TrxPath))
    {
        $trx = [xml](Get-Content -Path $cSharpResult.TrxPath -Raw)
        $counters = $trx.TestRun.ResultSummary.Counters
        $lines.Add("| xUnit | $($counters.passed) | $($counters.failed) | $([System.Int32]$counters.total - [System.Int32]$counters.executed) |")
    }

    $lines.Add('')

    if ($null -ne $cSharpResult -and (Test-Path -Path $cSharpResult.CoveragePath))
    {
        $coverage = [xml](Get-Content -Path $cSharpResult.CoveragePath -Raw)
        $lineRate = [System.Math]::Round([System.Double]$coverage.coverage.'line-rate' * 100, 2)
        $branchRate = [System.Math]::Round([System.Double]$coverage.coverage.'branch-rate' * 100, 2)

        $lines.Add('## Code Coverage')
        $lines.Add('')
        $lines.Add("**$lineRate%** of the lines and **$branchRate%** of the branches are covered.")
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
