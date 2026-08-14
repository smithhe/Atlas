# Blazor cutover performance (Phase 8)

Compare Blazor WASM cold load at `/dashboard` against the frozen React baseline in [`docs/migration-screenshots/react-baseline/PERFORMANCE.json`](../react-baseline/PERFORMANCE.json).

This is **not** a hard exit gate. Document variance only; no invented numeric budget.

## Method

Same as Phase 3 React capture: Chromium, cache disabled (`Network.setCacheDisabled`), hard reload of dashboard, TTI = reload start until Dashboard title visible and Search enabled. Transfer size = sum of `Network.loadingFinished` `encodedDataLength`.

Blazor was measured against the **published** `wwwroot` served by nginx (`gzip_static` on), matching Docker/nginx delivery.

| UI | Route | Viewport |
| --- | --- | --- |
| React baseline | `/#/dashboard` | 1440×900 |
| Blazor cutover | `/dashboard` | 1440×900 |

## Results

| Metric | React (Phase 3) | Blazor (Phase 8) | Variance |
| --- | --- | --- | --- |
| Transfer size | 259592 bytes (~254 KiB) | 4725330 bytes (~4.5 MiB) | **~18.2× larger** |
| TTI (approx) | 583 ms | 1055 ms | **+472 ms (~1.8×)** |
| Measured at | 2026-07-26T22:25:23.656Z | 2026-08-14T02:57:45.398Z | — |

Blazor WASM ships the .NET runtime plus app assemblies, so cold-load transfer size is expected to be much higher than the Vite React bundle. TTI on the test machine stayed near one second.

`dotnet publish` reports: *Publishing without optimizations. Although it's optional for Blazor, we strongly recommend using `wasm-tools`.* The cutover `Dockerfile.ui` uses `mcr.microsoft.com/dotnet/sdk:10.0` without installing that workload, so these numbers match what Compose will ship.

Trimming / lazy-load of Blazor assemblies was **not** applied in Phase 8 (no invented budget). Revisit `wasm-tools` + lazy load if a later measurement shows a user-visible regression on typical hardware.

See `PERFORMANCE.json` in this folder for the raw Blazor measurement.
