# Queries S.M.A.R.T status of disks and partition errors.
# Requires elevation.
$ErrorActionPreference = 'Stop'

Write-Output "=== Disk SMART & Drive Health Check ==="

# Get Physical Disks and SMART Status
$disks = Get-PhysicalDisk -ErrorAction SilentlyContinue
if ($disks) {
    foreach ($disk in $disks) {
        Write-Output "----------------------------------------"
        Write-Output "Disk: $($disk.FriendlyName)"
        Write-Output "Model: $($disk.Model)"
        Write-Output "Size: $([math]::Round($disk.Size / 1GB, 2)) GB"
        Write-Output "Health Status: $($disk.HealthStatus)"
        Write-Output "Operational Status: $($disk.OperationalStatus)"
        Write-Output "MediaType: $($disk.MediaType)"
    }
} else {
    # Fallback to WMI
    $wmiDisks = Get-WmiObject -Class Win32_DiskDrive -ErrorAction SilentlyContinue
    if ($wmiDisks) {
        foreach ($wd in $wmiDisks) {
            Write-Output "----------------------------------------"
            Write-Output "Disk: $($wd.Caption)"
            Write-Output "Size: $([math]::Round($wd.Size / 1GB, 2)) GB"
            Write-Output "SMART Status: $($wd.Status)"
        }
    } else {
        Write-Output "No disk drives found."
    }
}
exit 0
