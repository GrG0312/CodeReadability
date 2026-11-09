#!/bin/bash
# Shows your jobs and unfinished executions once.
# Run it again later rather than in a loop: the cluster rules ask for at least 15 seconds
# between queue queries.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

squeue --me -o "%.10i %.40j %.11P %.9T %.10M %.12l %R"

echo
echo "Unfinished executions (continue one with: scripts/submit.sh resume --checkpoint <folder>):"
if compgen -G "$ROOT/checkpoints/*" > /dev/null; then
    ls -1d "$ROOT"/checkpoints/*
else
    echo "  none"
fi
