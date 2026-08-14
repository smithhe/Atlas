# Blazor cutover performance (Phase 8)

Compare Blazor WASM cold load at `/dashboard` against the frozen React baseline in [`docs/migration-screenshots/react-baseline/PERFORMANCE.json`](../react-baseline/PERFORMANCE.json).

This is **not** a hard exit gate. Document variance only; no invented numeric budget.

## Method

Same as Phase 3 React capture: Chromium, cache disabled (`Network.setCacheDisabled`), hard reload of dashboard, TTI = reload start until Dashboard title visible and Search enabled. Transfer size = sum of `Network.loadingFinished` `encodedDataLength`.

| UI | Route | Viewport |
| --- | --- | --- |
| React baseline | `/#/dashboard` | 1440×900 |
| Blazor cutover | `/dashboard` | 1440×900 |

## Results

| Metric | React (Phase 3) | Blazor (Phase 8) | Variance |
| --- | --- | --- | --- |
| Transfer size | 259592 bytes | *measured during cutover validation* | — |
| TTI (approx) | 583 ms | *measured during cutover validation* | — |
| Measured at | 2026-07-26T22:25:23.656Z | — | — |

Blazor WASM ships a larger initial payload (runtime + app assemblies) than the Vite React bundle. Cold-load transfer size is expected to be higher; TTI depends on WASM compile/startup on the test machine.

Trimming / lazy-load of Blazor assemblies was **not** applied in Phase 8 (no invented budget). Revisit only if a later measurement shows a user-visible regression on typical hardware.

See `PERFORMANCE.json` in this folder once cutover validation records Blazor numbers.
