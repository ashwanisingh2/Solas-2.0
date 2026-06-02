# Analyzes Windows Event Logs and Minidumps for BSODs.
# Requires elevation.

$ErrorActionPreference = 'Stop'

Write-Output "=== BSOD & System Crash Analyzer ==="

# 1. Check Event Logs for BugCheck (EventID 1001)
Write-Output "Scanning Event Logs for past BugCheck events..."
$events = Get-WinEvent -FilterHashtable @{LogName='System'; ID=1001} -MaxEvents 5 -ErrorAction SilentlyContinue

if ($events) {
    foreach ($e in $events) {
        Write-Output "----------------------------------------"
        Write-Output "Crash Date: $($e.TimeCreated)"
        Write-Output "Details:"
        Write-Output ($e.Message)
    }
} else {
    Write-Output "No recent BugCheck event logs found."
}

# 2. Check Minidumps directory
$minidumpPath = "$env:SystemRoot\Minidump"
if (Test-Path $minidumpPath) {
    $dumps = Get-ChildItem -Path $minidumpPath -Filter "*.dmp" -ErrorAction SilentlyContinue
    if ($dumps) {
        Write-Output ""
        Write-Output "Found $($dumps.Count) minidump file(s) in $minidumpPath."
        Write-Output "Recent dump files:"
        $dumps | Sort-Object LastWriteTime -Descending | Select-Object -First 3 | ForEach-Object {
            Write-Output "- $($_.Name) (Modified: $($_.LastWriteTime))"
        }
    } else {
        Write-Output "No minidump files (.dmp) found in $minidumpPath."
    }
} else {
    Write-Output "Minidump directory ($minidumpPath) does not exist."
}

exit 0
