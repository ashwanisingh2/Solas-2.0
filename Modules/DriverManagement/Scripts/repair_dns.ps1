# repair_dns.ps1
# Flushes DNS resolver cache.
$ErrorActionPreference = 'SilentlyContinue'

Write-Output "=== Flushing DNS cache ==="
ipconfig /flushdns | Out-Null
Clear-DnsClientCache
Write-Output "DNS Cache flushed successfully."
exit 0
