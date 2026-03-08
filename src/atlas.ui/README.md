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
