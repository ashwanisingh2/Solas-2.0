# schedule_care.ps1
# Registers a scheduled task under local SYSTEM context that runs iobit_one_click_care.ps1 weekly.

$ErrorActionPreference = "Stop"

# Get absolute path of iobit_one_click_care.ps1
$scriptPath = Join-Path $PSScriptRoot "iobit_one_click_care.ps1"

if (-not (Test-Path $scriptPath)) {
    Write-Error "Could not find iobit_one_click_care.ps1 at path: $scriptPath"
    exit 1
}

# Task Name
$taskName = "SolasDriverRepair_WeeklyCare"

# Action: Launch powershell.exe bypass execution policy running the care script
$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""

# Trigger: Weekly on Sunday at 3:00 AM
$trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At 3:00AM

# Principal: SYSTEM (highest privilege)
$principal = New-ScheduledTaskPrincipal -UserId "NT AUTHORITY\SYSTEM" -LogonType ServiceAccount -RunLevel Highest

# Register Scheduled Task
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Description "Weekly automated Windows System Care, cleanup, and driver check." -Force

Write-Output "Successfully registered scheduled task '$taskName' weekly on Sunday at 3:00 AM."
