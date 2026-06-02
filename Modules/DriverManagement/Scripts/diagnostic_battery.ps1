# diagnostic_battery.ps1
# Diagnoses laptop battery health and power efficiency.
# Requires elevation.
$ErrorActionPreference = 'SilentlyContinue'

Write-Output "=== Battery Health & Power Diagnostics ==="

# Get Battery status using WMI
$battery = Get-WmiObject -Class Win32_Battery -ErrorAction SilentlyContinue
if (-not $battery) {
    Write-Output "No battery detected (could be a desktop computer)."
    exit 0
}

Write-Output "Battery Name: $($battery.Name)"
Write-Output "Device ID: $($battery.DeviceID)"
Write-Output "Estimated Charge Remaining: $($battery.EstimatedChargeRemaining)%"
if ($battery.EstimatedRunTime -and $battery.EstimatedRunTime -lt 71582) {
    Write-Output "Estimated Run Time: $($battery.EstimatedRunTime) minutes"
}

# Run Battery Report to temp HTML file (supported everywhere, unlike XML)
$reportPath = "$env:TEMP\batteryreport.html"
if (Test-Path $reportPath) { Remove-Item $reportPath -Force }

Start-Process powercfg -ArgumentList "/batteryreport /output `"$reportPath`"" -NoNewWindow -Wait

$designCapacity = 0
$fullChargeCapacity = 0
$cycleCount = 0
$chemistry = "Unknown"
$parsedSuccessfully = $false

if (Test-Path $reportPath) {
    try {
        $html = Get-Content -Path $reportPath -Raw -Encoding UTF8
        
        # Design Capacity parsing
        if ($html -match 'DESIGN CAPACITY\s*</td>\s*<td>\s*([\d,]+)\s*mWh') {
            $designCapacity = [int]($Matches[1] -replace '[^\d]', '')
        }
        
        # Full Charge Capacity parsing
        if ($html -match 'FULL CHARGE CAPACITY\s*</td>\s*<td>\s*([\d,]+)\s*mWh') {
            $fullChargeCapacity = [int]($Matches[1] -replace '[^\d]', '')
        }
        
        # Cycle Count parsing
        if ($html -match 'CYCLE COUNT\s*</td>\s*<td>\s*([\d,]+)\s*') {
            $cycleCount = [int]($Matches[1] -replace '[^\d]', '')
        }
        
        # Chemistry parsing
        if ($html -match 'CHEMISTRY\s*</td>\s*<td>\s*(\S+)\s*') {
            $chemistry = $Matches[1].Trim()
        }
        
        if ($designCapacity -gt 0 -and $fullChargeCapacity -gt 0) {
            $parsedSuccessfully = $true
        }
    } catch {}
    Remove-Item $reportPath -Force -ErrorAction SilentlyContinue
}

# WMI Fallback if powercfg parsing failed or returned empty results
if (-not $parsedSuccessfully) {
    $static = Get-CimInstance -Namespace root\wmi -ClassName BatteryStaticData -ErrorAction SilentlyContinue
    $fcc = Get-CimInstance -Namespace root\wmi -ClassName BatteryFullChargeCapacity -ErrorAction SilentlyContinue
    $cycle = Get-CimInstance -Namespace root\wmi -ClassName BatteryCycleCount -ErrorAction SilentlyContinue
    
    if ($static) {
        $designCapacity = $static.DesignedCapacity
        $chemistry = $static.Chemistry
    }
    if ($fcc) {
        $fullChargeCapacity = $fcc.FullChargeCapacity
    }
    if ($cycle) {
        $cycleCount = $cycle.CycleCount
    }
}

if ($designCapacity -gt 0 -and $fullChargeCapacity -gt 0) {
    $health = [math]::Round(($fullChargeCapacity / $designCapacity) * 100, 2)
    Write-Output "Design Capacity: $designCapacity mWh"
    Write-Output "Full Charge Capacity: $fullChargeCapacity mWh"
    Write-Output "Chemistry: $chemistry"
    if ($cycleCount -gt 0) {
        Write-Output "Cycle Count: $cycleCount"
    }
    Write-Output "Battery Health Score: $health %"
    if ($health -lt 70) {
        Write-Output "WARNING: Battery health is below 70%. Consider replacing the battery soon."
    } else {
        Write-Output "Battery health is good!"
    }
} else {
    Write-Output "Failed to retrieve design and full charge capacity information."
}

exit 0
