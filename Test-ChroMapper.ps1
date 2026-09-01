# PowerShell 7 is the supported automation runtime; Windows PowerShell 5.1 has incompatible legacy behavior.
#Requires -PSEdition Core
#Requires -Version 7.0
<#
.SYNOPSIS
Runs the ChroMapper playmode tests in Unity batch mode and prints only failures.

.DESCRIPTION
Uses the Unity version declared by the project and the same test-runner arguments
as Jenkins build 981, except for its Linux-only xvfb-run wrapper. The complete
Unity log and NUnit XML results are retained in a timestamped directory, while the
console receives only failed test details and a concise result summary. Unity is
located through an explicit parameter, UNITY_EDITOR_PATH/UNITY_PATH, a matching
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

# Jenkins failures must be reproducible without running unrelated build stages, so expose only test-specific overrides.
param(
    [string]$TestFilter,

    [switch]$IncludeManual,

    [string]$UnityExe,

    [string]$ProjectPath
)

# A failed test should not be obscured by PowerShell's permissive defaults, so make runner errors terminate consistently.
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Moving the runner into ChroMapper should make it portable with the checkout, so use its containing directory by default.
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = $PSScriptRoot
}

# Each run needs independent evidence without overwriting previous CI reproductions, so retain timestamped log and XML artifacts.
$RunTimestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$OutputDirectory = Join-Path $ProjectPath "TestResults/cli/$RunTimestamp"
$LogFile = Join-Path $OutputDirectory "unity.log"
$TestResultsFile = Join-Path $OutputDirectory "results.xml"

# Manual tests depend on developer-specific maps and settings, so keep them out of normal automated runs unless explicitly requested.
$TestAssemblies = if ($IncludeManual) {
    "Tests;ManualTests"
}
else {
    "Tests"
}

# The project path determines the required editor version, so validate it before resolving the Unity executable.
if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    throw "ChroMapper project not found: $ProjectPath"
}

# Cross-platform discovery needs the project-declared version before checking each operating system's Unity Hub layout.
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

# CI and custom installations can declare Unity directly, so prefer standard override environment variables before probing defaults.
if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    $UnityEnvironmentCandidates = @($env:UNITY_EDITOR_PATH, $env:UNITY_PATH) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $UnityExe = $UnityEnvironmentCandidates |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
}

# Unity Hub uses predictable versioned directories on each supported desktop OS, so probe all layouts without OS-specific syntax.
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

# Package-managed Linux installations may expose Unity only through PATH, so use executable discovery as the final fallback.
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

# A missing matching editor would otherwise look like a test-runner failure, so fail before creating run artifacts.
if ([string]::IsNullOrWhiteSpace($UnityExe) -or -not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
    throw "Unity $ProjectVersion was not found. Install it with Unity Hub, pass -UnityExe, or set UNITY_EDITOR_PATH/UNITY_PATH."
}

# Unity silently exits before creating results when this project is already open, so report the owning editor process explicitly.
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

# A stale EditorInstance file is harmless, so reject the run only when its recorded Unity process is still alive.
if ($null -ne $EditorInstance -and $null -ne $EditorInstance.process_id) {
    $OpenEditorProcess = Get-Process -Id ([int]$EditorInstance.process_id) -ErrorAction SilentlyContinue
    if ($null -ne $OpenEditorProcess -and $OpenEditorProcess.ProcessName -like "Unity*") {
        throw "ChroMapper is already open in Unity $($EditorInstance.version) (process $($EditorInstance.process_id)). Close that editor before running CLI tests."
    }
}

# Jenkins captures the complete Unity stream while presenting test failures separately, so create artifact storage before the run.
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

# Keep the test invocation aligned with Jenkins: runTests, projectPath, batchmode, testResults, then playmode.
$UnityArguments = @(
    "-runTests",
    "-projectPath", $ProjectPath,
    "-batchmode",
    "-logFile", $LogFile,
    "-testResults", $TestResultsFile,
    "-testPlatform", "playmode",
    "-assemblyNames", $TestAssemblies
)

