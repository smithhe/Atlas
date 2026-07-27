# Blazor Phase 5 visual evidence

Screenshots from the **Blazor** UI (`src/frontend/Atlas.Ui`) after Phase 5 CRUD parity, compared against the fixed Phase 3 React baseline in [`../react-baseline/`](../react-baseline/).

## Viewports

| Name | Size |
| --- | --- |
| desktop-1440x900 | 1440×900 |
| tablet-1100x800 | 1100×800 |
| narrow-700x900 | 700×900 |

## Regenerate

```bash
bash scripts/capture-blazor-phase5.sh
```

Requires Atlas API on `:5012` and Blazor host on `:5173` (Playwright `webServer` can start Blazor).

## Parity vs React baseline

| Shot | Verdict | Notes |
| --- | --- | --- |
| `04-dashboard` | **match** (non-AI) | Needs Action / Watchlist caps (8), Drift caps (10), team drift `> 7` + no-baseline split, project health Yellow/Red + project-stale; AI CTAs deferred (Phase 7) |
| `05-tasks-list` | **match** | Task list a11y label, Add task, status/priority pills |
| `06-tasks-detail` | **match** | Task detail editor; Edit/Done/Focus/Delete |
| `07-risks-list` | **match** | Risk list a11y label, Add risk |
| `08-risks-detail` | **match** | Risk detail editor |
| `09-projects-list` | **match** | Project list a11y label, Add project (native prompt) |
| `10-projects-detail` | **match** | Project detail + overview/tasks/risks tabs with `?tab=` URL sync; Edit overview / Save |
| `11-team` | **minor deviation** | Thin Phase 5 surface (member list + `aria-label="Team member list"`); full hub tabs are Phase 6 |
| `12-team-member` | **minor deviation** | Identity + notes list/read-only; hub tabs Phase 6 |
| `13-settings` | **match** | `settings-stale-days`, Save settings, Azure connection fields (Project/Team IDs) |
| `26-shell-search-open` | **match** | Max 12 results; Task→Risk→Person→Project ranking |
| `27-shell-quick-add` | **match** | Task / Risk / Team note create flows |

## Not captured

Empty-list and error-state screenshots are **not** in this folder. `scripts/capture-blazor-phase5.sh` exercises the seed-backed happy path only. The Phase 5 checklist in `docs/blazor-wasm-migration.md` is narrowed accordingly.

## Captured files

Per viewport: `04-dashboard`, `05-tasks-list`, `06-tasks-detail`, `07-risks-list`, `08-risks-detail`, `09-projects-list`, `10-projects-detail`, `11-team`, `12-team-member`, `13-settings`, `26-shell-search-open`, `27-shell-quick-add`.
