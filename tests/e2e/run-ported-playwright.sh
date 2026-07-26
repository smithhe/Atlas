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
echo "Running ported Playwright specs (${#SPECS[@]}): ${SPECS[*]}"
npx playwright test "${SPECS[@]}"