# A targeted rerun should retain the same runner configuration, so append Unity's native filter only when requested.
if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $UnityArguments += @("-testFilter", $TestFilter)
}

# Unity's full batch log is intentionally redirected to disk so successful-test chatter does not flood the console.
$UnityProcess = Start-Process -FilePath $UnityExe -ArgumentList $UnityArguments -PassThru -NoNewWindow
$UnityExitCode = $null

# Unity can outlive its logged batch completion on some hosts, so trust its reported return code and close only that orphaned process.
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

# A normally exited Unity process has the authoritative native exit code when no logged completion line was observed.
if ($null -eq $UnityExitCode) {
    $UnityProcess.WaitForExit()
    $UnityExitCode = $UnityProcess.ExitCode
}

# Test results are authoritative even when Unity's process code is ambiguous, so parse every failed NUnit case from the XML.
if (Test-Path -LiteralPath $TestResultsFile -PathType Leaf) {
    [xml]$TestResults = Get-Content -LiteralPath $TestResultsFile -Raw
    $FailedCases = @($TestResults.SelectNodes("//test-case[@result='Failed']"))
    $FailedSuites = @($TestResults.SelectNodes("//test-suite[@result='Failed' and failure and not(.//test-case[@result='Failed'])]"))

    # Only failed cases belong in console output, including their captured test output when the runner supplied it.
    foreach ($FailedCase in $FailedCases) {
        Write-Host ""
        Write-Host "FAILED: $($FailedCase.fullname)" -ForegroundColor Red

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

        # NUnit often embeds the failure stack inside captured output, so avoid printing the identical stack twice.
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

    # Suite-level setup failures may have no failed test case, so surface those separately instead of silently reporting zero failures.
    foreach ($FailedSuite in $FailedSuites) {
        Write-Host ""
        Write-Host "FAILED SUITE: $($FailedSuite.fullname)" -ForegroundColor Red
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

    # The root NUnit counts provide a quiet success result and an unambiguous Jenkins-style failure summary.
    $TestRun = $TestResults.SelectSingleNode("/test-run")
    Write-Host ""
    Write-Host "Result: $($TestRun.passed) passed, $($TestRun.failed) failed, $($TestRun.skipped) skipped." -ForegroundColor $(
        if ([int]$TestRun.failed -gt 0) {
            "Red"
        }
        else {
            "Green"
        }
    )
    Write-Host "Artifacts: $OutputDirectory"

    # Failed NUnit cases must produce a failing shell status even if Unity itself returned zero.
    if ($FailedCases.Count -gt 0 -or $FailedSuites.Count -gt 0) {
        exit 1
    }
}
else {
    # A missing XML file means the test runner itself failed, so prefer actionable errors but retain a short log tail when Unity gives no diagnosis.
    Write-Host "Unity did not create a test-results file." -ForegroundColor Red
    if (Test-Path -LiteralPath $LogFile -PathType Leaf) {
        $InfrastructureErrors = @(Select-String -LiteralPath $LogFile -Pattern "error CS|Scripts have compiler errors|Aborting batchmode|Exception|Test run failed|Crash!!!|crash report|fatal error" -CaseSensitive:$false)
        if ($InfrastructureErrors.Count -gt 0) {
            $InfrastructureErrors | ForEach-Object { Write-Host $_.Line }
        }
        else {
            Get-Content -LiteralPath $LogFile -Tail 30
        }
    }

    Write-Host "Artifacts: $OutputDirectory"
    exit $(
        if ($UnityExitCode -ne 0) {
            $UnityExitCode
        }
        else {
            1
        }
    )
}

# A nonzero Unity result without NUnit failures indicates infrastructure trouble, so preserve that status after reporting the parsed summary.
if ($UnityExitCode -ne 0) {
    Write-Host "Unity exited with code $UnityExitCode. See $LogFile" -ForegroundColor Red
    exit $UnityExitCode
}

exit 0
