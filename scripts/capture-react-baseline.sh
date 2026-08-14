#!/usr/bin/env bash
# Recapture React visual baseline screenshots into docs/migration-screenshots/react-baseline/.
#
# Phase 8 froze this baseline — do not recapture unless explicitly restoring React.
# Requires src/atlas.ui (deleted after Phase 8 validation) serving Vite preview on :5173.
#
# Prerequisites:
#   - Postgres + Atlas API on :5012 with ATLAS_SEED_DEMO=true
#   - Node 20+
#
# Usage (repo root):
#   bash scripts/capture-react-baseline.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UI_DIR="$ROOT/src/atlas.ui"
E2E_DIR="$ROOT/tests/e2e"

if [[ ! -d "$UI_DIR" ]]; then
  echo "error: src/atlas.ui is gone; the React baseline is frozen at docs/migration-screenshots/react-baseline/" >&2
  exit 1
fi

export PLAYWRIGHT_BASE_URL="${PLAYWRIGHT_BASE_URL:-http://localhost:5173}"
export API_BASE_URL="${API_BASE_URL:-http://localhost:5012}"
export ATLAS_REACT_BASELINE=1

echo "==> Building React UI"
cd "$UI_DIR"
npm ci
npm run build

echo "==> Installing Playwright in tests/e2e"
cd "$E2E_DIR"
if [[ ! -f node_modules/@playwright/test/package.json ]]; then
  npm ci
fi
npx playwright install chromium

echo "==> Starting Vite preview on 5173"
cd "$UI_DIR"
npx vite preview --host 127.0.0.1 --port 5173 >/tmp/atlas-react-preview.log 2>&1 &
PREVIEW_PID=$!
trap 'kill "$PREVIEW_PID" 2>/dev/null || true' EXIT

for i in $(seq 1 60); do
  if curl -fsS http://127.0.0.1:5173 >/dev/null 2>&1; then
    break
  fi
  sleep 1
  if [[ "$i" -eq 60 ]]; then
    echo "error: Vite preview did not start" >&2
    cat /tmp/atlas-react-preview.log >&2 || true
    exit 1
  fi
done

echo "==> Capturing baseline (Vite preview; React hash routes)"
cd "$E2E_DIR"
npx playwright test flows/capture-react-baseline.spec.ts --reporter=list

echo "==> Done → docs/migration-screenshots/react-baseline/"
