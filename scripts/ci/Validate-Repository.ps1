param(
    [string]$ExpectedUnityVersion = "6000.5.1f1"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path

Push-Location $repoRoot

try {
    $failures = [System.Collections.Generic.List[string]]::new()

    $trackedPaths = @(git ls-files)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to enumerate tracked files."
    }

    $trackedIgnoredPaths = @(git ls-files -ci --exclude-per-directory=.gitignore)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to check tracked files against ignore rules."
    }

    foreach ($path in $trackedIgnoredPaths) {
        $failures.Add("Tracked path is ignored by .gitignore: $path")
    }

    $forbiddenPathPatterns = @(
        '^(?i:Library|Temp|Obj|Logs|UserSettings|MemoryCaptures|Recordings)(/|$)',
        '^(?i:Build|Builds|Game|Web_Realease)(/|$)',
        '^\.vs(/|$)',
        '^\.vscode(/|$)',
        '^\.idea(/|$)',
        '(?i:\.(csproj|sln|suo|user|userprefs|pidb|booproj|opendb))$'
    )

    foreach ($path in $trackedPaths) {
        foreach ($pattern in $forbiddenPathPatterns) {
            if ($path -match $pattern) {
                $failures.Add("Generated or local-only path is tracked: $path")
                break
            }
        }
    }

    $projectVersionPath = Join-Path $repoRoot "ProjectSettings/ProjectVersion.txt"
    if (-not (Test-Path $projectVersionPath)) {
        $failures.Add("Missing ProjectSettings/ProjectVersion.txt.")
    }
    else {
        $projectVersion = Get-Content $projectVersionPath
        $expectedVersionLine = "m_EditorVersion: $ExpectedUnityVersion"

        if ($projectVersion -cnotcontains $expectedVersionLine) {
            $failures.Add("Unity editor version must remain $ExpectedUnityVersion.")
        }
    }

    $jsonFiles = @(
        "Packages/manifest.json",
        "Packages/packages-lock.json"
    )

    foreach ($relativePath in $jsonFiles) {
        $jsonPath = Join-Path $repoRoot $relativePath

        try {
            Get-Content $jsonPath -Raw | ConvertFrom-Json | Out-Null
        }
        catch {
            $failures.Add("Invalid JSON in ${relativePath}: $($_.Exception.Message)")
        }
    }

    git lfs version | Out-Null
    if ($LASTEXITCODE -ne 0) {
        $failures.Add("Git LFS is not available.")
    }
    else {
        git -c lfs.fetchexclude= lfs fsck --pointers HEAD
        if ($LASTEXITCODE -ne 0) {
            $failures.Add("Git LFS pointer validation failed for HEAD.")
        }

        git -c lfs.fetchexclude= lfs fsck --objects HEAD
        if ($LASTEXITCODE -ne 0) {
            $failures.Add("Git LFS object validation failed for HEAD.")
        }
    }

    if ($failures.Count -gt 0) {
        foreach ($failure in $failures) {
            [Console]::Error.WriteLine("ERROR: $failure")
        }

        exit 1
    }

    Write-Host "Repository validation passed."
    Write-Host "Unity version: $ExpectedUnityVersion"
    Write-Host "Tracked files checked: $($trackedPaths.Count)"
}
finally {
    Pop-Location
}
