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

## Docker

The UI image is built from the repo root (`Dockerfile.ui` + `docker compose`). See [docs/docker.md](../../docs/docker.md).

```bash
# from repo root
docker compose up --build
# UI: http://localhost:5173/#/dashboard
```

`VITE_API_BASE_URL` is passed as a Docker build arg (default `http://localhost:5012`) so the browser calls the host-mapped API.
