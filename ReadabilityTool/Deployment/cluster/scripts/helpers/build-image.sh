#!/bin/bash
#SBATCH --job-name=rd-build-image
#SBATCH --partition=small
#SBATCH --cpus-per-task=4
#SBATCH --mem=16G
#SBATCH --time=01:00:00
#SBATCH --output=logs/build-image-%j.out
#
# Builds the container image on a compute node; never build on nipg1.
# Submitted by Deployment/local/deploy.ps1 with: cd ~/readability && sbatch scripts/helpers/build-image.sh
# Needs no GPU. The build only downloads the base image and copies the tool in (see the
# .def file for why nothing is installed), which takes a few minutes.
set -euo pipefail

ROOT="${SLURM_SUBMIT_DIR:?This script runs as a SLURM job}"
cd "$ROOT/app"

# Node-local scratch space: faster than the network home, and the build needs room.
export APPTAINER_TMPDIR="/tmp/rd-build-${SLURM_JOB_ID}"
export APPTAINER_CACHEDIR="/tmp/rd-cache-${SLURM_JOB_ID}"
mkdir -p "$APPTAINER_TMPDIR" "$APPTAINER_CACHEDIR"
trap 'rm -rf "$APPTAINER_TMPDIR" "$APPTAINER_CACHEDIR" readabilityconsoleapp.sif.new' EXIT

echo "=== $(date) building on $(hostname)"

# Files copied from Windows arrive without the executable bit, and the image keeps the
# permissions it is given.
chmod +x publish/ReadabilityConsoleApp

# Built next to the old image and swapped in at the end, so running jobs are not disturbed.
apptainer build --force readabilityconsoleapp.sif.new readabilityconsoleapp.def

mv readabilityconsoleapp.sif.new readabilityconsoleapp.sif
echo "=== $(date) image ready: $ROOT/app/readabilityconsoleapp.sif"
apptainer exec readabilityconsoleapp.sif llama-server --version || true
apptainer run readabilityconsoleapp.sif help | head -n 1 || true
