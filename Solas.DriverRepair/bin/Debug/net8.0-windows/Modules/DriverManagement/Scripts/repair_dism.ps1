# Repairs the Windows component store with DISM.
# Requires elevation.

$ErrorActionPreference = 'Stop'

Write-Output "=== Running DISM /RestoreHealth ==="
DISM.exe /Online /Cleanup-Image /RestoreHealth
exit $LASTEXITCODE
