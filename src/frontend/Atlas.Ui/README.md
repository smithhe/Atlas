# Atlas.Ui (Blazor WebAssembly)

Phase 4: shell layout, path routes, React CSS, hash shim, login/setup stubs, and first Playwright ports.

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

Open [http://127.0.0.1:5173](http://127.0.0.1:5173) (path routes: `/`, `/dashboard`, …). Legacy hash URLs (`/#/dashboard`) are rewritten client-side to paths.

## Configuration

`ApiBaseUrl` is read from `wwwroot/appsettings*.json` (default `http://localhost:5012`) and applied to `HttpClient` + the generated `AtlasApiClient`.

## CSS

- `wwwroot/css/index.css` — React `index.css` (dark theme tokens; `#app` height)
- `wwwroot/css/atlas-app.css` — React `App.css` (shell/components; renamed to avoid clash with Blazor `app.css`)
- `wwwroot/css/app.css` — Blazor loading spinner + error UI only

## Shell / routes

- Default layout: `Layout/ShellLayout.razor` (nav, search/quick-add stubs, AI panel stub, hydration overlay)
- Entry: `Pages/Login.razor` (`/` + `/login`), `Pages/Setup.razor` (`/setup`) use `EmptyLayout`
- Stub pages for all React `router.tsx` patterns under `Pages/`
- Cache: `Services/AppCacheService` (`IsHydrating`, projects → risks → tasks)

## OpenAPI / NSwag

```bash
bash scripts/regenerate-openapi.sh
```

## Playwright

Umbrella e2e targets this host via `src/atlas.ui/playwright.config.ts`. Ported specs (Phase 4):

- `e2e/flows/blazor-host.spec.ts`
- `e2e/flows/login-dashboard.spec.ts`
- `e2e/flows/shell-nav.spec.ts`
- `e2e/flows/dashboard-nav.spec.ts`

```bash
bash tests/e2e/run-ported-playwright.sh
```

## Visual evidence

- React baseline: [`docs/migration-screenshots/react-baseline/`](../../../docs/migration-screenshots/react-baseline/)
- Blazor Phase 4: [`docs/migration-screenshots/blazor-phase4/`](../../../docs/migration-screenshots/blazor-phase4/) — `bash scripts/capture-blazor-phase4.sh`
