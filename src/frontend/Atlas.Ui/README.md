# Atlas.Ui (Blazor WebAssembly)

## UI layering

- **Razor pages/components** (`Pages/`, `Layout/`, `Shared/`, `Components/`) hold markup only.
- **Code-behind** (`*.razor.cs`) holds UI state, event handlers, and navigation.
- **Services** (`Services/`) own HTTP: domain services wrap the generated `IAtlasApiClient`; `AppCacheService` hydrates reads. Pages do not inject `IAtlasApiClient` or `HttpClient`.
- **Shared components** (`Shared/`, `Components/`) are reused across pages (modals, search, team tabs, `PageTitleRow`).


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

Open [http://127.0.0.1:5173](http://127.0.0.1:5173) (path routes: `/`, `/dashboard`, `/tasks`, …). Legacy hash URLs (`/#/dashboard`) are rewritten client-side to paths until the dedicated hash-shim removal PR.

## Configuration

`ApiBaseUrl` is read from `wwwroot/appsettings*.json` (default `http://localhost:5012`) and applied to `HttpClient` + the generated `AtlasApiClient`.

Docker Compose bakes `API_BASE_URL` into `wwwroot/appsettings.json` at image build time (see `Dockerfile.ui` and [docs/docker.md](../../../docs/docker.md)).

## CSS

- `wwwroot/css/index.css` — shared dark theme tokens (`#app` height)
- `wwwroot/css/atlas-app.css` — shell/components (renamed to avoid clash with Blazor `app.css`)
- `wwwroot/css/app.css` — Blazor loading spinner + error UI only

## Shell / routes

- Default layout: `Layout/ShellLayout.razor` (nav, search/quick-add, AI panel, hydration overlay)
- Entry: `Pages/Login.razor` (`/` + `/login`), `Pages/Setup.razor` (`/setup`) use `EmptyLayout`
- Pages for all former React `router.tsx` patterns under `Pages/`
- Cache: `IAppCacheService` / `AppCacheService` (`IsHydrating`, projects → risks → tasks)
- Hydration: `Program.cs` starts a single fire-and-forget `EnsureHydratedAsync()` so the cache warms before first render. That method latches one in-flight task, so it is safe to await again from page `OnInitializedAsync`. `ShellLayout` does not start a second background hydrate.

## House style

Atlas.Ui follows the EcommerceApp-derived conventions, with one documented indent exception:

- Domain HTTP wrappers live behind `Contracts/IXxxService`; pages inject interfaces, not concrete `*Service` types.
- `ServiceRegistration.AddAtlasUiServices` owns DI. `Program.cs` is host bootstrap only.
- `[Inject]` is a `_camelCase` private property on the code-behind (not `@inject`, not PascalCase inject properties). Blazor's `InjectAttribute` does not allow fields on this TFM.
- Page/modal UI state is PascalCase. Qualify instance members with `this.` in markup **values** and code-behind, not in Blazor component parameter names (`IsFocusMode="this.IsFocusMode"`, never `this.IsFocusMode="..."`).
- New/updated C# uses braced namespaces.
- Indent is **4 spaces** (see `src/.editorconfig`), not EcommerceApp tabs, so UI and backend stay consistent.

Keep NSwag `IAtlasApiClient`, `BrowserDialogs` (pages own dialogs; domain services do not call them), and Shared modals. Growth autosave persist failures are raised on `IGrowthService.PersistFailed`; `ShellLayout` is the durable listener so the alert survives page disposal during reload.

## OpenAPI / NSwag

```bash
bash scripts/regenerate-openapi.sh
```

## Playwright

E2E lives in `tests/e2e/` (Playwright config, fixtures, and specs). From the repository root:

```bash
cd tests/e2e && npm ci && npm run test:e2e
```

`playwright.config.ts` starts this Blazor host on port **5173**. Capture specs (`capture-*.spec.ts`) stay skipped unless their `ATLAS_*_SHOTS` / `ATLAS_REACT_BASELINE` env vars are set.

## Tests

From the repository root:

```bash
dotnet test src/frontend/Tests/Atlas.Ui.Tests
```

## Visual evidence

- React baseline: [`docs/migration-screenshots/react-baseline/`](../../../docs/migration-screenshots/react-baseline/)
- Blazor Phase 4: [`docs/migration-screenshots/blazor-phase4/`](../../../docs/migration-screenshots/blazor-phase4/) — `bash scripts/capture-blazor-phase4.sh`
- Cutover performance: [`docs/migration-screenshots/blazor-cutover/PERFORMANCE.md`](../../../docs/migration-screenshots/blazor-cutover/PERFORMANCE.md)
