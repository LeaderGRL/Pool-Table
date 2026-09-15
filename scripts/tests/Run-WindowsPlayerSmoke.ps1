param(
    [string]$UnityEditorPath,
    [string]$ResultsDirectory,
    [ValidateRange(5, 300)]
    [int]$PlayerTimeoutSeconds = 60
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
        return (Resolve-Path $RequestedPath -ErrorAction Stop).Path
    }

    if ($env:UNITY_EDITOR_PATH) {
        return (Resolve-Path $env:UNITY_EDITOR_PATH -ErrorAction Stop).Path
    }

    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$Version\Editor\Unity.exe",
        "C:\Program Files (x86)\Unity\Hub\Editor\$Version\Editor\Unity.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate -PathType Leaf) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Unity $Version was not found. Pass -UnityEditorPath or set UNITY_EDITOR_PATH."
}

function Invoke-WindowsPlayerBuild {
    param(
        [string]$EditorPath,
        [string]$ExecutablePath,
        [string]$LogPath
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $EditorPath
    $startInfo.UseShellExecute = $false
    $startInfo.Environment["POOLTABLE_WINDOWS_BUILD_PATH"] = $ExecutablePath

    foreach ($argument in @(
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath", $repoRoot,
        "-executeMethod", "PoolTable.Editor.WindowsPlayerBuild.BuildDevelopmentPlayer",
        "-logFile", $LogPath
    )) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $process.WaitForExit()
    $exitCode = $process.ExitCode
    $process.Dispose()

    if ($exitCode -ne 0) {
        throw "Unity Windows build failed with exit code $exitCode. See $LogPath."
    }

    if (-not (Test-Path $ExecutablePath -PathType Leaf)) {
        throw "Unity did not produce $ExecutablePath. See $LogPath."
    }
}

function Invoke-PlayerSmoke {
    param(
        [string]$ExecutablePath,
        [string]$ReportPath,
        [string]$LogPath,
        [int]$TimeoutSeconds
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $ExecutablePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Environment["POOLTABLE_PLAYER_SMOKE_REPORT"] = $ReportPath

    foreach ($argument in @(
        "-batchmode",
        "-nographics",
        "-logFile", $LogPath,
        "-pooltable-player-smoke"
    )) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill($true)
        $process.WaitForExit()
        $process.Dispose()
        throw "Windows player smoke timed out after $TimeoutSeconds seconds. See $LogPath."
    }

    $exitCode = $process.ExitCode
    $process.Dispose()

    if (Test-Path $LogPath -PathType Leaf) {
        $playerLog = Get-Content $LogPath -Raw
        $runtimeFailurePattern = '(?im)^(?:[A-Za-z_][A-Za-z0-9_.]*Exception:|Unhandled Exception:|Assertion failed|Error:)'
        if ($playerLog -match $runtimeFailurePattern) {
            throw "Windows player log contains a runtime exception or error. See $LogPath."
        }
    }

    if (-not (Test-Path $ReportPath -PathType Leaf)) {
        throw "Windows player did not produce $ReportPath. Exit code: $exitCode. See $LogPath."
    }

    $report = Get-Content $ReportPath -Raw | ConvertFrom-Json
    $requiredChecks = @(
        "Startup",
        "InputSystem",
        "LocalControls",
        "ShotStateEntered",
        "SpectateStateEntered",
        "ReturnedToPlay",
        "ShotFlow",
        "BallInHand",
        "CleanShutdownRequested"
    )

    foreach ($check in $requiredChecks) {
        if (-not $report.$check) {
            throw "Windows player smoke check '$check' failed. See $ReportPath and $LogPath."
        }
    }

    if ($exitCode -ne 0 -or -not $report.Success) {
        throw "Windows player smoke failed. Exit code: $exitCode. Error: $($report.Error). See $ReportPath and $LogPath."
    }

    Write-Host "Windows player smoke passed. Scene: $($report.SceneName), balls: $($report.BallCount), report: $ReportPath"
}

$unityVersion = Get-ProjectUnityVersion
$editorPath = Resolve-UnityEditor -RequestedPath $UnityEditorPath -Version $unityVersion
$outputDirectory = if ($ResultsDirectory) {
    [System.IO.Path]::GetFullPath($ResultsDirectory)
}
else {
    Join-Path $repoRoot "Logs/WindowsPlayerSmoke"
}

$buildDirectory = Join-Path $outputDirectory "Build"
$executablePath = Join-Path $buildDirectory "PoolTable.exe"
$buildLogPath = Join-Path $outputDirectory "Build.log"
$playerLogPath = Join-Path $outputDirectory "Player.log"
$reportPath = Join-Path $outputDirectory "player-smoke.json"

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
if (Test-Path $buildDirectory) {
    Remove-Item $buildDirectory -Recurse -Force
}
Remove-Item $reportPath -Force -ErrorAction SilentlyContinue

Invoke-WindowsPlayerBuild -EditorPath $editorPath -ExecutablePath $executablePath -LogPath $buildLogPath
Invoke-PlayerSmoke `
    -ExecutablePath $executablePath `
    -ReportPath $reportPath `
    -LogPath $playerLogPath `
    -TimeoutSeconds $PlayerTimeoutSeconds
