# Docker Compose

One-command local Atlas: Postgres + API + Blazor WASM UI (nginx).

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

- UI: http://localhost:5173 (path routes, e.g. http://localhost:5173/tasks)
- API: http://localhost:5012
- Health: http://localhost:5012/health

Default startup applies EF migrations (with retries while Postgres becomes reachable) and leaves the database empty (schema only).

**First-run tip:** Open http://localhost:5173 and click **Continue** — that opens the dashboard. Azure DevOps is optional (login also has **Azure Setup**; setup has **Skip for now**). For sample projects/tasks/risks/members, use the demo profile below.

If host port `5432` is already in use (local Postgres), set `POSTGRES_PORT` in `.env` (for example `5433`).

If you change `API_PORT` or `UI_PORT`, keep `API_BASE_URL` and `CORS_ORIGIN_*` aligned, then rebuild (`docker compose up --build`). Changing `API_BASE_URL` requires rebuilding the `ui` image (it is baked into `wwwroot/appsettings.json`).

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
| API exits during migrate / Npgsql timeout | API connects to Postgres via the Compose `db` service hostname (`Host=db;Port=5432`) and retries migrate up to ~30s (`restart: on-failure:5`). Check `docker compose logs api` and that `db` is healthy. |
| Build fails with `MSB3552: Resource file "**/*.resx"` | Path/layout issue in the API image build. Pull latest Dockerfile (flattened `/src/Api|Core|Infrastructure` layout). Retry with `DOCKER_BUILDKIT=0 docker compose build --no-cache api`. |
| UI empty after `--profile demo` | Wait for `demo-seed` to exit 0, then refresh; or use `ATLAS_SEED_DEMO=true`. |
| Old Compose without profiles | Upgrade to Compose v2 (`docker compose version`). |

## Configuration

See `.env.example` for all variables.

| Concern | Notes |
|---|---|
| OpenAI | `OpenAI__ApiKey` is **required for AI**. CRUD works without it; the UI shows setup guidance. |
| Azure DevOps | `AzureDevopsToken` is optional. Without it, use `/tasks`, `/projects`, `/dashboard` directly. Compose sets an empty default so the image does not use the placeholder from `appsettings.json`. |
| CORS | Defaults to `http://localhost:5173` and `http://127.0.0.1:5173`. |
| UI → API | `API_BASE_URL` is a **build arg** (default `http://localhost:5012`) baked into Blazor `wwwroot/appsettings.json` so the browser calls the host-mapped API. |

## Schema strategy

| Mode | Schema |
|---|---|
| Bare Development (`dotnet run`) | `EnsureCreated()` (retried on transient connection errors) |
| Docker Compose / non-Development | `Database.Migrate()` on API startup (and on `--seed-demo`), with retries |

Do not point a Compose Postgres volume at a database that was previously created with `EnsureCreated()` (or vice versa) without resetting the volume — the two strategies should not be mixed on the same database.

## Path routes

The UI uses Blazor path routing. After Compose is up, open:

- http://localhost:5173/dashboard
- http://localhost:5173/tasks
- http://localhost:5173/projects
- http://localhost:5173/team
- http://localhost:5173/risks
- http://localhost:5173/settings

Legacy hash URLs (`/#/dashboard`) are rewritten client-side to paths until the hash-shim follow-up PR.

## Images

- `Dockerfile.api` — multi-stage .NET 10 publish, listens on `8080`
- `Dockerfile.ui` — Blazor WASM `dotnet publish` + nginx static serve on port `80`

nginx caches fingerprinted `/_framework/*` for a year and sends `Cache-Control: no-cache` for `index.html` and `blazor.boot.json`. gzip (including `gzip_static` for precompressed `.gz` from publish) is enabled. Publish did not emit `.br` files in the cutover environment, so brotli is not configured.

Secrets (`OpenAI__ApiKey`, `AzureDevopsToken`, DB password) are injected at runtime via Compose/`.env` only — they are not baked into either image.

## Compose smoke

With Compose up (`docker compose up --build` or `--profile demo`):

```bash
./scripts/compose-smoke.sh
```

Checks API `/health`, UI `/health`, then creates a task via the API and GETs it. Override `API_URL` / `UI_URL` if you changed ports. Use `SKIP_UI_HEALTH=1` when only the API is running.
