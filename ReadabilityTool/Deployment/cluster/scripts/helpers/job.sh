#!/bin/bash
# The script every job runs. Submitted by the script that scripts/submit.sh writes, with the
# tool's arguments, for example: resume --checkpoint <folder> [--only-model <model>]
#
# - Models are copied to the compute node's local /tmp before loading, one at a time.
# - 10 minutes before the time limit SLURM sends USR1 (--signal=B:USR1@600). The tool is then
#   stopped cleanly (its checkpoint is kept) and the job is requeued under the same job ID,
#   so a long execution continues in a new allocation, and jobs depending on this one keep
#   waiting for it.
# - A stop because a model server kept failing (exit code 75) is requeued the same way.
set -u

ROOT="${SLURM_SUBMIT_DIR:?This script runs as a SLURM job}"
cd "$ROOT"

SIF="$ROOT/app/readabilityconsoleapp.sif"
RESUMABLE=75
MAX_REQUEUES=10

export READABILITY_MODEL_CACHE="/tmp/readability-${SLURM_JOB_ID}"
mkdir -p "$READABILITY_MODEL_CACHE"
trap 'rm -rf "$READABILITY_MODEL_CACHE"' EXIT

echo "=== $(date) job ${SLURM_JOB_ID}, restart ${SLURM_RESTART_COUNT:-0}, on $(hostname): $*"
nvidia-smi --query-gpu=name,memory.total --format=csv,noheader 2>/dev/null || true

TIME_LIMIT_NEAR=0
on_time_limit() {
    TIME_LIMIT_NEAR=1
    echo "=== $(date) time limit is near: stopping the tool cleanly"
    kill -INT "$TOOL_PID" 2>/dev/null
}
trap on_time_limit USR1

apptainer run --nv "$SIF" "$@" &
TOOL_PID=$!

# wait returns early when the signal trap runs; keep waiting until the tool has exited.
while true; do
    wait "$TOOL_PID"
    STATUS=$?
    kill -0 "$TOOL_PID" 2>/dev/null || break
done

echo "=== $(date) the tool exited with code $STATUS"

if { [ "$TIME_LIMIT_NEAR" = 1 ] || [ "$STATUS" = "$RESUMABLE" ]; }; then
    if [ "${SLURM_RESTART_COUNT:-0}" -lt "$MAX_REQUEUES" ]; then
        echo "=== requeueing job ${SLURM_JOB_ID} to continue where it stopped"
        scontrol requeue "${SLURM_JOB_ID}"
    else
        echo "=== requeued ${MAX_REQUEUES} times already; not requeueing again. Check the log, then resume by hand."
    fi
fi

exit "$STATUS"
