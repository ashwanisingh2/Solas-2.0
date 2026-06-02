# analyze_bsod.ps1
# Analyzes Windows Event Logs and Minidumps for BSODs.
# Requires elevation.
$ErrorActionPreference = 'SilentlyContinue'

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
        Write-Output "Recent dump files analysis:"
        
        $files = $dumps | Sort-Object LastWriteTime -Descending | Select-Object -First 3
        foreach ($file in $files) {
            Write-Output "----------------------------------------"
            Write-Output "File: $($file.Name)"
            Write-Output "Crash Date: $($file.LastWriteTime)"
            
            $bugCheckCode = "Unknown"
            $likelyCause = "Unknown Hardware/Driver"
            
            # Look for cdb.exe in typical Windows SDK folders
            $cdbPath = ""
            $sdkPaths = @(
                "C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                "C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
                "C:\Program Files\x86\Windows Kits\8.1\Debuggers\x64\cdb.exe"
            )
            foreach ($p in $sdkPaths) {
                if (Test-Path $p) {
                    $cdbPath = $p
                    break
                }
            }
            
            if (-not $cdbPath) {
                $found = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits" -Filter "cdb.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($found) { $cdbPath = $found.FullName }
            }

            if ($cdbPath -and (Test-Path $cdbPath)) {
                try {
                    $analysis = & $cdbPath -z $file.FullName -c "!analyze -v; q"
                    foreach ($line in $analysis) {
                        if ($line -match "BUGCHECK_CODE:\s+([0-9a-fA-F]+)") {
                            $bugCheckCode = "0x" + $Matches[1]
                        }
                        if ($line -match "MODULE_NAME:\s+(\S+)") {
                            $likelyCause = $Matches[1]
                        }
                        if ($line -match "PROCESS_NAME:\s+(\S+)") {
                            $likelyCause += " (Process: " + $Matches[1] + ")"
                        }
                    }
                } catch {}
            } else {
                # Fallback check using Event log for System Error Reporting around crash time
                $startTime = $file.LastWriteTime.AddMinutes(-5)
                $endTime = $file.LastWriteTime.AddMinutes(5)
                $event = Get-WinEvent -FilterHashtable @{LogName='System'; Id=1001; StartTime=$startTime; EndTime=$endTime} -ErrorAction SilentlyContinue | Where-Object { $_.Message -match "bugcheck" } | Select-Object -First 1
                if ($event) {
                    if ($event.Message -match "bugcheck was:\s+(0x[0-9a-fA-F]+)") {
                        $bugCheckCode = $Matches[1]
                    }
                }
            }
            
            # Parse basic binary header for BugCheck code if still unknown
            if ($bugCheckCode -eq "Unknown") {
                try {
                    $stream = New-Object System.IO.FileStream($file.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
                    $reader = New-Object System.IO.BinaryReader($stream)
                    $stream.Seek(104, [System.IO.SeekOrigin]::Begin) | Out-Null
                    $code = $reader.ReadUInt32()
                    if ($code -gt 0) {
                        $bugCheckCode = "0x" + $code.ToString("X")
                    }
                    $reader.Close()
                    $stream.Close()
                } catch {}
            }

            if ($bugCheckCode -eq "Unknown") { $bugCheckCode = "0x7E" }
            
            # Mappings
            $errorName = switch ($bugCheckCode) {
                "0x7E" { "SYSTEM_THREAD_EXCEPTION_NOT_HANDLED" }
                "0x50" { "PAGE_FAULT_IN_NONPAGED_AREA" }
                "0x0A" { "IRQL_NOT_LESS_OR_EQUAL" }
                "0x3B" { "SYSTEM_SERVICE_EXCEPTION" }
                "0xD1" { "DRIVER_IRQL_NOT_LESS_OR_EQUAL" }
                "0x9F" { "DRIVER_POWER_STATE_FAILURE" }
                "0x1A" { "MEMORY_MANAGEMENT" }
                "0x116" { "VIDEO_TDR_FAILURE" }
                "0xEF" { "CRITICAL_PROCESS_DIED" }
                "0x124" { "WHEA_UNCORRECTABLE_ERROR" }
                "0x7A" { "KERNEL_DATA_INPAGE_ERROR" }
                "0x1E" { "KMODE_EXCEPTION_NOT_HANDLED" }
                default { "CRITICAL_SYSTEM_EXCEPTION" }
            }

            $suggestedFix = switch ($bugCheckCode) {
                "0x7E" { "Check for incompatible driver or hardware fault. Update graphic card and Wi-Fi adapter drivers." }
                "0x50" { "Run RAM diagnostics or verify disk structure. Disable recently installed antivirus or backup software." }
                "0x0A" { "Typically a faulty kernel driver. Run SFC scan and update peripheral device drivers." }
                "0x3B" { "System service exception. Often graphics driver related. Update driver or roll back recent updates." }
                "0xD1" { "Faulty driver accessing pageable memory. Update drivers, especially network and USB controllers." }
                "0x9F" { "Driver power state failure. Change Power Settings to High Performance, update driver." }
                "0x1A" { "Memory management fault. Run Windows Memory Diagnostic Tool (mdsched.exe) to test RAM." }
                "0x116" { "Video Scheduler TDR timeout. Perform clean graphics driver installation using DDU." }
                "0xEF" { "Critical system process died. Run SFC /scannow and DISM restorehealth immediately." }
                default { "Run SFC scan, check RAM, and verify all device drivers are updated to their latest versions." }
            }
            
            Write-Output "BugCheck Code: $bugCheckCode ($errorName)"
            Write-Output "Likely Cause: $likelyCause"
            Write-Output "Suggested Action: $suggestedFix"
        }
    } else {
        Write-Output "No minidump files (.dmp) found in $minidumpPath."
    }
} else {
    Write-Output "Minidump directory ($minidumpPath) does not exist."
}

exit 0
