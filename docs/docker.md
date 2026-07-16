# Docker Compose

One-command local Atlas: Postgres + API + UI.

**Requires Docker Compose v2.24+** (demo profile uses `depends_on.required: false`). Check with `docker compose version`.

## Quick start

```bash
cp .env.example .env
# Set OpenAI__ApiKey in .env for AI features (required for the AI panel).

docker compose up --build
```

- UI: http://localhost:5173 (hash routes, e.g. http://localhost:5173/#/tasks)
- API: http://localhost:5012
- Health: http://localhost:5012/health

Default startup applies EF migrations and leaves the database empty (schema only).

If host port `5432` is already in use (local Postgres), set `POSTGRES_PORT` in `.env` (for example `5433`).

If you change `API_PORT` or `UI_PORT`, keep `VITE_API_BASE_URL` and `CORS_ORIGIN_*` aligned, then rebuild (`docker compose up --build`). Changing `VITE_API_BASE_URL` requires rebuilding the `ui` image.

### Demo data

```bash
docker compose --profile demo up --build
```

This runs a one-shot `demo-seed` job (`--seed-demo`) before the API becomes healthy. Seeding is idempotent.

To reset demo data, remove the volume and start again:

```bash
docker compose --profile demo down -v
docker compose --profile demo up --build
```

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
| Bare Development (`dotnet run`) | `EnsureCreated()` |
| Docker Compose / non-Development | `Database.Migrate()` on API startup (and on `--seed-demo`) |

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
