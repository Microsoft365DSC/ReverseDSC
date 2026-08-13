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

    return $results
}
