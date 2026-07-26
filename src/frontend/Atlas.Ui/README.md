# Atlas.Ui (Blazor WebAssembly)

Phase 2 scaffold for the React → Blazor WASM migration. Feature pages land in later phases.

## Prerequisites

- .NET SDK **10.0.x**
- Atlas API on **http://localhost:5012** (optional for the placeholder page; required once API calls are wired)

## Run (pinned host port **5173**)

From the repository root:

```bash
dotnet run --project src/frontend/Atlas.Ui/Atlas.Ui.csproj --launch-profile http
```

This uses `Properties/launchSettings.json` profile `http` with:

`applicationUrl`: `http://127.0.0.1:5173`

Open [http://127.0.0.1:5173](http://127.0.0.1:5173).

## Configuration

`ApiBaseUrl` is read from `wwwroot/appsettings*.json` (default `http://localhost:5012`) and applied to the shared `HttpClient`.

## Playwright

Umbrella e2e targets this host via `src/atlas.ui/playwright.config.ts` (`webServer.command` runs the command above from `src/atlas.ui`). Ported specs are listed in `tests/e2e/playwright-ported.txt` and executed with:

```bash
bash tests/e2e/run-ported-playwright.sh
```
