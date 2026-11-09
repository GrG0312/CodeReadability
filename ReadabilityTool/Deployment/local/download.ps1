# Copies a finished run folder from the NIPG cluster to this computer.
#
#   ./ReadabilityTool/Deployment/local/download.ps1 -Run run_20261015_140233
#   ./ReadabilityTool/Deployment/local/download.ps1 -List      # show the available run folders
param(
    [string]$Run,
    [switch]$List,
    [string]$Destination = (Join-Path (Get-Location) "cluster-results"),
    [string]$Server = "nipg-cluster",
    [string]$RemoteRoot = "readability"
)

$ErrorActionPreference = "Stop"

if ($List -or -not $Run) {
    ssh $Server "ls -1 $RemoteRoot/output"
    if (-not $Run) { return }
}

New-Item -ItemType Directory -Force $Destination | Out-Null
scp -r "${Server}:$RemoteRoot/output/$Run" $Destination
if ($LASTEXITCODE -ne 0) { throw "Download failed. Check the run name with -List." }

Write-Host "Downloaded to $(Join-Path $Destination $Run)"
