# repair_temp_cleanup.ps1
# Cleans user and Windows temp files, browser caches, and prefetch files safely.
# Requires elevation.

$ErrorActionPreference = 'SilentlyContinue'

Write-Output "=== Advanced System Junk & Temp File Cleanup ==="

$targets = @(
    @{ Path = "$env:TEMP"; Category = "User Temp" },
    @{ Path = "$env:SystemRoot\Temp"; Category = "System Temp" },
    @{ Path = "$env:SystemRoot\Prefetch"; Category = "Prefetch" },
    @{ Path = "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Cache"; Category = "Chrome Cache" },
    @{ Path = "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Cache"; Category = "Edge Cache" }
)

$totalCleaned = 0
$totalFailed = 0
$now = Get-Date

foreach ($target in $targets) {
    if (Test-Path $target.Path) {
        Write-Output "Cleaning Category: $($target.Category) ($($target.Path))..."
        $files = Get-ChildItem -Path $target.Path -File -Recurse -ErrorAction SilentlyContinue
        $catCleaned = 0
        $catFailed = 0
        
        foreach ($f in $files) {
            # 5-minute whitelist protection for active sessions
            $ageMin = ($now - $f.LastWriteTime).TotalMinutes
            if ($ageMin -lt 5) { 
                continue 
            }
            
            $size = $f.Length
            try {
                Remove-Item -Path $f.FullName -Force -Confirm:$false -ErrorAction Stop | Out-Null
                $catCleaned += $size
                $totalCleaned += $size
            } catch {
                $catFailed++
                $totalFailed++
            }
        }
        
        $mbCleaned = [Math]::Round($catCleaned / 1024 / 1024, 2)
        Write-Output "-> Cleaned: $mbCleaned MB (Skipped/Locked files: $catFailed)"
    }
}

$totalMbCleaned = [Math]::Round($totalCleaned / 1024 / 1024, 2)
Write-Output ""
Write-Output "=== Cleanup Finished ==="
Write-Output "Total Freed Space: $totalMbCleaned MB"
Write-Output "Total Locked Files Skipped: $totalFailed"
exit 0
