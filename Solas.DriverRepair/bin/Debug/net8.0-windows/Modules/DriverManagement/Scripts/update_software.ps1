# update_software.ps1
param (
    [Parameter(Mandatory=$true)]
    [string]$Id
)

$ErrorActionPreference = "Continue"

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Error "Winget is not installed on this system."
    exit 1
}

# Run winget upgrade for the package
winget upgrade --id $Id --silent --accept-package-agreements --accept-source-agreements
exit $LASTEXITCODE
