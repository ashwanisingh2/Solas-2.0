# Resets Windows Update components.
# Requires elevation.

$ErrorActionPreference = 'Stop'

function Stop-ServiceIfExists {
    param([string]$Name)
    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -ne 'Stopped') {
        Write-Output "Stopping $Name..."
        Stop-Service -Name $Name -Force -ErrorAction SilentlyContinue
    }
}

function Start-ServiceIfExists {
    param([string]$Name)
    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($svc) {
        Write-Output "Starting $Name..."
        Start-Service -Name $Name -ErrorAction SilentlyContinue
    }
}

Write-Output "=== Resetting Windows Update components ==="
Stop-ServiceIfExists -Name "wuauserv"
Stop-ServiceIfExists -Name "bits"
Stop-ServiceIfExists -Name "cryptsvc"

$softwareDistribution = Join-Path $env:SystemRoot "SoftwareDistribution"
$catroot2 = Join-Path $env:SystemRoot "System32\catroot2"

if (Test-Path $softwareDistribution) {
    $backup = "$softwareDistribution.old"
    if (Test-Path $backup) { Remove-Item -LiteralPath $backup -Recurse -Force -ErrorAction SilentlyContinue }
    Rename-Item -LiteralPath $softwareDistribution -NewName "SoftwareDistribution.old" -ErrorAction SilentlyContinue
    Write-Output "Renamed SoftwareDistribution."
}

if (Test-Path $catroot2) {
    $backup = "$catroot2.old"
    if (Test-Path $backup) { Remove-Item -LiteralPath $backup -Recurse -Force -ErrorAction SilentlyContinue }
    Rename-Item -LiteralPath $catroot2 -NewName "catroot2.old" -ErrorAction SilentlyContinue
    Write-Output "Renamed catroot2."
}

Start-ServiceIfExists -Name "cryptsvc"
Start-ServiceIfExists -Name "bits"
Start-ServiceIfExists -Name "wuauserv"

Write-Output "Windows Update reset completed."
exit 0
