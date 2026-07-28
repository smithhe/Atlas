#!/usr/bin/env bash
# Capture Blazor Phase 6 Team hub screenshots into docs/migration-screenshots/blazor-phase6/.
#
# Prerequisites:
#   - Postgres + Atlas API on :5012
#   - .NET SDK 10.0.x
#
# Usage (repo root):
#   bash scripts/capture-blazor-phase6.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT/src/atlas.ui"

export PLAYWRIGHT_BASE_URL="${PLAYWRIGHT_BASE_URL:-http://localhost:5173}"
export API_BASE_URL="${API_BASE_URL:-http://localhost:5012}"
export ATLAS_BLAZOR_PHASE6_SHOTS=1

echo "==> Installing deps / Playwright Chromium (if needed)"
if [[ ! -f node_modules/@playwright/test/package.json ]]; then
  npm ci
fi
npx playwright install chromium

echo "==> Capturing Blazor Phase 6 screenshots"
npx playwright test e2e/flows/capture-blazor-phase6.spec.ts --reporter=list

echo "==> Done → docs/migration-screenshots/blazor-phase6/"
