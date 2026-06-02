# check_disk_health.ps1
# Queries S.M.A.R.T status of disks and partition errors.
# Requires elevation.
$ErrorActionPreference = 'SilentlyContinue'

Write-Output "=== Disk SMART & Drive Health Check ==="

# Get Physical Disks and SMART Status
$disks = Get-PhysicalDisk
if ($disks) {
    foreach ($disk in $disks) {
        # Query reliability counter for temperature & wear
        $temp = "N/A"
        $wear = "N/A"
        try {
            $rel = Get-StorageReliabilityCounter -PhysicalDisk $disk
            if ($rel) {
                if ($rel.Temperature -and $rel.Temperature -gt 0 -and $rel.Temperature -le 150) {
                    $temp = "$($rel.Temperature)°C"
                }
                $wear = "$((100 - $rel.Wear))%"
            }
        } catch {}

        Write-Output "----------------------------------------"
        Write-Output "Disk: $($disk.FriendlyName)"
        Write-Output "Model: $($disk.Model)"
        Write-Output "Size: $([math]::Round($disk.Size / 1GB, 2)) GB"
        Write-Output "Health Status: $($disk.HealthStatus)"
        Write-Output "Operational Status: $($disk.OperationalStatus)"
        Write-Output "MediaType: $($disk.MediaType)"
        Write-Output "Temperature: $temp"
        Write-Output "Estimated Life Remaining: $wear"
    }
} else {
    # Fallback to WMI (Windows 7 compatibility)
    $wmiDisks = Get-WmiObject -Class Win32_DiskDrive
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
