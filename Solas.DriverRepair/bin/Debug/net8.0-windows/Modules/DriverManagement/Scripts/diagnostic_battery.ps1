# Diagnoses laptop battery health and power efficiency.
$ErrorActionPreference = 'Stop'

Write-Output "=== Battery Health & Power Diagnostics ==="

# Get Battery status using WMI
$battery = Get-WmiObject -Class Win32_Battery -ErrorAction SilentlyContinue
if ($battery) {
    Write-Output "Battery Name: $($battery.Name)"
    Write-Output "Status: $($battery.Status)"
    Write-Output "Estimated Charge Remaining: $($battery.EstimatedChargeRemaining)%"
    Write-Output "Estimated Run Time: $($battery.EstimatedRunTime) minutes"
} else {
    Write-Output "No battery detected (could be a desktop computer)."
}

# Run Battery Report
$reportPath = "$env:TEMP\batteryreport.xml"
Write-Output "Generating Windows Battery Report..."
powercfg /batteryreport /output "$reportPath" /xml | Out-Null

if (Test-Path $reportPath) {
    [xml]$xml = Get-Content $reportPath
    $designCapacity = $xml.BatteryReport.BatteryInfo.DesignCapacity
    $fullChargeCapacity = $xml.BatteryReport.BatteryInfo.FullChargeCapacity
    
    if ($designCapacity -and $fullChargeCapacity) {
        $health = [math]::Round(($fullChargeCapacity / $designCapacity) * 100, 2)
        Write-Output "Design Capacity: $designCapacity mWh"
        Write-Output "Full Charge Capacity: $fullChargeCapacity mWh"
        Write-Output "Battery Health Score: $health %"
        if ($health -lt 70) {
            Write-Output "WARNING: Battery health is below 70%. Consider replacing the battery soon."
        } else {
            Write-Output "Battery health is good!"
        }
    }
    Remove-Item $reportPath -Force -ErrorAction SilentlyContinue
} else {
    Write-Output "Failed to generate detailed powercfg battery report."
}
exit 0
