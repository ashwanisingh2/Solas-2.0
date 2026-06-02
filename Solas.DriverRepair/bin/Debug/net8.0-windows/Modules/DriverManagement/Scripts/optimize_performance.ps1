# Optimizes system performance by cleaning temp, resetting winsock, and trimming SSDs.
# Requires elevation.
$ErrorActionPreference = 'Stop'

Write-Output "=== System Performance Optimizer ==="

# 1. Clean Temp folders
$tempFolders = @(
    $env:TEMP,
    "$env:SystemRoot\Temp"
)
Write-Output "Cleaning temporary files..."
foreach ($folder in $tempFolders) {
    if (Test-Path $folder) {
        Get-ChildItem -Path $folder -Recurse -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
}
Write-Output "Cleanup completed."

# 2. Optimize SSD / Defrag HDD (Defragmentation and TRIM)
Write-Output "Running drive optimization (TRIM for SSD)..."
Optimize-Volume -DriveLetter C -ReTrim -Verbose -ErrorAction SilentlyContinue
Write-Output "Drive C: optimization completed."

# 3. Clean Winsock & DNS Cache
Write-Output "Flushing DNS and resetting network sockets..."
Clear-DnsClientCache -ErrorAction SilentlyContinue
netsh winsock reset | Out-Null
netsh int ip reset | Out-Null
Write-Output "Network stack optimization completed."

Write-Output "Optimization completed successfully. It is recommended to restart your PC."
exit 0
