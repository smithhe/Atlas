# React visual baseline (Phase 3)

Captured from the **React** UI (`src/atlas.ui`) with hash routes, against API + Postgres with `ATLAS_SEED_DEMO=true`.

## Viewports

| Name | Size |
| --- | --- |
| desktop-1440x900 | 1440×900 |
| tablet-1100x800 | 1100×800 |
| narrow-700x900 | 700×900 |

## Regenerate

The React UI was deleted in Phase 8 (`57e85657a551c364d3cc7ded33778bc38940aea1`). Do not recapture. Restore `src/atlas.ui` with `git revert` of that commit if a recapture is ever required.

## Performance (`/#/dashboard`)

| Metric | Value |
| --- | --- |
| Transfer size | 259592 bytes |
| TTI (approx) | 583 ms |

See `PERFORMANCE.json` for raw measurement details. Not a hard exit gate — document variance at Phase 8.

## Gaps / not captured

- team work-item detail (/#/team/:memberId/work-items/:workItemId) — demo seed has no azureWorkItems
- team member risk detail (/#/team/:memberId/risks/:teamMemberRiskId) — demo seed has no member risks
- growth goal detail (/#/team/:memberId/growth/goals/:goalId) — demo seed has no growth records (GET growth → 404)

Also not captured (require fault injection or transient UI):

- Empty-list states (demo seed populates lists)
- Validation / server-error overlays
- AI panel streaming transcript (Phase 7+)
- Hydration overlay freeze frame

Per-viewport gap lines recorded during capture (for debugging):

- desktop-1440x900: team work-item detail (/#/team/:memberId/work-items/:workItemId) — demo seed has no azureWorkItems
- desktop-1440x900: team member risk detail (/#/team/:memberId/risks/:teamMemberRiskId) — demo seed has no member risks
- desktop-1440x900: growth goal detail (/#/team/:memberId/growth/goals/:goalId) — demo seed has no growth records (GET growth → 404)
- tablet-1100x800: team work-item detail (/#/team/:memberId/work-items/:workItemId) — demo seed has no azureWorkItems
- tablet-1100x800: team member risk detail (/#/team/:memberId/risks/:teamMemberRiskId) — demo seed has no member risks
- tablet-1100x800: growth goal detail (/#/team/:memberId/growth/goals/:goalId) — demo seed has no growth records (GET growth → 404)
- narrow-700x900: team work-item detail (/#/team/:memberId/work-items/:workItemId) — demo seed has no azureWorkItems
- narrow-700x900: team member risk detail (/#/team/:memberId/risks/:teamMemberRiskId) — demo seed has no member risks
- narrow-700x900: growth goal detail (/#/team/:memberId/growth/goals/:goalId) — demo seed has no growth records (GET growth → 404)

## Captured files

- `desktop-1440x900/01-login-index.png`
- `desktop-1440x900/02-login.png`
- `desktop-1440x900/03-setup.png`
- `desktop-1440x900/04-dashboard.png`
- `desktop-1440x900/05-tasks-list.png`
- `desktop-1440x900/06-tasks-split.png`
- `desktop-1440x900/07-tasks-focus.png`
- `desktop-1440x900/08-projects-list.png`
- `desktop-1440x900/09-projects-detail-overview.png`
- `desktop-1440x900/10-projects-tab-tasks.png`
- `desktop-1440x900/11-projects-tab-risks.png`
- `desktop-1440x900/12-risks-list.png`
- `desktop-1440x900/13-risks-detail.png`
- `desktop-1440x900/14-team.png`
- `desktop-1440x900/15-team-member.png`
- `desktop-1440x900/16-team-member-notes.png`
- `desktop-1440x900/17-team-member-work-items.png`
- `desktop-1440x900/18-team-member-risks.png`
- `desktop-1440x900/19-team-member-growth.png`
- `desktop-1440x900/20-team-note-detail.png`
- `desktop-1440x900/24-settings.png`
- `desktop-1440x900/25-settings-azure-import.png`
- `desktop-1440x900/26-shell-search-open.png`
- `desktop-1440x900/27-shell-quick-add.png`
- `tablet-1100x800/01-login-index.png`
- `tablet-1100x800/02-login.png`
- `tablet-1100x800/03-setup.png`
- `tablet-1100x800/04-dashboard.png`
- `tablet-1100x800/05-tasks-list.png`
- `tablet-1100x800/06-tasks-split.png`
- `tablet-1100x800/07-tasks-focus.png`
- `tablet-1100x800/08-projects-list.png`
- `tablet-1100x800/09-projects-detail-overview.png`
- `tablet-1100x800/10-projects-tab-tasks.png`
- `tablet-1100x800/11-projects-tab-risks.png`
- `tablet-1100x800/12-risks-list.png`
- `tablet-1100x800/13-risks-detail.png`
- `tablet-1100x800/14-team.png`
- `tablet-1100x800/15-team-member.png`
- `tablet-1100x800/16-team-member-notes.png`
- `tablet-1100x800/17-team-member-work-items.png`
- `tablet-1100x800/18-team-member-risks.png`
- `tablet-1100x800/19-team-member-growth.png`
- `tablet-1100x800/20-team-note-detail.png`
- `tablet-1100x800/24-settings.png`
- `tablet-1100x800/25-settings-azure-import.png`
- `tablet-1100x800/26-shell-search-open.png`
- `tablet-1100x800/27-shell-quick-add.png`
- `narrow-700x900/01-login-index.png`
- `narrow-700x900/02-login.png`
- `narrow-700x900/03-setup.png`
- `narrow-700x900/04-dashboard.png`
- `narrow-700x900/05-tasks-list.png`
- `narrow-700x900/06-tasks-split.png`
- `narrow-700x900/07-tasks-focus.png`
- `narrow-700x900/08-projects-list.png`
- `narrow-700x900/09-projects-detail-overview.png`
- `narrow-700x900/10-projects-tab-tasks.png`
- `narrow-700x900/11-projects-tab-risks.png`
- `narrow-700x900/12-risks-list.png`
- `narrow-700x900/13-risks-detail.png`
- `narrow-700x900/14-team.png`
- `narrow-700x900/15-team-member.png`
- `narrow-700x900/16-team-member-notes.png`
- `narrow-700x900/17-team-member-work-items.png`
- `narrow-700x900/18-team-member-risks.png`
- `narrow-700x900/19-team-member-growth.png`
- `narrow-700x900/20-team-note-detail.png`
- `narrow-700x900/24-settings.png`
- `narrow-700x900/25-settings-azure-import.png`
- `narrow-700x900/26-shell-search-open.png`
- `narrow-700x900/27-shell-quick-add.png`
