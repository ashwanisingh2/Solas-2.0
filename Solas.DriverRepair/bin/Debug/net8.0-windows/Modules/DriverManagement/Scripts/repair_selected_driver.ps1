# Resets and repairs a specific device driver by its PnpDeviceId.
# Requires elevation.

param(
    [string]$PnpDeviceId
)

$ErrorActionPreference = 'Continue'

if ([string]::IsNullOrWhiteSpace($PnpDeviceId)) {
    Write-Error "No PnpDeviceId provided to repair."
    exit 1
}

Write-Output "=== Repairing Specific Device Driver ==="
Write-Output "Target Device ID: $PnpDeviceId"

# Query the device
$device = Get-PnpDevice -InstanceId $PnpDeviceId -ErrorAction SilentlyContinue
if (-not $device) {
    Write-Error "Device not found with Instance ID: $PnpDeviceId"
    exit 1
}

Write-Output "Device Name: $($device.FriendlyName)"
Write-Output "Current Status: $($device.Status) (Problem: $($device.Problem))"

# Step 1: Disable the device
Write-Output ""
Write-Output "[1/3] Disabling device..."
try {
    Disable-PnpDevice -InstanceId $PnpDeviceId -Confirm:$false -ErrorAction Stop
    Write-Output "Device disabled successfully."
} catch {
    Write-Output "Notice: Could not disable device (it may be a core system device). Proceeding..."
}

Start-Sleep -Seconds 2

# Step 2: Enable the device
Write-Output ""
Write-Output "[2/3] Enabling device..."
try {
    Enable-PnpDevice -InstanceId $PnpDeviceId -Confirm:$false -ErrorAction Stop
    Write-Output "Device enabled successfully. Driver reload forced."
} catch {
    Write-Error "Failed to enable device: $_"
    exit 1
}

# Step 3: Scan for hardware changes
Write-Output ""
Write-Output "[3/3] Scanning for hardware changes..."
pnputil /scan-devices
Write-Output "Scan complete."

Write-Output ""
Write-Output "=== Specific device driver reset completed. ==="
exit 0
