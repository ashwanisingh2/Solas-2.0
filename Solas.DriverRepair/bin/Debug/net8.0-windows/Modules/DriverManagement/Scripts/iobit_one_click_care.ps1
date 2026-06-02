# Performs complete system cleanup, driver updates, and performance optimization.
# Requires elevation.

$ErrorActionPreference = 'Continue'

Write-Output "=== IOBit-Style Advanced System Care (One-Click Care) ==="

function Log-Step {
    param([string]$Name, [scriptblock]$Action)
    Write-Output ""
    Write-Output "[Step] $Name starting..."
    try {
        & $Action
        Write-Output "[Step] $Name completed successfully."
    } catch {
        Write-Output "[Step] $Name failed: $_"
    }
}

# Step 1: System Restore Point (Safety First)
Log-Step "Create System Restore Point" {
    $registryPath = "HKLM:\Software\Microsoft\Windows NT\CurrentVersion\SystemRestore"
    if (Test-Path $registryPath) {
        Set-ItemProperty -Path $registryPath -Name "SystemRestorePointCreationFrequency" -Value 0 -ErrorAction SilentlyContinue
    }
    Enable-ComputerRestorePoint -Drive "C:\" -ErrorAction SilentlyContinue
    Checkpoint-Computer -Description "Solas One-Click Care Restore Point" -RestorePointType "APPLICATION_INSTALL" -Confirm:$false
}

# Step 2: System Junk & Cache Cleanup
Log-Step "System Junk Cleanup" {
    $tempFolders = @($env:TEMP, "$env:SystemRoot\Temp")
    foreach ($folder in $tempFolders) {
        if (Test-Path $folder) {
            Get-ChildItem -Path $folder -Recurse -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    Write-Output "Cleaned temporary cache files."
}

# Step 3: Internet Booster & Sockets Reset
Log-Step "Internet Optimization" {
    Clear-DnsClientCache -ErrorAction SilentlyContinue
    netsh winsock reset | Out-Null
    netsh int ip reset | Out-Null
    # TCP settings optimization for lower latency
    netsh int tcp set global autotuninglevel=normal -ErrorAction SilentlyContinue
    Write-Output "Network stack tuned for high speed."
}

# Step 4: Driver Booster (Scan, Reset and Install Updates)
Log-Step "Driver Updates Scan & Installation" {
    # 1. Scan devices first
    pnputil /scan-devices | Out-Null
    
    # 2. Reset problem devices
    $devices = Get-PnpDevice | Where-Object { $_.Problem -ne "NoError" -or $_.Status -eq "Error" }
    if ($devices) {
        Write-Output "Found $($devices.Count) device(s) with errors. Resetting..."
        foreach ($dev in $devices) {
            try {
                Disable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction Stop
                Start-Sleep -Seconds 1
                Enable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction Stop
                Write-Output "Reset device: $($dev.FriendlyName)"
            } catch {
                Write-Output "Could not reset: $($dev.FriendlyName)"
            }
        }
    }

    # 3. Search and install from Windows Update
    try {
        $session = New-Object -ComObject Microsoft.Update.Session
        $searcher = $session.CreateUpdateSearcher()
        $searchResult = $searcher.Search("IsInstalled=0 and Type='Driver'")
        $count = $searchResult.Updates.Count
        if ($count -eq 0) {
            Write-Output "All system drivers are up to date in Windows Update."
        } else {
            Write-Output "Found $count driver update(s). Installing..."
            $downloader = $session.CreateUpdateDownloader()
            $downloader.Updates = $searchResult.Updates
            $null = $downloader.Download()
            
            $installer = $session.CreateUpdateInstaller()
            $installer.Updates = $searchResult.Updates
            $installResult = $installer.Install()
            Write-Output "Drivers installation complete (Result Code: $($installResult.ResultCode))."
        }
    } catch {
        Write-Output "Windows Update scan skipped: $_"
    }
}

# Step 5: System Files SFC Check
Log-Step "System File Checker (SFC)" {
    sfc /scannow
}

# Step 6: Disk Speed Optimizer (SSD TRIM)
Log-Step "Disk Speed Optimization" {
    Optimize-Volume -DriveLetter C -ReTrim -Verbose -ErrorAction SilentlyContinue
}

# Step 7: Security Shield Check
Log-Step "Security Shield Audit" {
    $defender = Get-Service -Name "WinDefend" -ErrorAction SilentlyContinue
    if ($defender) {
        Write-Output "Antivirus active shield is: $($defender.Status)"
    }
    $firewall = netsh advfirewall show allprofiles state
    Write-Output "Firewall Shield States:"
    Write-Output ($firewall | Select-String "State")
}

Write-Output ""
Write-Output "=== Complete Advanced Care Run Completed! Restart your PC to apply all updates. ==="
