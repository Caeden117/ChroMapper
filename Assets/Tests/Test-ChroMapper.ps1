#Requires -PSEdition Core
#Requires -Version 7.0
<#
.SYNOPSIS
Runs the ChroMapper PlayMode and EditMode tests in Unity batch mode and prints only failures.

.DESCRIPTION
Uses the Unity version declared by the project and the same test-runner arguments
as Jenkins build 981, except for its Linux-only xvfb-run wrapper. PlayMode and
EditMode run separately so Unity discovers both test platforms. The complete Unity
logs and NUnit XML results are retained in a timestamped directory, while the console
receives only failed test details and concise platform and aggregate summaries. Unity
is located through an explicit parameter, UNITY_EDITOR_PATH/UNITY_PATH, a matching
Unity Hub installation, or the executable search path.

.PARAMETER TestFilter
Optionally limits the run to a Unity Test Framework test-name filter.

.PARAMETER IncludeManual
Includes the ManualTests assembly, which is excluded by default.

.PARAMETER UnityExe
Overrides automatic Unity executable discovery.

.PARAMETER ProjectPath
Overrides the ChroMapper Unity project directory. By default, the script's own
directory is used.
#>

param(
    [string]$TestFilter,

    [switch]$IncludeManual,

    [string]$UnityExe,

    [string]$ProjectPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = "$PSScriptRoot\..\.."
}

$RunTimestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$OutputDirectory = Join-Path $ProjectPath "TestResults/cli/$RunTimestamp"

$TestAssemblies = if ($IncludeManual) {
    "Tests;ManualTests"
}
else {
    "Tests"
}

if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    throw "ChroMapper project not found: $ProjectPath"
}

$ProjectVersionFile = Join-Path $ProjectPath "ProjectSettings/ProjectVersion.txt"
if (-not (Test-Path -LiteralPath $ProjectVersionFile -PathType Leaf)) {
    throw "Unity project version file not found: $ProjectVersionFile"
}

$ProjectVersionLine = Get-Content -LiteralPath $ProjectVersionFile |
    Where-Object { $_ -match '^m_EditorVersion:\s*(?<Version>\S+)' } |
    Select-Object -First 1

if ($null -eq $ProjectVersionLine) {
    throw "Could not read m_EditorVersion from: $ProjectVersionFile"
}

