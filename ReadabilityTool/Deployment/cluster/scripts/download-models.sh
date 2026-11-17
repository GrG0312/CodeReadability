#!/bin/bash
#SBATCH --job-name=rd-download-models
#SBATCH --partition=small
#SBATCH --cpus-per-task=2
#SBATCH --mem=4G
#SBATCH --time=12:00:00
#SBATCH --output=logs/download-models-%j.out
#
# Downloads the experiment's model files into models/ on a compute node; never download on nipg1.
# On nipg1: cd ~/readability && sbatch scripts/download-models.sh
# Needs no GPU. About 152 GB; wget -c continues partial files, so if the job stops half-way,
# submit it again. The description file is uploaded separately (Deployment/models/descriptions.json).
#
# Every model comes as the standard 4-bit (Q4_K_M) and 8-bit (Q8_0) file. The one exception is
# Qwen3.8-27B: its repository has no plain Q4_K_M, so its 4-bit file is the Unsloth Dynamic UD-Q4_K_M.
set -euo pipefail

ROOT="${SLURM_SUBMIT_DIR:?This script runs as a SLURM job}"
cd "$ROOT/models"

BASE="https://huggingface.co/unsloth"
FILES=(
    "Qwen3.8-27B-GGUF/Qwen3.8-27B-UD-Q4_K_M.gguf"
    "Qwen3.8-27B-GGUF/Qwen3.8-27B-Q8_0.gguf"
    "gemma-4-31B-it-GGUF/gemma-4-31B-it-Q4_K_M.gguf"
    "gemma-4-31B-it-GGUF/gemma-4-31B-it-Q8_0.gguf"
    "gemma-4-12b-it-GGUF/gemma-4-12b-it-Q4_K_M.gguf"
    "gemma-4-12b-it-GGUF/gemma-4-12b-it-Q8_0.gguf"
    "gemma-4-E4B-it-GGUF/gemma-4-E4B-it-Q4_K_M.gguf"
    "gemma-4-E4B-it-GGUF/gemma-4-E4B-it-Q8_0.gguf"
    "Qwen3.5-9B-GGUF/Qwen3.5-9B-Q4_K_M.gguf"
    "Qwen3.5-9B-GGUF/Qwen3.5-9B-Q8_0.gguf"
    "Qwen3.5-4B-GGUF/Qwen3.5-4B-Q4_K_M.gguf"
    "Qwen3.5-4B-GGUF/Qwen3.5-4B-Q8_0.gguf"
)

echo "=== $(date) downloading ${#FILES[@]} files on $(hostname)"
for file in "${FILES[@]}"; do
    wget -nv -c "$BASE/${file%%/*}/resolve/main/${file#*/}"
done

# Expected sizes (GiB, 4-bit / 8-bit): 27B 15.3 / 27.1, 31B 17.1 / 30.4, 12B 6.6 / 11.8,
# E4B 4.6 / 7.6, 9B 5.3 / 8.9, 4B 2.6 / 4.2.
echo "=== $(date) done"
ls -lh *.gguf

# The tool tests every .gguf in this folder, so files of an earlier model list must go.
for present in *.gguf; do
    keep=false
    for file in "${FILES[@]}"; do
        [[ "$present" == "${file#*/}" ]] && keep=true
    done
    $keep || echo "WARNING: $present is not on the model list; delete it before the run."
done
