#!/usr/bin/env bash
# Runs DeskTodo locally from source (macOS/Linux dev loop) — a thin wrapper
# around `dotnet run` so contributors don't need to remember the project
# path. Any extra arguments are passed straight through to the app.
#
# Usage: scripts/run.sh [-- app-args...]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
APP_CSPROJ="$REPO_ROOT/src/DeskTodo.App/DeskTodo.App.csproj"

dotnet run --project "$APP_CSPROJ" "$@"
