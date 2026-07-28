# Blazor Phase 6 visual evidence — Team hub

Screenshots from the **Blazor** UI (`src/frontend/Atlas.Ui`) after Phase 6 Team hub parity, compared against the fixed Phase 3 React baseline in [`../react-baseline/`](../react-baseline/).

## Viewports

| Name | Size |
| --- | --- |
| desktop-1440x900 | 1440×900 |
| tablet-1100x800 | 1100×800 |
| narrow-700x900 | 700×900 |

## Regenerate

```bash
bash scripts/capture-blazor-phase6.sh
```

Requires Atlas API on `:5012` and Blazor host on `:5173` (Playwright `webServer` can start Blazor).

## Parity vs React baseline

| Shot | Verdict | Notes |
| --- | --- | --- |
| `11-team-list` | **match** | `teamGrid` list + center pane; `aria-label="Team member list"`; Focus/Exit focus |
| `12-team-member-overview` | **match** | Member tabs (`aria-label="Member tabs"`); Overview profile/focus/signals/pinned/activity |
| `12b-team-member-notes` | **match** | Notes tab a11y; `+ New` → `New note` dialog; `.memberNotesRow`; ADO chips; Open note / Open full page |
| `12c-team-member-work-items` | **match** (seed-dependent) | Work items tab filters; demo seed may have few/no Azure items |
| `12d-team-member-risks` | **match** (seed-dependent) | Risks tab + Add risk; demo seed may lack member risks |
| `12e-team-member-growth` | **match** (seed-dependent) | Growth overview; goals/skills/themes may be empty until ensure/create |
| `20-team-note-detail` | **match** | `aria-label="Note fields"`; Title/Type/ADO/PR; Edit/Save; path `/team/{id}/notes/{noteId}` |

## Seed / data caveats (same as React baseline)

- Demo seed often lacks Azure work items, team-member risks, and growth goals.
- Capture creates a note for `20-team-note-detail` (same pattern as `team-note-ado-pr`).
- Note markdown rendering uses a lightweight plain pre-wrap block until Phase 7 Markdig.

## Captured files

Per viewport: `11-team-list`, `12-team-member-overview`, `12b-team-member-notes`, `12c-team-member-work-items`, `12d-team-member-risks`, `12e-team-member-growth`, `20-team-note-detail`.
