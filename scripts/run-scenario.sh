#!/usr/bin/env bash
# Run a scenario JSON headlessly. Renders via Xvfb so scenario captures work.
# Usage: scripts/run-scenario.sh tests/scenarios/arena.json
set -euo pipefail
SCENARIO="${1:?usage: run-scenario.sh <scenario.json>}"
cd "$(dirname "$0")/.."
exec xvfb-run -a -s "-screen 0 1280x720x24" godot --path . \
  --audio-driver Dummy --rendering-driver opengl3 \
  --script src/Nodes/ScenarioRunner.cs -- --scenario="$SCENARIO"
