# Atlas.Ui (Blazor WebAssembly)

Phase 3: OpenAPI/NSwag client, mapping stubs, cache hydration skeleton, and sample list-tasks on Home.

## Prerequisites

- .NET SDK **10.0.x**
- Atlas API on **http://localhost:5012**
- Postgres for the running API (default `localhost:5432`, `atlas` / `change-me`)

## Run (pinned host port **5173**)

From the repository root:

```bash
dotnet run --project src/frontend/Atlas.Ui/Atlas.Ui.csproj --launch-profile http
```

This uses `Properties/launchSettings.json` profile `http` with:

`applicationUrl`: `http://127.0.0.1:5173`

Open [http://127.0.0.1:5173](http://127.0.0.1:5173).

## Configuration

`ApiBaseUrl` is read from `wwwroot/appsettings*.json` (default `http://localhost:5012`) and applied to `HttpClient` + the generated `AtlasApiClient`.

## OpenAPI / NSwag

Regenerate committed OpenAPI + C# client (no Postgres required):

```bash
bash scripts/regenerate-openapi.sh
```

- Spec: `openapi/atlas.v1.json` (see `openapi/README.md`)
- Client: `Api/Generated/AtlasApiClient.cs` (SSE events path excluded)
- Config: `nswag.json`

## Cache / hydration

`Services/AppCacheService` mirrors React query topology (`IsHydrating`, projects → risks → tasks). Home shows a sample task list from the cache.

## React visual baseline

Phase 3 parity screenshots live at [`docs/migration-screenshots/react-baseline/`](../../../docs/migration-screenshots/react-baseline/). Regenerate with:

```bash
bash scripts/capture-react-baseline.sh
```

## Playwright

Umbrella e2e targets this host via `src/atlas.ui/playwright.config.ts` (`webServer.command` runs the command above from `src/atlas.ui`). Ported specs are listed in `tests/e2e/playwright-ported.txt` and executed with:

```bash
bash tests/e2e/run-ported-playwright.sh
```

The runner installs npm deps (`npm ci`) and Chromium on first use when missing; CI still runs `npm ci` and `npx playwright install --with-deps chromium` explicitly before invoking the script.
