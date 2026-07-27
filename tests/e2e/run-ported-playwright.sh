#!/usr/bin/env bash
# Invoke from repo root: bash tests/e2e/run-ported-playwright.sh
# Do not assume executable bit. Never run bare "playwright test".
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MANIFEST="${ROOT}/tests/e2e/playwright-ported.txt"
UI_DIR="${ROOT}/src/atlas.ui"

if [[ ! -f "${MANIFEST}" ]]; then
  echo "error: manifest missing: ${MANIFEST}" >&2
  exit 1
fi

mapfile -t SPECS < <(grep -vE '^\s*(#|$)' "${MANIFEST}" | sed 's/[[:space:]]*$//' | sed '/^$/d' || true)

if [[ ${#SPECS[@]} -eq 0 ]]; then
  echo "error: manifest empty or whitespace-only: ${MANIFEST}" >&2
  exit 1
fi

cd "${UI_DIR}"

if [[ ! -f node_modules/@playwright/test/package.json ]]; then
  echo "Installing npm dependencies (npm ci) in ${UI_DIR}..."
  npm ci
fi

# Install Chromium only when missing (no --with-deps; avoids privileged OS packages each run).
if ! node -e "const { chromium } = require('@playwright/test'); require('fs').accessSync(chromium.executablePath());" 2>/dev/null; then
  echo "Installing Playwright Chromium (user cache)..."
  if ! npx playwright install chromium; then
    cat >&2 <<'EOF'
error: Failed to install Playwright Chromium.

Local Linux: install browser system dependencies once, then re-run:
  cd src/atlas.ui && npx playwright install-deps chromium
  # or: npx playwright install --with-deps chromium

CI (ubuntu): umbrella workflow runs "npx playwright install --with-deps chromium" before this script.
EOF
    exit 1
  fi
fi

echo "Running ported Playwright specs (${#SPECS[@]}): ${SPECS[*]}"
npx playwright test "${SPECS[@]}"
