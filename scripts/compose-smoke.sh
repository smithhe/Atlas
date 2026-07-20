#!/usr/bin/env bash
# Compose smoke: API (/health) + optional UI (/health) + one task CRUD path.
# Secrets (OpenAI, Azure PAT, DB password) come from Compose/.env only — never from images.
set -euo pipefail

API_URL="${API_URL:-http://localhost:5012}"
UI_URL="${UI_URL:-http://localhost:5173}"
SKIP_UI_HEALTH="${SKIP_UI_HEALTH:-0}"

echo "==> API health: ${API_URL}/health"
curl -fsS "${API_URL}/health" | tee /tmp/atlas-api-health.json
echo

if [[ "${SKIP_UI_HEALTH}" != "1" ]]; then
  echo "==> UI health: ${UI_URL}/health"
  if curl -fsS "${UI_URL}/health"; then
    echo
  else
    echo "UI health check failed (is the ui service up?). Set SKIP_UI_HEALTH=1 to skip."
    exit 1
  fi
fi

TITLE="compose-smoke-$(date +%s)"
echo "==> Create task: ${TITLE}"
CREATE_BODY=$(cat <<EOF
{
  "title": "${TITLE}",
  "priority": "Medium",
  "status": "NotStarted",
  "estimatedDurationText": "1h",
  "estimateConfidence": "Medium",
  "notes": "compose smoke",
  "dependencyTaskIds": []
}
EOF
)

CREATE_RESP=$(curl -fsS -X POST "${API_URL}/tasks" \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json' \
  -d "${CREATE_BODY}")
echo "${CREATE_RESP}"

TASK_ID=$(printf '%s' "${CREATE_RESP}" | sed -n 's/.*"id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')
if [[ -z "${TASK_ID}" ]]; then
  echo "Failed to parse task id from create response"
  exit 1
fi

echo "==> Get task: ${TASK_ID}"
GET_RESP=$(curl -fsS "${API_URL}/tasks/${TASK_ID}" -H 'Accept: application/json')
echo "${GET_RESP}"

if ! printf '%s' "${GET_RESP}" | grep -q "${TITLE}"; then
  echo "GET task response did not include created title"
  exit 1
fi

echo "==> Compose smoke OK"
