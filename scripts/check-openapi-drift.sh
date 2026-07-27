#!/usr/bin/env bash
# Fail if FastEndpoints OpenAPI export drifts from openapi/atlas.v1.json.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

API_PROJECT="src/backend/Api/Atlas.Api/Atlas.Api.csproj"
EXPORT_FILE="src/backend/Api/Atlas.Api/wwwroot/openapi/v1.json"
COMMITTED="openapi/atlas.v1.json"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
# Dedicated port so a local API on :5012 does not block export.
export ASPNETCORE_URLS="${ATLAS_OPENAPI_EXPORT_URLS:-http://127.0.0.1:5055}"

if [ ! -f "$COMMITTED" ]; then
  echo "error: missing committed OpenAPI at ${COMMITTED}" >&2
  exit 1
fi

mkdir -p "$(dirname "$EXPORT_FILE")"
rm -f "$EXPORT_FILE"

echo "==> Exporting OpenAPI for drift check (${ASPNETCORE_URLS})"
# Environment.Exit from the API can yield odd statuses; success is the export file.
set +e
dotnet run --project "$API_PROJECT" --no-launch-profile -- --export-swagger-docs true
set -e

if [ ! -f "$EXPORT_FILE" ]; then
  echo "error: export did not produce ${EXPORT_FILE}" >&2
  exit 1
fi

NORMALIZED="$(mktemp "${TMPDIR:-/tmp}/atlas-openapi-norm-XXXXXX.json")"
cleanup() { rm -f "$NORMALIZED"; }
trap cleanup EXIT

python3 - "$EXPORT_FILE" "$NORMALIZED" <<'PY'
import json, sys
src, dst = sys.argv[1], sys.argv[2]
with open(src, encoding="utf-8") as f:
    doc = json.load(f)
with open(dst, "w", encoding="utf-8", newline="\n") as f:
    json.dump(doc, f, indent=2, sort_keys=True, ensure_ascii=False)
    f.write("\n")
PY

if ! cmp -s "$NORMALIZED" "$COMMITTED"; then
  echo "error: OpenAPI drift detected. Update with: bash scripts/regenerate-openapi.sh" >&2
  diff -u "$COMMITTED" "$NORMALIZED" | head -n 80 >&2 || true
  exit 1
fi

echo "OpenAPI drift check passed (${COMMITTED})"
