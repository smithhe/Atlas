# Blazor Phase 4 visual evidence

Screenshots from the **Blazor** UI (`src/frontend/Atlas.Ui`) with path routes, compared against the fixed Phase 3 React baseline in [`../react-baseline/`](../react-baseline/).

## Viewports

| Name | Size |
| --- | --- |
| desktop-1440x900 | 1440×900 |
| tablet-1100x800 | 1100×800 |
| narrow-700x900 | 700×900 |

## Regenerate

```bash
bash scripts/capture-blazor-phase4.sh
```

Requires Atlas API on `:5012` and Blazor host on `:5173` (Playwright `webServer` starts Blazor).

## Parity vs React baseline

| Shot | Verdict | Notes |
| --- | --- | --- |
| `01-login-index` | **match** | EmptyLayout Login; dark theme; Continue + Azure Setup |
| `02-login` | **match** | Same as index (`/login`) |
| `03-setup` | **minor deviation** | Header/title/Skip match; full Azure wizard form stubbed (Phase later) |
| `04-dashboard` | **minor deviation** | Shell chrome + Needs Action open risks match selectors; severity/dot from mapped DTO (`dot-{severity}`); full dashboard grid (watchlist, commitment, team pulse, drift) deferred to Phase 5 |
| `26-shell-search-open` | **minor deviation** | Combobox + focus styling present; result ranking/navigation deferred to Phase 5 (stub empty panel) |
| `27-shell-quick-add` | **match** | Modal title “Quick Add”, Task/Risk/Team note chips, Create/Cancel; create action stubbed |

Other checklist routes (tasks/projects/risks/team CRUD, AI streaming) are **N/A** for Phase 4 shell stubs — covered in later phases.

## Captured files

Per viewport:

- `01-login-index.png`
- `02-login.png`
- `03-setup.png`
- `04-dashboard.png`
- `26-shell-search-open.png`
- `27-shell-quick-add.png`
