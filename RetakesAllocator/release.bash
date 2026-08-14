#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if ! command -v pwsh >/dev/null 2>&1; then
  echo "PowerShell (pwsh) is required to build the complete release package." >&2
  exit 1
fi

exec pwsh -NoProfile -File "$ROOT_DIR/compile.ps1"
