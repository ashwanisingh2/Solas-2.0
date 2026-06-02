# Resets common Windows networking state.
# Requires elevation.

$ErrorActionPreference = 'Stop'

Write-Output "=== Resetting network stack ==="
ipconfig /flushdns
netsh winsock reset
netsh int ip reset
Write-Output "Network reset completed. Restart Windows to fully apply changes."
exit 0
