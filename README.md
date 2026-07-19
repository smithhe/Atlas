# Atlas

Local engineering-manager cockpit (React UI + ASP.NET Core API + Postgres).

## Quick start (Docker)

Requires Docker Compose **v2** (`docker compose version`). Compose **v2.24+** recommended.

```bash
cp .env.example .env
docker compose up --build
```

If BuildKit/Bake fails: `COMPOSE_BAKE=false DOCKER_BUILDKIT=0 docker compose up --build`

- UI: http://localhost:5173/#/dashboard
- API: http://localhost:5012/health

Demo data: `docker compose --profile demo up --build` (seed runs in parallel; refresh if UI loads empty)

Full details and troubleshooting: [docs/docker.md](docs/docker.md).

## Local development (without Docker)

- API: `src/backend/Api/Atlas.Api` (default http://localhost:5012) — see `AGENTS.md`
- UI: `src/atlas.ui` — see [src/atlas.ui/README.md](src/atlas.ui/README.md)
