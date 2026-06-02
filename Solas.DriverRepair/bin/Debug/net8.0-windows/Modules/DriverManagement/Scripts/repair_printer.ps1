# Resets the Windows Print Spooler service and queued print jobs.
# Requires elevation.

$ErrorActionPreference = 'Stop'

Write-Output "=== Resetting Print Spooler ==="
Stop-Service -Name Spooler -Force -ErrorAction SilentlyContinue
$spoolPath = Join-Path $env:SystemRoot "System32\spool\PRINTERS"
if (Test-Path $spoolPath) {
    Get-ChildItem -LiteralPath $spoolPath -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Output "Cleared queued print jobs."
}
Start-Service -Name Spooler
Write-Output "Print Spooler reset completed."
exit 0
