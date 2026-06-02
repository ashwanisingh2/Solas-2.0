# repair_windows.ps1
# Runs common Windows repair operations.
# Requires elevation.

$ErrorActionPreference = 'Stop'

function Run-Step {
    param([string]$ScriptName)
    $scriptPath = Join-Path $PSScriptRoot $ScriptName
    if (-not (Test-Path $scriptPath)) {
        throw "Missing repair script: $scriptPath"
    }

    Write-Output ""
    Write-Output "=== Running $ScriptName ==="
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $scriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "$ScriptName failed with exit code $LASTEXITCODE"
    }
}

try {
    Run-Step "repair_sfc.ps1"
    Run-Step "repair_dism.ps1"
    Run-Step "repair_windows_update.ps1"
    Run-Step "repair_dns.ps1"
    Write-Output ""
    Write-Output "Full repair completed. Restart Windows if SFC, DISM, Windows Update, or network components were repaired."
    exit 0
} catch {
    Write-Error $_.Exception.Message
    exit 1
}
