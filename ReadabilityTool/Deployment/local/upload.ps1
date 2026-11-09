# Copies datasets and/or models to the NIPG cluster.
#
#   ./ReadabilityTool/Deployment/local/upload.ps1 -Datasets C:\data\bw-dataset.csv
#   ./ReadabilityTool/Deployment/local/upload.ps1 -Models D:\models\qwen3.5-9b-q4_k_m.gguf, D:\models\descriptions.json
#
# Datasets go to ~/readability/datasets (next to the tool's ID-stamped copies), models and
# the model description file to ~/readability/models.
param(
    [string[]]$Datasets = @(),
    [string[]]$Models = @(),
    [string]$Server = "nipg-cluster",
    [string]$RemoteRoot = "readability"
)

$ErrorActionPreference = "Stop"

if ($Datasets.Count -eq 0 -and $Models.Count -eq 0) {
    throw "Nothing to upload: pass -Datasets and/or -Models."
}

foreach ($file in $Datasets + $Models) {
    if (-not (Test-Path $file)) { throw "File not found: $file" }
}

ssh $Server "mkdir -p $RemoteRoot/datasets $RemoteRoot/models"

foreach ($file in $Datasets) {
    Write-Host "=== $file -> datasets/"
    scp $file "${Server}:$RemoteRoot/datasets/"
    if ($LASTEXITCODE -ne 0) { throw "Upload failed: $file" }
}

foreach ($file in $Models) {
    Write-Host "=== $file -> models/"
    scp $file "${Server}:$RemoteRoot/models/"
    if ($LASTEXITCODE -ne 0) { throw "Upload failed: $file" }
}

Write-Host "Done."
