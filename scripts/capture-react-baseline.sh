#!/usr/bin/env bash
# Capture React visual baseline screenshots into docs/migration-screenshots/react-baseline/.
#
# Prerequisites:
#   - Postgres + Atlas API on :5012 with ATLAS_SEED_DEMO=true
#   - Node 20+
#
# Usage (repo root):
#   bash scripts/capture-react-baseline.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT/src/atlas.ui"

export PLAYWRIGHT_BASE_URL="${PLAYWRIGHT_BASE_URL:-http://localhost:5173}"
export API_BASE_URL="${API_BASE_URL:-http://localhost:5012}"
export ATLAS_REACT_BASELINE=1

echo "==> Installing deps / Playwright Chromium"
npm ci
npx playwright install chromium

echo "==> Building React UI"
npm run build

echo "==> Capturing baseline (Vite preview; React hash routes)"
npx playwright test e2e/flows/capture-react-baseline.spec.ts --reporter=list

echo "==> Done → docs/migration-screenshots/react-baseline/"
