# Runs Windows System File Checker.
# Requires elevation.

$ErrorActionPreference = 'Stop'

Write-Output "=== Running SFC /scannow ==="
sfc /scannow
exit $LASTEXITCODE
