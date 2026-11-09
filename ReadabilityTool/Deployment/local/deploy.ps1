# Builds the tool, copies it and the cluster scripts to the NIPG cluster, and submits the
# container image build as a job on a compute node (building on the login node is not allowed).
#
#   ./ReadabilityTool/Deployment/local/deploy.ps1
#   ./ReadabilityTool/Deployment/local/deploy.ps1 -SkipImageBuild    # only update the scripts
#
# Needs the SSH alias 'nipg-cluster' in ~/.ssh/config (or pass -Server), and the ELTE VPN when
# off campus. The image build takes a few minutes; check it with scripts/status.sh on nipg1.
param(
    [string]$Server = "nipg-cluster",
    [string]$RemoteRoot = "readability",
    [switch]$SkipImageBuild
)

$ErrorActionPreference = "Stop"

$toolRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$deployment = Join-Path $toolRoot "Deployment"
$publish = Join-Path ([IO.Path]::GetTempPath()) "readability-publish"

Write-Host "=== Building the tool for Linux..."
if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
# Self-contained: the container has no .NET runtime (see the .def file).
dotnet publish (Join-Path $toolRoot "ReadabilityConsoleApp/ReadabilityConsoleApp.csproj") `
    -c Release -r linux-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "=== Preparing folders on $Server..."
ssh $Server "mkdir -p $RemoteRoot/app $RemoteRoot/scripts/helpers $RemoteRoot/models $RemoteRoot/datasets $RemoteRoot/logs && rm -rf $RemoteRoot/app/publish"
if ($LASTEXITCODE -ne 0) { throw "Could not reach $Server. Is the SSH key added in the portal, and the VPN on?" }

Write-Host "=== Copying the tool and the scripts..."
scp -r $publish "${Server}:$RemoteRoot/app/publish"
scp (Join-Path $deployment "container/readabilityconsoleapp.def") "${Server}:$RemoteRoot/app/"
scp (Get-ChildItem (Join-Path $deployment "cluster/scripts") -Filter *.sh).FullName "${Server}:$RemoteRoot/scripts/"
scp (Get-ChildItem (Join-Path $deployment "cluster/scripts/helpers") -Filter *.sh).FullName "${Server}:$RemoteRoot/scripts/helpers/"
if ($LASTEXITCODE -ne 0) { throw "Copying failed." }

# Scripts edited on Windows may carry CR line endings, which bash refuses.
ssh $Server "cd $RemoteRoot && sed -i 's/\r$//' scripts/*.sh scripts/helpers/*.sh && chmod +x scripts/*.sh scripts/helpers/*.sh"

if (-not $SkipImageBuild) {
    Write-Host "=== Submitting the container image build..."
    ssh $Server "cd $RemoteRoot && sbatch scripts/helpers/build-image.sh"
}

Remove-Item -Recurse -Force $publish

Write-Host ""
Write-Host "Deployed to ${Server}:~/$RemoteRoot"
Write-Host "Next: upload models and datasets (upload.ps1), then on nipg1: cd ~/$RemoteRoot && scripts/submit.sh ..."
