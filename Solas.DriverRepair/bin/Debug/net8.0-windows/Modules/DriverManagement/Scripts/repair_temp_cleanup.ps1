# Cleans user and Windows temp files that are not currently locked.

$ErrorActionPreference = 'Continue'

Write-Output "=== Cleaning temporary files ==="
$paths = @($env:TEMP, "$env:SystemRoot\Temp")

foreach ($path in $paths) {
    if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path)) {
        Write-Output "Cleaning $path"
        Get-ChildItem -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Output "Temporary cleanup completed."
exit 0
