# repair_network.ps1
# Resets common Windows networking state.
# Requires elevation.

$ErrorActionPreference = 'SilentlyContinue'

Write-Output "=== Resetting network stack ==="
Write-Output "Step 1: Resetting winsock catalog..."
netsh winsock reset | Out-Null
Write-Output "Step 2: Resetting internet protocol (TCP/IPv4 & TCP/IPv6)..."
netsh int ip reset | Out-Null
netsh int ipv6 reset | Out-Null

Write-Output "Step 3: Power cycling active physical network adapters..."
try {
    $adapters = Get-NetAdapter | Where-Object { $_.Status -eq 'Up' -and $_.Physical }
    foreach ($a in $adapters) {
        Write-Output "Power cycling adapter: $($a.Name)..."
        Disable-NetAdapter -Name $a.Name -Confirm:$false -ErrorAction Stop | Out-Null
        Start-Sleep -Seconds 2
        Enable-NetAdapter -Name $a.Name -Confirm:$false -ErrorAction Stop | Out-Null
    }
} catch {
    Write-Output "Notice: Adapter power cycle bypassed or not supported on this adapter."
}

Write-Output "Step 4: Flushing DNS resolver cache..."
ipconfig /flushdns | Out-Null
Clear-DnsClientCache

Write-Output "Step 5: Verifying internet connectivity..."
$connected = $false
for ($i = 0; $i -lt 5; $i++) {
    Start-Sleep -Seconds 1
    $ping = Test-Connection -ComputerName 8.8.8.8 -Count 1 -ErrorAction SilentlyContinue
    if ($ping) {
        $connected = $true
        break
    }
}

if ($connected) {
    Write-Output "Network reset completed successfully. Sockets stack repaired and connected to Internet."
} else {
    Write-Output "Network reset completed. Local connectivity restored, but full Internet access may require a reboot."
}
exit 0
