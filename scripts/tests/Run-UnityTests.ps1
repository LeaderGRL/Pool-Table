param(
    [ValidateSet("EditMode", "PlayMode", "All")]
    [string]$TestPlatform = "All",
    [string]$Category,
    [string]$UnityEditorPath,
    [string]$ResultsDirectory
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$projectVersionPath = Join-Path $repoRoot "ProjectSettings/ProjectVersion.txt"

function Get-ProjectUnityVersion {
    $versionLine = Get-Content $projectVersionPath | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
    if (-not $versionLine) {
        throw "Unable to read m_EditorVersion from $projectVersionPath."
    }

    return ($versionLine -split ":", 2)[1].Trim()
}

function Resolve-UnityEditor {
    param([string]$RequestedPath, [string]$Version)

    if ($RequestedPath) {
        $resolved = Resolve-Path $RequestedPath -ErrorAction Stop
        return $resolved.Path
    }

    if ($env:UNITY_EDITOR_PATH) {
        $resolved = Resolve-Path $env:UNITY_EDITOR_PATH -ErrorAction Stop
        return $resolved.Path
    }

    $candidates = @()
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        $candidates += "C:\Program Files\Unity\Hub\Editor\$Version\Editor\Unity.exe"
        $candidates += "C:\Program Files (x86)\Unity\Hub\Editor\$Version\Editor\Unity.exe"
    }
    elseif ($IsMacOS) {
        $candidates += "/Applications/Unity/Hub/Editor/$Version/Unity.app/Contents/MacOS/Unity"
    }
    elseif ($IsLinux) {
        $candidates += "$HOME/Unity/Hub/Editor/$Version/Editor/Unity"
    }

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate -PathType Leaf) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Unity $Version was not found. Pass -UnityEditorPath or set UNITY_EDITOR_PATH."
}

function Invoke-UnityTestPlatform {
    param(
        [string]$EditorPath,
        [string]$Platform,
        [string]$OutputDirectory,
        [string]$ScenarioDirectory,
        [string]$CategoryFilter
    )

    $resultPath = Join-Path $OutputDirectory "$Platform-results.xml"
    $logPath = Join-Path $OutputDirectory "$Platform.log"
    Remove-Item $resultPath -Force -ErrorAction SilentlyContinue

    $arguments = @(
        "-batchmode",
        "-nographics",
        "-projectPath", $repoRoot,
        "-runTests",
        "-testPlatform", $Platform,
        "-testResults", $resultPath,
        "-logFile", $logPath
    )

    if ($CategoryFilter) {
        $arguments += @("-testCategory", $CategoryFilter)
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $EditorPath
    $startInfo.UseShellExecute = $false
    $startInfo.Environment["POOLTABLE_SCENARIO_REPORT_DIR"] = $ScenarioDirectory
    foreach ($argument in $arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $process.WaitForExit()
    $unityExitCode = $process.ExitCode
    $process.Dispose()

    if (-not (Test-Path $resultPath -PathType Leaf)) {
        throw "Unity did not produce $resultPath. Exit code: $unityExitCode. See $logPath."
    }

    [xml]$testResults = Get-Content $resultPath -Raw
    $testRun = $testResults.'test-run'
    if ($null -eq $testRun) {
        throw "Unity produced an invalid test result file at $resultPath."
    }

    $failed = [int]$testRun.failed
    $passed = [int]$testRun.passed
    $skipped = [int]$testRun.skipped
    if ($unityExitCode -ne 0 -or $failed -ne 0 -or $testRun.result -ne "Passed") {
        throw "$Platform tests failed. Passed: $passed, failed: $failed, skipped: $skipped, Unity exit code: $unityExitCode. See $resultPath and $logPath."
    }

    Write-Host "$Platform tests passed. Passed: $passed, skipped: $skipped. Results: $resultPath"
}

$unityVersion = Get-ProjectUnityVersion
$editorPath = Resolve-UnityEditor -RequestedPath $UnityEditorPath -Version $unityVersion
$outputDirectory = if ($ResultsDirectory) {
    [System.IO.Path]::GetFullPath($ResultsDirectory)
}
else {
    Join-Path $repoRoot "Logs/TestResults"
}
$scenarioDirectory = Join-Path $outputDirectory "Scenarios"
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $scenarioDirectory -Force | Out-Null

$platforms = if ($TestPlatform -eq "All") { @("EditMode", "PlayMode") } else { @($TestPlatform) }
foreach ($platform in $platforms) {
    Invoke-UnityTestPlatform `
        -EditorPath $editorPath `
        -Platform $platform `
        -OutputDirectory $outputDirectory `
        -ScenarioDirectory $scenarioDirectory `
        -CategoryFilter $Category
}
