#!/usr/bin/env bash
# Capture Blazor Phase 5 CRUD screenshots into docs/migration-screenshots/blazor-phase5/.
#
# Prerequisites:
#   - Postgres + Atlas API on :5012
#   - .NET SDK 10.0.x
#
# Usage (repo root):
#   bash scripts/capture-blazor-phase5.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT/tests/e2e"

export PLAYWRIGHT_BASE_URL="${PLAYWRIGHT_BASE_URL:-http://localhost:5173}"
export API_BASE_URL="${API_BASE_URL:-http://localhost:5012}"
export ATLAS_BLAZOR_PHASE5_SHOTS=1

echo "==> Installing deps / Playwright Chromium (if needed)"
if [[ ! -f node_modules/@playwright/test/package.json ]]; then
  npm ci
fi
npx playwright install chromium

echo "==> Capturing Blazor Phase 5 screenshots"
npx playwright test flows/capture-blazor-phase5.spec.ts --reporter=list

echo "==> Done → docs/migration-screenshots/blazor-phase5/"
