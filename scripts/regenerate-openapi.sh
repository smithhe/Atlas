#!/usr/bin/env bash
# Regenerate openapi/atlas.v1.json from the API export and refresh the NSwag C# client.
#
# Prerequisites:
#   - .NET SDK 10.0.x
#
# Usage (from repo root):
#   bash scripts/regenerate-openapi.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

API_PROJECT="src/backend/Api/Atlas.Api/Atlas.Api.csproj"
EXPORT_DIR="src/backend/Api/Atlas.Api/wwwroot/openapi"
EXPORT_FILE="${EXPORT_DIR}/v1.json"
COMMITTED="openapi/atlas.v1.json"
COMMITTED_ABS="${ROOT}/${COMMITTED}"
BLAZOR_PROJECT="src/frontend/Atlas.Ui/Atlas.Ui.csproj"
NSWAG_CONFIG_ABS="${ROOT}/src/frontend/Atlas.Ui/nswag.json"
GENERATED_DIR="src/frontend/Atlas.Ui/Api/Generated"
CLIENT_OUT="${ROOT}/src/frontend/Atlas.Ui/Api/Generated/AtlasApiClient.cs"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
# Dedicated port so a local API on :5012 does not block export.
export ASPNETCORE_URLS="${ATLAS_OPENAPI_EXPORT_URLS:-http://127.0.0.1:5055}"

mkdir -p openapi "$EXPORT_DIR" "$GENERATED_DIR"

echo "==> Exporting OpenAPI via FastEndpoints (--export-swagger-docs true) on ${ASPNETCORE_URLS}"
rm -f "$EXPORT_FILE"
set +e
dotnet run --project "$API_PROJECT" --no-launch-profile -- --export-swagger-docs true
set -e

if [ ! -f "$EXPORT_FILE" ]; then
  echo "error: expected export file missing: ${EXPORT_FILE}" >&2
  ls -la "$EXPORT_DIR" >&2 || true
  exit 1
fi

echo "==> Normalizing to ${COMMITTED}"
python3 - "$EXPORT_FILE" "$COMMITTED_ABS" <<'PY'
import json, sys
src, dst = sys.argv[1], sys.argv[2]
with open(src, encoding="utf-8") as f:
    doc = json.load(f)
with open(dst, "w", encoding="utf-8", newline="\n") as f:
    json.dump(doc, f, indent=2, sort_keys=True, ensure_ascii=False)
    f.write("\n")
PY

echo "==> Preparing NSwag input (strip SSE events path; EventSource stays hand-written)"
NSWAG_INPUT="$(mktemp "${TMPDIR:-/tmp}/atlas-openapi-XXXXXX.json")"
NSWAG_RUNTIME_CONFIG="$(mktemp "${TMPDIR:-/tmp}/atlas-nswag-XXXXXX.json")"
cleanup() {
  rm -f "$NSWAG_INPUT" "$NSWAG_RUNTIME_CONFIG"
}
trap cleanup EXIT

python3 - "$COMMITTED_ABS" "$NSWAG_INPUT" <<'PY'
import json, sys
src, dst = sys.argv[1], sys.argv[2]
with open(src, encoding="utf-8") as f:
    doc = json.load(f)
paths = doc.get("paths") or {}
for key in list(paths):
    if "/ai/sessions/" in key and key.rstrip("/").endswith("/events"):
        del paths[key]
with open(dst, "w", encoding="utf-8", newline="\n") as f:
    json.dump(doc, f, indent=2, sort_keys=True, ensure_ascii=False)
    f.write("\n")
PY

python3 - "$NSWAG_CONFIG_ABS" "$NSWAG_INPUT" "$NSWAG_RUNTIME_CONFIG" "$CLIENT_OUT" <<'PY'
import json, sys
cfg_path, doc_path, out_cfg, client_out = sys.argv[1:5]
with open(cfg_path, encoding="utf-8") as f:
    cfg = json.load(f)
cfg["documentGenerator"]["fromDocument"]["url"] = doc_path
cfg["documentGenerator"]["fromDocument"]["json"] = ""
cfg["codeGenerators"]["openApiToCSharpClient"]["output"] = client_out
with open(out_cfg, "w", encoding="utf-8") as f:
    json.dump(cfg, f, indent=2)
    f.write("\n")
PY

echo "==> Generating NSwag C# client -> ${CLIENT_OUT}"
NUGET_ROOT="$(dotnet nuget locals global-packages --list | awk -F': ' '/global-packages/{print $2}')"
NSWAG_DLL="${NUGET_ROOT%/}/nswag.msbuild/14.6.3/tools/Net100/dotnet-nswag.dll"
if [ ! -f "$NSWAG_DLL" ]; then
  echo "error: NSwag.MSBuild Net100 tool not found at ${NSWAG_DLL}" >&2
  echo "Restore the Blazor project first: dotnet restore ${BLAZOR_PROJECT}" >&2
  exit 1
fi
dotnet "$NSWAG_DLL" run "$NSWAG_RUNTIME_CONFIG"

if [ ! -f "$CLIENT_OUT" ]; then
  echo "error: generated client missing: ${CLIENT_OUT}" >&2
  exit 1
fi

echo "==> Done"
echo "    OpenAPI: ${COMMITTED}"
echo "    Client:  ${CLIENT_OUT}"
