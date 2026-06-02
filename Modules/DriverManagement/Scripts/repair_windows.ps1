# repair_windows.ps1
# Runs system integrity checks and Windows Update reset operations.
# Requires elevation.

$ErrorActionPreference = 'Stop'

function Run-Sfc {
    Write-Output "=== Running SFC /scannow ==="
    sfc /scannow | Out-String
}

function Run-DISM {
    Write-Output "=== Running DISM RestoreHealth ==="
    DISM.exe /Online /Cleanup-Image /RestoreHealth | Out-String
}

function Reset-WindowsUpdate {
    Write-Output "=== Resetting Windows Update components ==="
    net stop wuauserv | Out-String
    net stop bits | Out-String
    if (Test-Path "C:\Windows\SoftwareDistribution") { Remove-Item -Recurse -Force "C:\Windows\SoftwareDistribution" -ErrorAction SilentlyContinue }
    if (Test-Path "C:\Windows\System32\catroot2") { Remove-Item -Recurse -Force "C:\Windows\System32\catroot2" -ErrorAction SilentlyContinue }
    net start bits | Out-String
    net start wuauserv | Out-String
}

try {
    $outSfc = Run-Sfc
    Write-Output $outSfc

    $outDism = Run-DISM
    Write-Output $outDism

    $outWU = Reset-WindowsUpdate
    Write-Output $outWU

    exit 0
} catch {
    Write-Error $_.Exception.Message
    exit 1
}
