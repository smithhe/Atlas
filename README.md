# Atlas

Local engineering-manager cockpit (Blazor WebAssembly UI + ASP.NET Core API + Postgres).

React → Blazor WASM cutover: [docs/blazor-wasm-migration.md](docs/blazor-wasm-migration.md). Frozen React visual baseline: [docs/migration-screenshots/react-baseline/](docs/migration-screenshots/react-baseline/).

## Quick start (Docker)

Requires Docker Compose **v2** (`docker compose version`). Compose **v2.24+** recommended.

```bash
cp .env.example .env
docker compose up --build
```

If BuildKit/Bake fails: `COMPOSE_BAKE=false DOCKER_BUILDKIT=0 docker compose up --build`

- UI: http://localhost:5173 — click **Continue** (Azure optional)
- API: http://localhost:5012/health

Demo data: `docker compose --profile demo up --build` (seed runs in parallel; refresh if UI loads empty)

Full details and troubleshooting: [docs/docker.md](docs/docker.md).

## Local development (without Docker)

- API: `src/backend/Api/Atlas.Api` (default http://localhost:5012) — see `AGENTS.md`
- UI (Blazor WASM): `src/frontend/Atlas.Ui` — see [src/frontend/Atlas.Ui/README.md](src/frontend/Atlas.Ui/README.md)
- Playwright: `tests/e2e/` — `cd tests/e2e && npm ci && npm run test:e2e`

## OpenAPI

Committed spec: [`openapi/atlas.v1.json`](openapi/atlas.v1.json). Regenerate (no Postgres required):

```bash
bash scripts/regenerate-openapi.sh
```

## License

Atlas is licensed under the [PolyForm Noncommercial License 1.0.0](LICENSE).

You may use Atlas without a paid license for personal, hobby, research, education, charity, and government use, as defined in the license. Any commercial use — including use at a for-profit company — requires a separate commercial license. Atlas is source-available software; it is not OSI-approved open source.

For commercial licensing, open a [GitHub issue](https://github.com/smithhe/Atlas/issues). Contributions are accepted under the same license.
