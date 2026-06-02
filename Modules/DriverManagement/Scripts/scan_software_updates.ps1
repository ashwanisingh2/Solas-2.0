# scan_software_updates.ps1
# Runs winget upgrade and parses the results into JSON.

$ErrorActionPreference = "Continue"

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Output "[]"
    exit 0
}

# Run winget upgrade
$tempFile = [System.IO.Path]::GetTempFileName()
try {
    # Run winget upgrade in a way that avoids interactive prompts and captures output
    $process = Start-Process winget -ArgumentList "upgrade --accept-source-agreements" -NoNewWindow -PassThru -RedirectStandardOutput $tempFile -RedirectStandardError "$tempFile.err"
    $process.WaitForExit()

    if (-not (Test-Path $tempFile)) {
        Write-Output "[]"
        exit 0
    }

    $lines = Get-Content $tempFile -Encoding utf8
}
finally {
    # Cleanup temp files
    if (Test-Path $tempFile) { Remove-Item $tempFile -ErrorAction SilentlyContinue }
    if (Test-Path "$tempFile.err") { Remove-Item "$tempFile.err" -ErrorAction SilentlyContinue }
}

$updates = @()
$startParsing = $false

foreach ($line in $lines) {
    # Remove progress bars or loading animation characters
    $line = $line -replace '\x1b\[[0-9;]*[a-zA-Z]', '' # Strip ANSI escape codes
    $line = $line.Trim()
    
    if ($line -match "Name\s+Id\s+Version\s+Available") {
        $startParsing = $true
        continue
    }
    
    if ($startParsing) {
        if ($line.StartsWith("-")) {
            continue
        }
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }
        # Terminate when we hit the summary line (e.g., "9 upgrades available." or similar)
        if ($line -match "^\d+\s+upgrades?\s+available" -or $line -match "package\(s\) have version numbers") {
            break
        }
        
        # Split by at least two spaces
        $parts = $line -split '\s{2,}'
        if ($parts.Count -ge 4) {
            $name = $parts[0].Trim()
            $id = $parts[1].Trim()
            $version = $parts[2].Trim()
            $available = $parts[3].Trim()
            $source = if ($parts.Count -ge 5) { $parts[4].Trim() } else { "winget" }
            
            $updates += [PSCustomObject]@{
                Name = $name
                Id = $id
                CurrentVersion = $version
                AvailableVersion = $available
                Source = $source
            }
        }
    }
}

Write-Output ($updates | ConvertTo-Json -Compress)
