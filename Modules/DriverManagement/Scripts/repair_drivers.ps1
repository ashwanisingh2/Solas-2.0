# Searches Windows Update for driver updates and performs PNP device repair resets.
# Requires elevation.

$ErrorActionPreference = 'Continue'

Write-Output "=== Missing & Outdated Driver Fixer ==="

# Step 1: Force Windows to scan for hardware changes
Write-Output ""
Write-Output "[1/3] Scanning for hardware changes (pnputil)..."
pnputil /scan-devices
Write-Output "Hardware scan complete."

# Step 2: Search Windows Update for driver packages
Write-Output ""
Write-Output "[2/3] Searching Windows Update for driver packages..."
try {
    $session = New-Object -ComObject Microsoft.Update.Session
    $searcher = $session.CreateUpdateSearcher()
    
    # Search for non-installed driver updates
    $searchResult = $searcher.Search("IsInstalled=0 and Type='Driver'")
    
    $count = $searchResult.Updates.Count
    if ($count -eq 0) {
        Write-Output "No driver updates found in Windows Update catalog."
    } else {
        Write-Output "Found $count driver update(s):"
        for ($i = 0; $i -lt $count; $i++) {
            $update = $searchResult.Updates.Item($i)
            Write-Output "- [$i] $($update.Title)"
        }
        
        Write-Output "Downloading updates..."
        $downloader = $session.CreateUpdateDownloader()
        $downloader.Updates = $searchResult.Updates
        $downloader.Download() | Out-Null
        
        Write-Output "Installing updates..."
        $installer = $session.CreateUpdateInstaller()
        $installer.Updates = $searchResult.Updates
        $installResult = $installer.Install()
        Write-Output "Installation complete (Result Code: $($installResult.ResultCode))."
    }
} catch {
    Write-Output "Windows Update Driver Search bypassed or failed: $_"
}

# Step 3: Identify devices with issues and perform diagnostic reset (Disable/Enable)
Write-Output ""
Write-Output "[3/3] Scanning for devices with driver errors..."
$devices = Get-PnpDevice | Where-Object { $_.Problem -ne "NoError" -or $_.Status -eq "Error" }

if ($devices) {
    Write-Output "Found $($devices.Count) device(s) with errors. Attempting driver reset..."
    foreach ($dev in $devices) {
        Write-Output "Resetting device: $($dev.FriendlyName) (InstanceId: $($dev.InstanceId))"
        try {
            Disable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction Stop
            Start-Sleep -Seconds 2
            Enable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction Stop
            Write-Output "Successfully reset: $($dev.FriendlyName)"
        } catch {
            Write-Output "Failed to reset device: $($dev.FriendlyName) ($_)"
        }
    }
} else {
    Write-Output "No devices with driver errors detected in Device Manager."
}

Write-Output ""
Write-Output "=== Driver repair operations completed. Please check Device Manager or restart your PC. ==="
exit 0
