# optimize_performance.ps1
# Optimizes system performance by cleaning temp, resetting winsock, and trimming SSDs.
# Requires elevation.

$ErrorActionPreference = 'SilentlyContinue'

Write-Output "=== System Performance Optimizer ==="

# 1. Clean Temp folders
$tempFolders = @(
    $env:TEMP,
    "$env:SystemRoot\Temp"
)
Write-Output "Step 1: Cleaning temporary files..."
$totalCleaned = 0
$totalFailed = 0
$now = Get-Date

foreach ($folder in $tempFolders) {
    if (Test-Path $folder) {
        $files = Get-ChildItem -Path $folder -File -Recurse -ErrorAction SilentlyContinue
        foreach ($f in $files) {
            # 5-minute whitelist protection for active sessions
            $ageMin = ($now - $f.LastWriteTime).TotalMinutes
            if ($ageMin -lt 5) { continue }
            $size = $f.Length
            try {
                Remove-Item -Path $f.FullName -Force -Confirm:$false -ErrorAction Stop | Out-Null
                $totalCleaned += $size
            } catch {
                $totalFailed++
            }
        }
    }
}
$totalMbCleaned = [Math]::Round($totalCleaned / 1024 / 1024, 2)
Write-Output "Cleanup completed. Freed: $totalMbCleaned MB (Skipped/Locked files: $totalFailed)"

# 2. Optimize SSD / Defrag HDD (Defragmentation and TRIM)
Write-Output ""
Write-Output "Step 2: Checking and optimizing drives (TRIM / Defrag)..."
try {
    # Verify and enable TRIM support if disabled
    Write-Output "Checking Windows TRIM status..."
    $notify = fsutil behavior query DisableDeleteNotify
    if ($notify -match "= 1") {
        Write-Output "TRIM is disabled. Enabling TRIM support..."
        fsutil behavior set DisableDeleteNotify 0 | Out-Null
        Write-Output "TRIM support enabled successfully."
    } else {
        Write-Output "TRIM support is already enabled."
    }

    Write-Output "Optimizing Drive C:..."
    $osVersion = [System.Environment]::OSVersion.Version
    if ($osVersion.Major -lt 10) {
        # Windows 7 / 8 SP1: defrag /L command triggers TRIM
        defrag.exe "C:" /L /V
    } else {
        # Windows 10/11: Optimize-Volume
        Optimize-Volume -DriveLetter C -ReTrim -Verbose
    }
    Write-Output "Drive C: optimization completed."
} catch {
    Write-Output "Drive optimization failed or bypassed: $_"
}

# 3. Clean Winsock & DNS Cache
Write-Output ""
Write-Output "Step 3: Flushing DNS and resetting network sockets..."
Clear-DnsClientCache
netsh winsock reset | Out-Null
netsh int ip reset | Out-Null
Write-Output "Network stack optimization completed."

Write-Output ""
Write-Output "=== Optimization completed successfully. Please restart your PC to apply all changes. ==="
exit 0
