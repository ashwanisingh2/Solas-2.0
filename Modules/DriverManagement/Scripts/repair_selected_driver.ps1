# repair_selected_driver.ps1
# Resets and repairs a specific device driver by its PnpDeviceId.
# Requires elevation.
param(
    [string]$PnpDeviceId,
    [bool]$SafeMode = $true
)

$ErrorActionPreference = 'Continue'

if ([string]::IsNullOrWhiteSpace($PnpDeviceId)) {
    Write-Error "No PnpDeviceId provided to repair."
    exit 1
}

Write-Output "=== Repairing Specific Device Driver ==="
Write-Output "Target Device ID: $PnpDeviceId"

$SafeId = $PnpDeviceId -replace '[^a-zA-Z0-9]', '_'
$backupFile = "$env:TEMP\solas_driver_backup_$SafeId.reg"
$regKey = "HKLM\System\CurrentControlSet\Enum\$PnpDeviceId"
$osVersion = [System.Environment]::OSVersion.Version

# 1. Back up registry key first
Write-Output "[SYSTEM] Exporting driver registry backup to $backupFile ..."
if (Test-Path "Registry::$regKey") {
    reg.exe export $regKey $backupFile /y | Out-Null
} else {
    Write-Output "[WARNING] Registry key HKLM\System\CurrentControlSet\Enum\$PnpDeviceId not found."
}

# Verify backup was created and is not empty if SafeMode is enabled
$backupOk = $false
if (Test-Path $backupFile) {
    $item = Get-Item $backupFile
    if ($item.Length -gt 0) { $backupOk = $true }
}

if (-not $backupOk -and $SafeMode) {
    Write-Error "Safe Mode Abort: Registry backup failed. Operation canceled to prevent system instability."
    exit 1
}

# Step 1: Disable the device
Write-Output ""
Write-Output "[1/3] Disabling device..."
try {
    if ($osVersion.Major -ge 10) {
        pnputil.exe /disable-device $PnpDeviceId | Out-Null
    } else {
        Disable-PnpDevice -InstanceId $PnpDeviceId -Confirm:$false -ErrorAction Stop | Out-Null
    }
    Write-Output "Device disabled successfully."
} catch {
    Write-Output "Notice: Could not disable device (it may be a core system device). Proceeding..."
}

Start-Sleep -Seconds 2

# Step 2: Enable the device
Write-Output ""
Write-Output "[2/3] Enabling device..."
try {
    if ($osVersion.Major -ge 10) {
        pnputil.exe /enable-device $PnpDeviceId | Out-Null
    } else {
        Enable-PnpDevice -InstanceId $PnpDeviceId -Confirm:$false -ErrorAction Stop | Out-Null
    }
    Write-Output "Device enabled successfully. Driver reload forced."
} catch {
    Write-Error "Failed to enable device: $_"
    exit 1
}

# Step 3: Scan for hardware changes
Write-Output ""
Write-Output "[3/3] Scanning for hardware changes..."
pnputil /scan-devices | Out-Null
Write-Output "Scan complete."

Write-Output ""
Write-Output "=== Specific device driver reset completed. ==="
exit 0
