# Docker Compose

One-command local Atlas: Postgres + API + UI.

**Requires Docker Compose v2** (`docker compose version`). Compose **v2.24+** is recommended.

## Quick start

```bash
cp .env.example .env
# Set OpenAI__ApiKey in .env for AI features (required for the AI panel).

docker compose up --build
```

If BuildKit / Bake fails (overlay errors or “configured to build using Bake, but buildx isn't installed”), use the classic builder:

```bash
COMPOSE_BAKE=false DOCKER_BUILDKIT=0 docker compose up --build
```

- UI: http://localhost:5173 (hash routes, e.g. http://localhost:5173/#/tasks)
- API: http://localhost:5012
- Health: http://localhost:5012/health

Default startup applies EF migrations (with retries while Postgres becomes reachable) and leaves the database empty (schema only).

If host port `5432` is already in use (local Postgres), set `POSTGRES_PORT` in `.env` (for example `5433`).

If you change `API_PORT` or `UI_PORT`, keep `VITE_API_BASE_URL` and `CORS_ORIGIN_*` aligned, then rebuild (`docker compose up --build`). Changing `VITE_API_BASE_URL` requires rebuilding the `ui` image.

### Demo data

```bash
docker compose --profile demo up --build
```

This starts a one-shot `demo-seed` job (`--seed-demo`) **alongside** the API (the API does not wait on it). Seeding is idempotent; demo rows appear after `demo-seed` finishes — refresh the UI if it loaded empty.

Alternatively, seed from the API process itself:

```bash
ATLAS_SEED_DEMO=true docker compose up --build
```

To reset demo data, remove the volume and start again:

```bash
docker compose --profile demo down -v
docker compose --profile demo up --build
```

## Troubleshooting

| Symptom | Fix |
|---|---|
| Port `5432` already allocated | Set `POSTGRES_PORT=5433` (or another free port) in `.env`. |
| Build fails with Bake / BuildKit / overlay errors | `COMPOSE_BAKE=false DOCKER_BUILDKIT=0 docker compose up --build` |
| Build fails with `MSB3552: Resource file "**/*.resx"` | Path/layout issue in the API image build. Pull latest Dockerfile (flattened `/src/Api|Core|Infrastructure` layout). Retry with `DOCKER_BUILDKIT=0 docker compose build --no-cache api`. |
| API exits during migrate / Npgsql timeout | API connects via `host.docker.internal` (host-published Postgres port) and retries migrate up to ~30s (`restart: on-failure:5`). Check `docker compose logs api`. If host port `5432` is taken, set `POSTGRES_PORT` in `.env`. |
| UI empty after `--profile demo` | Wait for `demo-seed` to exit 0, then refresh; or use `ATLAS_SEED_DEMO=true`. |
| Old Compose without profiles | Upgrade to Compose v2 (`docker compose version`). |

## Configuration

See `.env.example` for all variables.

| Concern | Notes |
|---|---|
| OpenAI | `OpenAI__ApiKey` is **required for AI**. CRUD works without it; the UI shows setup guidance. |
| Azure DevOps | `AzureDevopsToken` is optional. Without it, use `/#/tasks`, `/#/projects`, `/#/dashboard` directly. Compose sets an empty default so the image does not use the placeholder from `appsettings.json`. |
| CORS | Defaults to `http://localhost:5173` and `http://127.0.0.1:5173`. |
| UI → API | `VITE_API_BASE_URL` is a **build arg** (default `http://localhost:5012`) so the browser calls the host-mapped API. |

## Schema strategy

| Mode | Schema |
|---|---|
| Bare Development (`dotnet run`) | `EnsureCreated()` (retried on transient connection errors) |
| Docker Compose / non-Development | `Database.Migrate()` on API startup (and on `--seed-demo`), with retries |

Do not point a Compose Postgres volume at a database that was previously created with `EnsureCreated()` (or vice versa) without resetting the volume — the two strategies should not be mixed on the same database.

## Hash routes

The UI uses a hash router. After Compose is up, open:

- http://localhost:5173/#/dashboard
- http://localhost:5173/#/tasks
- http://localhost:5173/#/projects
- http://localhost:5173/#/team
- http://localhost:5173/#/risks
- http://localhost:5173/#/settings

## Images

- `Dockerfile.api` — multi-stage .NET 10 publish, listens on `8080`
- `Dockerfile.ui` — Vite build + nginx static serve on port `80`
