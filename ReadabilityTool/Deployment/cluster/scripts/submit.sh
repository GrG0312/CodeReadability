#!/bin/bash
# Submits a tool command as SLURM job(s). Run it on nipg1, from anywhere.
#
#   scripts/submit.sh run-pipeline --dataset datasets/bw.csv --model-dir models --language python
#   scripts/submit.sh run-pipeline ... --job-mode multi-job      (one job per model, in parallel)
#   scripts/submit.sh check-models --model-dir models --dataset datasets/bw.csv
#   scripts/submit.sh compute-statistics --judgements <file> --metrics <file>
#
# The tool runs briefly here (inside the container) to check everything that needs no GPU,
# create the execution, and size each job's GPU request. It writes a small submission script
# into logs/, which is then run here, outside the container, where sbatch is available.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SIF="$ROOT/app/readabilityconsoleapp.sif"
if [ ! -f "$SIF" ]; then
    echo "Container image not found: $SIF"
    echo "Deploy with Deployment/local/deploy.ps1 and wait for the image build job to finish."
    exit 1
fi

mkdir -p logs
SCRIPT="$ROOT/logs/submit_$(date +%Y%m%d_%H%M%S).sh"

apptainer run "$SIF" submit "$@" --script-out "$SCRIPT"
bash "$SCRIPT"

echo
echo "Check progress with scripts/status.sh (run it now and then, not in a loop)."
echo "Job output is in logs/slurm-<job id>.out."
