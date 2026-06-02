# Creates a Windows System Restore Point.
# Requires elevation.

$ErrorActionPreference = 'Stop'

Write-Output "=== Creating System Restore Point ==="

try {
    # Bypass 24-hour limit registry check
    $registryPath = "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\SystemRestore"
    if (Test-Path $registryPath) {
        Set-ItemProperty -Path $registryPath -Name "SystemRestorePointCreationFrequency" -Value 0 -ErrorAction SilentlyContinue
    }

    # Enable restore point on C: drive
    Enable-ComputerRestorePoint -Drive "C:\" -ErrorAction SilentlyContinue

    # Create restore point
    Checkpoint-Computer -Description "Solas Pre-Repair Restore Point" -RestorePointType "DEVICE_DRIVER_INSTALL" -Confirm:$false
    
    Write-Output "System Restore Point 'Solas Pre-Repair Restore Point' created successfully."
    exit 0
} catch {
    Write-Error "Failed to create restore point: $_"
    exit 1
}
