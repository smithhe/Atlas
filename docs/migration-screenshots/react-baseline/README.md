# React visual baseline (Phase 3)

Captured from the **React** UI (`src/atlas.ui`) with hash routes, against API + Postgres with `ATLAS_SEED_DEMO=true`.

## Viewports

| Name | Size |
| --- | --- |
| desktop-1440x900 | 1440×900 |
| tablet-1100x800 | 1100×800 |
| narrow-700x900 | 700×900 |

## Regenerate

```bash
bash scripts/capture-react-baseline.sh
```

Requires Atlas API on `:5012` with demo seed and Postgres.

## Performance (`/#/dashboard`)

| Metric | Value |
| --- | --- |
| Transfer size | 259592 bytes |
| TTI (approx) | 581 ms |

See `PERFORMANCE.json` for raw measurement details. Not a hard exit gate — document variance at Phase 8.

## Gaps / not captured

- (none recorded during this run)

Also not captured (require fault injection or transient UI):

- Empty-list states (demo seed populates lists)
- Validation / server-error overlays
- AI panel streaming transcript (Phase 7+)
- Hydration overlay freeze frame

## Captured files

- `desktop-1440x900/01-login-index.png`
- `desktop-1440x900/02-login.png`
- `desktop-1440x900/03-setup.png`
- `desktop-1440x900/04-dashboard.png`
- `desktop-1440x900/05-tasks-list.png`
- `desktop-1440x900/06-tasks-detail.png`
- `desktop-1440x900/07-projects-list.png`
- `desktop-1440x900/08-projects-detail-overview.png`
- `desktop-1440x900/09-projects-tab-tasks.png`
- `desktop-1440x900/10-projects-tab-risks.png`
- `desktop-1440x900/11-risks-list.png`
- `desktop-1440x900/12-risks-detail.png`
- `desktop-1440x900/13-team.png`
- `desktop-1440x900/14-team-member.png`
- `desktop-1440x900/15-team-member-notes.png`
- `desktop-1440x900/16-team-member-work-items.png`
- `desktop-1440x900/17-team-member-risks.png`
- `desktop-1440x900/18-team-member-growth.png`
- `desktop-1440x900/19-settings.png`
- `desktop-1440x900/20-settings-azure-import.png`
- `desktop-1440x900/21-shell-search-open.png`
- `desktop-1440x900/22-shell-quick-add.png`
- `tablet-1100x800/01-login-index.png`
- `tablet-1100x800/02-login.png`
- `tablet-1100x800/03-setup.png`
- `tablet-1100x800/04-dashboard.png`
- `tablet-1100x800/05-tasks-list.png`
- `tablet-1100x800/06-tasks-detail.png`
- `tablet-1100x800/07-projects-list.png`
- `tablet-1100x800/08-projects-detail-overview.png`
- `tablet-1100x800/09-projects-tab-tasks.png`
- `tablet-1100x800/10-projects-tab-risks.png`
- `tablet-1100x800/11-risks-list.png`
- `tablet-1100x800/12-risks-detail.png`
- `tablet-1100x800/13-team.png`
- `tablet-1100x800/14-team-member.png`
- `tablet-1100x800/15-team-member-notes.png`
- `tablet-1100x800/16-team-member-work-items.png`
- `tablet-1100x800/17-team-member-risks.png`
- `tablet-1100x800/18-team-member-growth.png`
- `tablet-1100x800/19-settings.png`
- `tablet-1100x800/20-settings-azure-import.png`
- `tablet-1100x800/21-shell-search-open.png`
- `tablet-1100x800/22-shell-quick-add.png`
- `narrow-700x900/01-login-index.png`
- `narrow-700x900/02-login.png`
- `narrow-700x900/03-setup.png`
- `narrow-700x900/04-dashboard.png`
- `narrow-700x900/05-tasks-list.png`
- `narrow-700x900/06-tasks-detail.png`
- `narrow-700x900/07-projects-list.png`
- `narrow-700x900/08-projects-detail-overview.png`
- `narrow-700x900/09-projects-tab-tasks.png`
- `narrow-700x900/10-projects-tab-risks.png`
- `narrow-700x900/11-risks-list.png`
- `narrow-700x900/12-risks-detail.png`
- `narrow-700x900/13-team.png`
- `narrow-700x900/14-team-member.png`
- `narrow-700x900/15-team-member-notes.png`
- `narrow-700x900/16-team-member-work-items.png`
- `narrow-700x900/17-team-member-risks.png`
- `narrow-700x900/18-team-member-growth.png`
- `narrow-700x900/19-settings.png`
- `narrow-700x900/20-settings-azure-import.png`
- `narrow-700x900/21-shell-search-open.png`
- `narrow-700x900/22-shell-quick-add.png`
