# Flushes DNS resolver cache.

$ErrorActionPreference = 'Stop'

Write-Output "=== Flushing DNS cache ==="
ipconfig /flushdns
exit $LASTEXITCODE
