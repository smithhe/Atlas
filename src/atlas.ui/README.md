# Atlas UI (Web)

`atlas.ui` now runs as a browser-hosted React + Vite application.

## Requirements

- Node.js 20+
- Atlas API running (default local API URL: `http://localhost:5012`)

## Environment Configuration

Create `.env.local` from `.env.example` and set:

```bash
VITE_API_BASE_URL=http://localhost:5012
```

Behavior when `VITE_API_BASE_URL` is not set:

- `localhost` / `127.0.0.1`: defaults to `http://localhost:5012`
- non-localhost hosts: defaults to same-origin

## Local Development

```bash
npm install
npm run dev
```

## Build and Preview

```bash
npm run build
npm run preview
```

## Lint

```bash
npm run lint
```

## End-to-end (Playwright)

Requires the Atlas API on `http://localhost:5012` (Postgres `atlas` / `atlas` / `change-me`). Leave `OpenAI__ApiKey` empty for the missing-key AI guidance test; other AI flows stub the conversation/SSE endpoints.

```bash
# API in another terminal (from repo root):
#   ATLAS_SEED_DEMO=true dotnet run --project src/backend/Api/Atlas.Api --launch-profile http

npm run test:e2e
```

CI runs lint + build and a separate Playwright job (see `.github/workflows/frontend-ci.yml`).

## Docker

The UI image is built from the repo root (`Dockerfile.ui` + `docker compose`). See [docs/docker.md](../../docs/docker.md).

```bash
# from repo root
docker compose up --build
# UI: http://localhost:5173/#/dashboard
```

`VITE_API_BASE_URL` is passed as a Docker build arg (default `http://localhost:5012`) so the browser calls the host-mapped API.
