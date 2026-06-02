# unschedule_care.ps1
# Unregisters/deletes the weekly scheduled task.

$ErrorActionPreference = "Stop"

$taskName = "SolasDriverRepair_WeeklyCare"

# Check if task exists first
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue

if ($task) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Write-Output "Successfully unregistered scheduled task '$taskName'."
} else {
    Write-Output "Scheduled task '$taskName' does not exist."
}