$ProjectVersion = [regex]::Match($ProjectVersionLine, '^m_EditorVersion:\s*(?<Version>\S+)').Groups["Version"].Value

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $UnityEnvironmentCandidates = @($env:UNITY_EDITOR_PATH, $env:UNITY_PATH) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $UnityExe = $UnityEnvironmentCandidates |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $UnityHubCandidates = @()

    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $UnityHubCandidates += Join-Path $env:ProgramFiles "Unity/Hub/Editor/$ProjectVersion/Editor/Unity.exe"
    }

    if (-not [string]::IsNullOrWhiteSpace($env:HOME)) {
        $UnityHubCandidates += Join-Path $env:HOME "Unity/Hub/Editor/$ProjectVersion/Editor/Unity"
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITYHUB_EDITORS_FOLDER_LOCATION)) {
        $UnityHubRoot = $env:UNITYHUB_EDITORS_FOLDER_LOCATION
        $UnityHubCandidates += Join-Path $UnityHubRoot "$ProjectVersion/Editor/Unity.exe"
        $UnityHubCandidates += Join-Path $UnityHubRoot "$ProjectVersion/Editor/Unity"
        $UnityHubCandidates += Join-Path $UnityHubRoot "$ProjectVersion/Unity.app/Contents/MacOS/Unity"
    }

    $UnityHubCandidates += "/Applications/Unity/Hub/Editor/$ProjectVersion/Unity.app/Contents/MacOS/Unity"
    $UnityHubCandidates += "/opt/unity/Editor/Unity"
    $UnityExe = $UnityHubCandidates |
        Select-Object -Unique |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    foreach ($UnityCommandName in @("Unity", "unity-editor", "unity")) {
        $UnityCommand = Get-Command -Name $UnityCommandName -CommandType Application -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($null -ne $UnityCommand) {
            $UnityExe = $UnityCommand.Source
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($UnityExe) -or -not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw "Unity $ProjectVersion was not found. Install it with Unity Hub, pass -UnityExe, or set UNITY_EDITOR_PATH/UNITY_PATH."
}

$EditorInstanceFile = Join-Path $ProjectPath "Library/EditorInstance.json"
$EditorInstance = $null
if (Test-Path -LiteralPath $EditorInstanceFile -PathType Leaf) {
    try {
        $EditorInstance = Get-Content -LiteralPath $EditorInstanceFile -Raw | ConvertFrom-Json
    }
    catch {
        $EditorInstance = $null
    }
}

if ($null -ne $EditorInstance -and $null -ne $EditorInstance.process_id) {
    $OpenEditorProcess = Get-Process -Id ([int]$EditorInstance.process_id) -ErrorAction SilentlyContinue
    if ($null -ne $OpenEditorProcess -and $OpenEditorProcess.ProcessName -like "Unity*") {
        throw "ChroMapper is already open in Unity $($EditorInstance.version) (process $($EditorInstance.process_id)). Close that editor before running CLI tests."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

# BeatmapV2Test.GetFromJson was silently excluded by the PlayMode-only runner, so execute and report each Unity test
# platform independently while retaining the existing concise failure diagnostics and orphan cleanup behavior.
function Invoke-ChroMapperTestPlatform {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("playmode", "editmode")]
        [string]$TestPlatform
    )

    # The explicit phase announcement prevents a platform summary from being mistaken for the completed two-platform run.
    $PlatformLabel = if ($TestPlatform -eq "editmode") { "EditMode" } else { "PlayMode" }
    Write-Host ""
    Write-Host "Starting $PlatformLabel tests..." -ForegroundColor Cyan

    $LogFile = Join-Path $OutputDirectory "$TestPlatform-unity.log"
    $TestResultsFile = Join-Path $OutputDirectory "$TestPlatform-results.xml"

    # EditMode tests live in their own assembly, while IncludeManual extends only the PlayMode assembly selection.
    $PlatformTestAssemblies = if ($TestPlatform -eq "editmode") {
        "TestsEditMode"
    }
    else {
        $TestAssemblies
    }

    $UnityArguments = @(
        "-runTests",
        "-projectPath", $ProjectPath,
        "-batchmode",
        "-logFile", $LogFile,
        "-testResults", $TestResultsFile,
        "-testPlatform", $TestPlatform,
        "-assemblyNames", $PlatformTestAssemblies
    )

    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $UnityArguments += @("-testFilter", $TestFilter)
    }

    $UnityProcess = Start-Process -FilePath $UnityExe -ArgumentList $UnityArguments -PassThru -NoNewWindow
    $UnityExitCode = $null

    # Unity can outlive its logged batch completion, so close only the process started for this platform.
    while (-not $UnityProcess.HasExited) {
        Start-Sleep -Seconds 1

        if (Test-Path -LiteralPath $LogFile -PathType Leaf) {
            $CompletionLine = Get-Content -LiteralPath $LogFile -Tail 80 |
                Select-String -Pattern "Application will terminate with return code (?<ExitCode>-?\d+)" |
                Select-Object -Last 1

            if ($null -ne $CompletionLine) {
                $UnityExitCode = [int]$CompletionLine.Matches[0].Groups["ExitCode"].Value
                Start-Sleep -Seconds 2
                $UnityProcess.Refresh()

                if (-not $UnityProcess.HasExited) {
                    Stop-Process -Id $UnityProcess.Id -Force
                    $UnityProcess.WaitForExit()
                }

                break
            }
        }

        $UnityProcess.Refresh()
    }

    if ($null -eq $UnityExitCode) {
        $UnityProcess.WaitForExit()
        $UnityExitCode = $UnityProcess.ExitCode
    }

    if (-not (Test-Path -LiteralPath $TestResultsFile -PathType Leaf)) {
        Write-Host ""
        Write-Host "${TestPlatform}: Unity did not create a test-results file." -ForegroundColor Red
        if (Test-Path -LiteralPath $LogFile -PathType Leaf) {
            $InfrastructureErrors = @(Select-String -LiteralPath $LogFile -Pattern "error CS|Scripts have compiler errors|Aborting batchmode|Exception|Test run failed|Crash!!!|crash report|fatal error" -CaseSensitive:$false)
            if ($InfrastructureErrors.Count -gt 0) {
                $InfrastructureErrors | ForEach-Object { Write-Host $_.Line }
            }
            else {
                Get-Content -LiteralPath $LogFile -Tail 30
            }
        }

        return [pscustomobject]@{
            Platform = $TestPlatform
            Passed = 0
            Failed = 1
            Skipped = 0
            ExitCode = if ($UnityExitCode -ne 0) { $UnityExitCode } else { 1 }
        }
    }

    [xml]$TestResults = Get-Content -LiteralPath $TestResultsFile -Raw
    $FailedCases = @($TestResults.SelectNodes("//test-case[@result='Failed']"))
    $FailedSuites = @($TestResults.SelectNodes("//test-suite[@result='Failed' and failure and not(.//test-case[@result='Failed'])]"))

    foreach ($FailedCase in $FailedCases) {
        Write-Host ""
        Write-Host "$TestPlatform FAILED: $($FailedCase.fullname)" -ForegroundColor Red

        $MessageNode = $FailedCase.SelectSingleNode("failure/message")
        if ($null -ne $MessageNode -and -not [string]::IsNullOrWhiteSpace($MessageNode.InnerText)) {
            Write-Host $MessageNode.InnerText.Trim()
        }

        $OutputNode = $FailedCase.SelectSingleNode("output")
        $CapturedOutput = $null
        if ($null -ne $OutputNode -and -not [string]::IsNullOrWhiteSpace($OutputNode.InnerText)) {
            $CapturedOutput = $OutputNode.InnerText.TrimEnd()
            Write-Host ""
            Write-Host "Captured output:"
            Write-Host $CapturedOutput
        }

        # Avoid duplicating stacks already embedded in captured output.
        $StackTraceNode = $FailedCase.SelectSingleNode("failure/stack-trace")
        $StackTrace = if ($null -ne $StackTraceNode) {
            $StackTraceNode.InnerText.Trim()
        }
        else {
            $null
        }

        if (-not [string]::IsNullOrWhiteSpace($StackTrace) -and
            ($null -eq $CapturedOutput -or -not $CapturedOutput.Contains($StackTrace))) {
            Write-Host ""
            Write-Host $StackTrace
        }
    }

    # Setup failures may exist only at suite level.
    foreach ($FailedSuite in $FailedSuites) {
        Write-Host ""
        Write-Host "$TestPlatform FAILED SUITE: $($FailedSuite.fullname)" -ForegroundColor Red
        $SuiteMessageNode = $FailedSuite.SelectSingleNode("failure/message")
        $SuiteStackTraceNode = $FailedSuite.SelectSingleNode("failure/stack-trace")

        if ($null -ne $SuiteMessageNode -and -not [string]::IsNullOrWhiteSpace($SuiteMessageNode.InnerText)) {
            Write-Host $SuiteMessageNode.InnerText.Trim()
        }

        if ($null -ne $SuiteStackTraceNode -and -not [string]::IsNullOrWhiteSpace($SuiteStackTraceNode.InnerText)) {
            Write-Host ""
            Write-Host $SuiteStackTraceNode.InnerText.Trim()
        }
    }

    $TestRun = $TestResults.SelectSingleNode("/test-run")
    $ResultExitCode = if ($FailedCases.Count -gt 0 -or $FailedSuites.Count -gt 0) {
        1
    }
    elseif ($UnityExitCode -ne 0) {
        $UnityExitCode
    }
    else {
        0
    }

    Write-Host ""
    Write-Host "$TestPlatform result: $($TestRun.passed) passed, $($TestRun.failed) failed, $($TestRun.skipped) skipped." -ForegroundColor $(
        if ($ResultExitCode -ne 0) { "Red" } else { "Green" }
    )

    return [pscustomobject]@{
        Platform = $TestPlatform
        Passed = [int]$TestRun.passed
        Failed = [int]$TestRun.failed
        Skipped = [int]$TestRun.skipped
        ExitCode = $ResultExitCode
    }
}

# Run both platforms even when one reports test failures so a single invocation retains all actionable evidence.
$PlatformResults = @(
    Invoke-ChroMapperTestPlatform -TestPlatform "playmode"
    Invoke-ChroMapperTestPlatform -TestPlatform "editmode"
)
$TotalPassed = ($PlatformResults | Measure-Object -Property Passed -Sum).Sum
$TotalFailed = ($PlatformResults | Measure-Object -Property Failed -Sum).Sum
$TotalSkipped = ($PlatformResults | Measure-Object -Property Skipped -Sum).Sum
$FinalExitCode = if (@($PlatformResults | Where-Object { $_.ExitCode -ne 0 }).Count -gt 0) { 1 } else { 0 }

Write-Host ""
Write-Host "Result: $TotalPassed passed, $TotalFailed failed, $TotalSkipped skipped." -ForegroundColor $(
    if ($FinalExitCode -ne 0) { "Red" } else { "Green" }
)
Write-Host "Artifacts: $OutputDirectory"

exit $FinalExitCode
