# Atlas UI: React → Blazor WebAssembly migration plan

## Goal

Replace `src/atlas.ui` (React/Vite) with a Blazor WebAssembly frontend that talks to the existing ASP.NET Core API, preserving feature parity for core Atlas flows (tasks, projects, risks, team, settings, AI assistant) and the **Docker/nginx** hosting model.

Remote agents must keep UI look and behavior as close to identical as practical. Visual parity is **manually approved** against route/state checklists and before/after screenshots — **not** gated by automated pixel diff.

---

## Decisions (locked)

| Topic | Decision |
| --- | --- |
| **Merge to `main`** | Only once React is fully removed and Blazor is the sole frontend. |
| **Parity baseline** | **React development is frozen** at umbrella-branch creation (`cursor/blazor-wasm-frontend-82c4`). No ongoing React sync. |
| **Parity acceptance** | Manual review + screenshots. **No pixel-diff gate.** |
| **Visual baseline timing** | Complete React screenshot baseline **no later than end of Phase 3**, before Phase 4. |
| **API types** | OpenAPI codegen via **NSwag** from FastEndpoints Swagger (Phase 3). |
| **Routing** | **Path-based** Blazor routing (`/dashboard`, not `/#/dashboard`). |
| **Legacy hash URLs** | Temporary **client-side** shim `/#/…` → `/…` ([Legacy hash URL shim](#legacy-hash-url-shim)). |
| **Hosting** | **Docker/nginx only.** No Azure Static Web Apps. |
| **React removal** | **Hard delete** `src/atlas.ui` in a **standalone commit**. |
| **Phase sequencing** | **Team (Phase 6) before AI (Phase 7).** |
| **Tests** | Playwright ported **progressively each phase** — not deferred to cutover. |
| **Umbrella CI** | **Mandatory in Phase 2.** Every later phase PR runs Blazor build + available Playwright flows. Phase 8 **extends/finalizes** CI — does not introduce it. |
| **Dev/test host port** | Blazor dev and Playwright host pinned to **5173** (match existing CORS and Compose `UI_PORT`). |
| **.NET SDK** | Blazor project and CI use **.NET SDK 10.0.x** (match `backend-tests.yml` / repo today). |
| **Playwright home** | Phases 2–7: config + deps in **`src/atlas.ui`**; cumulative manifest in **`tests/e2e/`**. Phase 8: relocate all e2e assets to **`tests/e2e/`** before React delete. |

### Defaults preserved

| Topic | Default |
| --- | --- |
| Styling | Reuse global CSS/classes (`index.css`, `App.css`); **no component library** |
| Auth | **Out of scope** — login/setup stubs |
| Playwright | **All 14 flows** ported to path URLs |
| Settings | Preserve `localStorage` key `atlas.defaultAiPanelOpen` |
| Markdown | **Sanitized Markdig** + syntax highlighting — intentional security improvement vs React `react-markdown` (rendering differences require **manual acceptance**, not exact React parity) |
| AI streaming | **EventSource via JS interop** — outside NSwag |

### Why path-based routing

Blazor defaults to path routing. nginx already has SPA `try_files` (`docker/nginx/default.conf`). The hash shim is a **temporary** client-side bridge only; fragments are invisible to nginx.

---

## Branch strategy

| Branch | Role |
| --- | --- |
| `cursor/blazor-wasm-frontend-82c4` | **Umbrella.** All Blazor work merges here until Phase 8 cutover to `main`. |
| `cursor/blazor-wasm-<phase>-82c4` | Phase branches; **PRs target umbrella**. |
| `main` | React-only until Phase 8 merge. |

Optional Blazor compose profile on umbrella only — must not ship to `main` early.

---

## Current baseline (repo facts)

- ~14k LOC under `src/atlas.ui/src`, **roughly two dozen** route patterns in `src/atlas.ui/src/app/router.tsx`, ~9 shared components, Playwright e2e only.
- React Router **hash** routing (`createHashRouter`); TanStack Query; native `fetch`; global CSS.
- Hotspots: `TeamView.tsx`, `useAppCache.ts` / `cacheUpdates.ts`, AI SSE, markdown.
- Backend: FastEndpoints **8.1.0** + `FastEndpoints.Swagger`. `SwaggerDocument()` + `UseSwaggerGen()` in Development only. Compose Production → no live `/swagger`.
- **OpenAPI export not wired.** No `ExportSwaggerDocsAndExitAsync` in `Program.cs`. No committed OpenAPI JSON, no NSwag config.
- **Startup requires Postgres today:** `Program.cs` runs `EnsureCreated()` / `Migrate()` and optional demo seed **before** `UseFastEndpoints()` — export wiring inherits this unless Phase 3 adds an early export path.
- Compose: `Dockerfile.ui` (React/Vite → nginx:80); host `UI_PORT` default **5173**. API **5012**. CORS defaults `http://localhost:5173`, `http://127.0.0.1:5173`.
- nginx: SPA fallback; **no `Cache-Control` headers** today.
- CI today: `frontend-ci.yml` on **`main` only** — React lint/build + Playwright.

---

## Non-goals

- API/domain rewrite; SSO/auth; frontend unit-test gates; SignalR AI rewrite; pixel-diff tooling; Azure Static Web Apps; long-term hash URLs.

---

## Success criteria

- [ ] Same user journeys as frozen React baseline: login/setup stub, dashboard, tasks, projects, risks, team, settings, Azure import, AI panel.
- [ ] Path-based URLs; Docker/nginx serves Blazor; umbrella CI green from Phase 2 onward.
- [ ] React visual baseline captured by **end of Phase 3**; Phase 4–7 PRs compare Blazor against it.
- [ ] Manual parity sign-off per phase; Playwright in `tests/e2e/`; `src/atlas.ui` deleted in standalone commit; umbrella merges to `main`.

---

## Remote-agent operating protocol

### Branch, scope, prerequisites

- Phase PRs → **`cursor/blazor-wasm-frontend-82c4`** only.
- One phase per PR unless plan allows sub-PRs.
- Edit only files required for current phase deliverables.
- **Do not start Phase 7 until Phase 6 is merged.**
- React on `main` stays untouched; on umbrella, change React only for Playwright URL migration tied to Blazor hosting.

| Phase | Prerequisite on umbrella |
| --- | --- |
| 2 | Phase 1 accepted |
| 3 | Phase 2 exit criteria (incl. CI live) |
| 4 | Phase 3 exit criteria (incl. **React visual baseline complete**) |
| 5 | Phase 4: shell + hash shim + first Playwright ports |
| 6 | Phase 5 CRUD parity + related Playwright |
| 7 | **Phase 6 merged** |
| 8 | Phases 5–7 complete; manifest lists `blazor-host` + all 14 ported flows |

### Required validation (every phase PR)

1. **Umbrella CI green** — Blazor build + progressively ported Playwright (see [CI strategy](#ci-strategy)).
2. **OpenAPI drift check** — from Phase 3 onward.
3. **Manual parity** — screenshots vs fixed React baseline (Phase 4+) + checklist (see [Visual parity](#visual--behavioral-parity)).

### Handoff format

Files changed; tests/commands run; screenshot artifact links; known parity deviations; blockers.

### Confirmed commands today

```bash
dotnet run --project src/backend/Api/Atlas.Api/Atlas.Api.csproj --launch-profile http
cd src/atlas.ui && npm ci && npm run test:e2e   # main-branch React baseline only
dotnet restore src/Atlas.sln && dotnet test src/Atlas.sln --no-restore --verbosity normal
docker compose up --build   # Phase 8 cutover validation
```

### Introduced by migration (do not assume until landed)

- FastEndpoints export after `Program.cs` wiring + regenerate script
- OpenAPI drift CI step
- Blazor `webServer.command` on port **5173** in `src/atlas.ui/playwright.config.ts` (Phases 2–7)
- **`tests/e2e/run-ported-playwright.sh`** — manifest runner (Phases 2–7); fails if manifest empty; **never** bare `playwright test`
- `src/atlas.ui/e2e/flows/blazor-host.spec.ts` — Phase 2 smoke spec; first manifest entry
- `dotnet publish` Blazor → nginx `Dockerfile.ui`

---

## Visual & behavioral parity

### Acceptance

- Human-approved against checklist + screenshots. No pixel-diff tooling.
- Deviations listed in PR handoff.

### React baseline capture (**mandatory by end of Phase 3**)

Capture **before Phase 4 starts**. React remains in repo until Phase 8 standalone delete — baseline is taken from React, not Blazor.

| Viewport | Size |
| --- | --- |
| Desktop | **1440×900** |
| Tablet | **1100×800** |
| Narrow | **700×900** |

**Storage:** committed path (e.g. `docs/migration-screenshots/react-baseline/`) **or** CI artifact uploaded from umbrella workflow — must be **remotely accessible** to phase agents without local React runs.

**Capture environment:** API with demo seed (`ATLAS_SEED_DEMO=true`), same data CI Playwright uses; Playwright Chromium; hash URLs (`/#/…`) as React serves today.

### Checklist routes/states

| Area | Route / state |
| --- | --- |
| Login | `/` (index login) and `/login` — form visible |
| Setup | `/setup` — Azure setup stub |
| Dashboard | `/dashboard` |
| Tasks | `/tasks`; `/tasks/{id}` split; `/tasks/{id}` focus |
| Projects | `/projects`; `/projects/{id}?tab=overview\|tasks\|risks` |
| Risks | `/risks`; `/risks/{id}` focus |
| Team | `/team`; member tabs; note/work-item/risk/growth detail routes |
| Settings | `/settings`; `/settings/azure-import` |
| Shell | Global search open; Quick Add modal |
| System | Hydration overlay; empty list; validation/server error |
| AI (Phase 7+) | Panel closed/open; streaming transcript |

### Phase 4–7 PR evidence

- Blazor screenshots for routes touched in the phase, same viewports.
- Compare against **fixed Phase 3 React baseline** — not re-captured React.
- Mark each row: **match**, **minor deviation** (describe), or **N/A**.

Phase 8 **verifies baseline completeness** only — must not create the baseline for the first time.

---

## Parity inventory (hidden behavior)

| Area | React source | Required Blazor behavior |
| --- | --- | --- |
| **SelectionState** | `state/SelectionState.tsx` | In-memory task/risk/team/project IDs; not persisted |
| **Hydration gates** | `queries/hooks.ts` | See [Cache / state design](#cache--state-design); `IsHydrating` + redirect guards |
| **Tasks/Risks focus** | `TasksView.tsx`, `RisksView.tsx` | Focus mode; exit returns to list |
| **Projects `?tab=`** | `ProjectsView.tsx` | Preserve query on focus enter/exit; edit pins `tab=overview` |
| **Team paths** | `TeamView.tsx` | Tab paths + detail child routes |
| **Team redirect** | `TeamNoteDetailView.tsx` | Invalid `memberId` → `/team` after hydration |
| **GlobalSearch** | `GlobalSearch.tsx` | Max 12; keyboard nav; `aria-label="Search"`; disabled while hydrating |
| **Modal / Quick Add** | `Modal.tsx`, `QuickAddModal.tsx` | Escape; focus; native `alert` validation |
| **Native dialogs** | Tasks/Risks/Projects views | `confirm` delete; `alert` errors; `prompt` new project name |
| **A11y / responsive** | Shell, `App.css` | Nav/search/AI labels; breakpoints **700** and **1100** |
| **AI panel** | `AiState.tsx`, `AiPanel.tsx` | Resize, overlay, SSE, scroll-stick, selection context |
| **localStorage** | `localSettings.ts` | `atlas.defaultAiPanelOpen` only |
| **404** | `NotFoundView` | Unknown paths |

---

## Cache / state design

Port intent from `useAppCache.ts`, `cacheUpdates.ts`, `invalidateAppQueries.ts` via scoped WASM services.

### Hydration topology (match React query dependencies)

React starts these **in parallel** on app load:

- **settings**, **projects**, **productOwners**, **team**

**Dependent loads:**

- **risks** — starts only after **projects** succeed (`enabled: projectsQuery.isSuccess`); needs project names for mapping.
- **tasks** — starts only after **projects and risks** succeed; needs both for mapping.

```text
Independent parallel roots (start together):
  settings
  projects ──► risks ──► tasks
  productOwners
  team                         (no dependency edge into tasks)

Only projects → risks → tasks form a chain. Team is a separate root.
```

**Blazor implementation:**

1. Mirror dependency gates — do not fetch risks until projects loaded; do not fetch tasks until projects **and** risks loaded.
2. **`IsHydrating`** = any of settings, projects, productOwners, team, risks, or tasks initial load still in flight (risks/tasks count as hydrating while waiting on prerequisites **or** fetching).
3. Shell spinner, `LoadingOverlay`, disabled search, deferred AI default-open, and redirect guards (invalid team member) all wait for `IsHydrating == false`.
4. Preserve parallel starts for the four independent roots — do not serialize settings before projects unless a real dependency exists.

### Mutations

- **Refetch-after-mutation** preferred over optimistic updates until parity proven.
- Port link/unlink/rename repairs from `cacheUpdates.ts`.
- Azure import → broad invalidate (match `invalidateAppQueries.ts`).

---

## OpenAPI / NSwag

### Current state

- `Program.cs`: `AddFastEndpoints()`, `SwaggerDocument()` (bare call — **document name not customized**), `UseSwaggerGen()` in Development only.
- **Not implemented:** export call, `--export-swagger-docs` handling, committed artifact, NSwag config.

### FastEndpoints 8.1.0 export (verified API shape)

Atlas uses **`FastEndpoints.Swagger`**, not `FastEndpoints.OpenApi`. Phase 3 wires:

```csharp
app.UseFastEndpoints();

await app.ExportSwaggerDocsAndExitAsync(/* document name(s) — see below */);

app.Run();
```

**Critical details:**

| Item | Requirement |
| --- | --- |
| **Method args** | Pass configured **document name string(s)** — e.g. `"v1"` — **not** `args`. Names must match `SwaggerDocument()` registration. |
| **Placement** | Immediately **after** `app.UseFastEndpoints()`, before `app.Run()`. |
| **Activation** | `dotnet run --export-swagger-docs true` sets configuration that triggers export-then-exit (FastEndpoints reads export flag from configuration). |
| **Document name** | Confirm from `.SwaggerDocument(...)` — today bare `SwaggerDocument()` uses FastEndpoints default; inspect export output filename or add explicit `DocumentSettings` if ambiguous. |
| **Default export path** | FastEndpoints 8.1 default: **`wwwroot/openapi`** under the API project — **verify filename on first export** (document name may suffix the file). |
| **Committed artifact** | **`openapi/atlas.v1.json`** — set `<SwaggerExportPath>` in `Atlas.Api.csproj` **or** copy/rename in the introduced regenerate script. |

**Do not document a final script/command until Phase 3 adds and tests it.** Phase 3 acceptance criteria define success.

### Database prerequisite

**Today:** export wiring runs after DB `EnsureCreated()` / `Migrate()` in `Program.cs` — **Postgres required** for `dotnet run --export-swagger-docs true` unless changed.

**Phase 3 must either:**

- Document and CI-test export with Postgres (same as Playwright job), **or**
- Implement an **early export path** that skips DB initialization when export configuration is active — and test on fresh clone.

### Phase 3 deliverables & acceptance

| Deliverable | Acceptance |
| --- | --- |
| Export wired per API shape above | Export produces JSON; process exits 0 with `--export-swagger-docs true` |
| Committed `openapi/atlas.v1.json` | Matches export output after script normalization |
| Regenerate script | Deterministic; documented in PR; runs on fresh clone |
| NSwag client + mapping layer | Stubs for `mappers.ts`, `duration.ts`, `tones.ts`, `team.ts` |
| Cache skeleton | `IsHydrating` with correct dependency gates |
| CI drift check | Fails PR if committed artifact drifts without intentional update |
| Sample Blazor HTTP call | e.g. list tasks via generated client |

**SSE exception:** `GET /ai/sessions/{id}/events` — hand-written EventSource interop; not NSwag.

---

## Playwright path migration

React uses hash URLs. Blazor uses paths. Update **every** hash reference when porting each flow.

### Files with hash URLs today (search all when porting)

| File | Patterns to update |
| --- | --- |
| `e2e/fixtures/app.ts` | `page.goto('/#/')` → path entry (e.g. `/` or `/login`) |
| `e2e/flows/login-dashboard.spec.ts` | `toHaveURL(/#\/dashboard/)` |
| `e2e/flows/focus-url.spec.ts` | `toHaveURL` with `#/tasks/...` (3 assertions) |
| `e2e/flows/quick-add.spec.ts` | `toHaveURL` for tasks, risks, team notes |
| `e2e/flows/search.spec.ts` | `toHaveURL` for tasks, risks, team |
| `e2e/flows/dashboard-nav.spec.ts` | `toHaveURL` for risks detail |
| `e2e/flows/shell-nav.spec.ts` | `goto('/#/this-route-does-not-exist')`, dashboard URL assert |
| `e2e/flows/team-note-ado-pr.spec.ts` | `toHaveURL` for note detail; `page.goto(noteUrl)` after reload |

Also grep `e2e/` for `#/`, `goto(`, and `toHaveURL` when porting — no hash assertions may remain on Blazor target.

**Phase 4 explicit deliverable:** convert `e2e/fixtures/app.ts` (`page.goto('/#/')` and any hash helpers) **before** flow specs — downstream specs import this fixture.

### Phase 2: Blazor test host on 5173

The Blazor project **must** expose a confirmed command serving HTTP on **127.0.0.1:5173**, e.g.:

- `Properties/launchSettings.json` profile with `applicationUrl` including port **5173**, run via `dotnet run --project <wasm-csproj> --launch-profile <profile>`, **or**
- `dotnet run` with `--urls http://127.0.0.1:5173`, **or**
- Serve `dotnet publish` output via a static server on 5173.

**Phase 2 deliverable:** pin the Blazor host command in the WASM project README and set `src/atlas.ui/playwright.config.ts` `webServer.command` to that command (replacing React `npm run preview` / `npm run dev`). Keep `PLAYWRIGHT_BASE_URL=http://localhost:5173` and API CORS on 5173 — **do not change API port**. Config and `package.json` Playwright deps **stay in `src/atlas.ui` until Phase 8 relocation**.

### Progressive spec selection (Phases 2–7)

Umbrella CI runs **only manifest-listed specs**. **Bare `playwright test` (no explicit file list) is prohibited before Phase 8** — an empty or omitted list would silently run all legacy React-targeted specs.

| Item | Detail |
| --- | --- |
| **Manifest file** | `tests/e2e/playwright-ported.txt` — one spec path per line, **relative to `src/atlas.ui`** (e.g. `e2e/flows/blazor-host.spec.ts`, `e2e/flows/login-dashboard.spec.ts`) |
| **Manifest rule** | **Never empty.** Minimum one entry from Phase 2 onward. |
| **Phase 2 seed spec** | `src/atlas.ui/e2e/flows/blazor-host.spec.ts` — verifies Blazor host loads / bootstrap shell on 5173; **first manifest line**; stays in the cumulative suite through Phase 7 (remove or replace only if equivalent coverage exists elsewhere). |
| **Runner script** | Introduce **`tests/e2e/run-ported-playwright.sh`** (Phase 2): invoke as **`bash tests/e2e/run-ported-playwright.sh`** from **repo root** (do not assume executable bit or shebang); reads manifest; **exits non-zero if missing, empty, or whitespace-only**; `cd src/atlas.ui` and runs `npx playwright test` with explicit paths from the manifest — **never** bare `playwright test`. |
| **Growth rule** | Each porting phase **appends** new paths; earlier entries (including `blazor-host.spec.ts`) stay unless deliberately replaced. |
| **Phase 8** | Relocate smoke spec with all other e2e assets; relocated **`tests/e2e/package.json` must define `"test:e2e"`** (e.g. `"playwright test"`); delete manifest + runner filter; **`npm ci` in `tests/e2e/`**; bare **`npm run test:e2e`** / full suite allowed from `tests/e2e/` only. |

### Phases 2–7 CI setup (exact)

Playwright config and Node dependencies remain under **`src/atlas.ui`** until Phase 8:

```yaml
# Umbrella Playwright job (Phases 2–7) — illustrative
- name: Install Playwright deps
  working-directory: src/atlas.ui
  run: npm ci

- name: Install Playwright browsers
  working-directory: src/atlas.ui
  run: npx playwright install --with-deps chromium

- name: Run ported Playwright specs
  run: bash tests/e2e/run-ported-playwright.sh   # repo root; NOT bare playwright test
```

- **`npm ci`** in `src/atlas.ui` every Playwright job (temporary Playwright home Phases 2–7).
- Playwright executes with **`working-directory: src/atlas.ui`** (via runner script `cd`).
- Manifest resolved from **`tests/e2e/playwright-ported.txt`** (runner reads `../../tests/e2e/...` from `src/atlas.ui` or absolute from repo root).
- `playwright.config.ts` stays in `src/atlas.ui`; **`webServer.command`** points at Blazor on **5173**.

---

## CI strategy

### Today (`main` only)

- `frontend-ci.yml`: React lint/build + Playwright.
- `backend-tests.yml`: `dotnet test src/Atlas.sln`.

### Umbrella CI — **mandatory Phase 2 deliverable**

Add or extend workflow(s) triggered on **pull requests targeting `cursor/blazor-wasm-frontend-82c4`** (and pushes to umbrella):

| Job | From | Detail |
| --- | --- | --- |
| Blazor build | Phase 2 | `dotnet build` WASM project / solution; **SDK 10.0.x** (`setup-dotnet@v4`) |
| Playwright | Phase 2 | API + Postgres + demo seed; **`npm ci`** in `src/atlas.ui`; **`bash tests/e2e/run-ported-playwright.sh`** from repo root (manifest paths relative to `src/atlas.ui`); Blazor `webServer` on 5173 — must pass |
| OpenAPI drift | Phase 3 | Diff export vs `openapi/atlas.v1.json` |
| Blazor publish | Phase 8 | Add `dotnet publish` Release |
| React npm | Phase 8 | Remove React lint/build from frontend CI on `main` |

**No "document only" escape hatch** — workflow files merged in Phase 2; later phases extend job steps.

### Playwright port schedule (all 14 flows)

| Phase | Flows |
| --- | --- |
| 4 | `login-dashboard`, `shell-nav`, `dashboard-nav` |
| 5 | `tasks-crud`, `risks-crud`, `projects-crud`, `quick-add`, `search`, `focus-url`, `delete-smoke`, `settings-smoke`, `persist-reload` |
| 6 | `team-note-ado-pr` |
| 7 | `ai-panel` |
| 8 | Full suite + compose smoke + deep-link refresh |

---

## Build / delivery (Docker / nginx only)

**Phase 8** replaces `Dockerfile.ui` with Blazor `dotnet publish` → `nginx:1.27-alpine`; keep `UI_PORT:-5173` → container 80.

| Topic | Detail |
| --- | --- |
| API base URL | Dev: `wwwroot/appsettings.Development.json` → `http://localhost:5012`; Compose: replace `VITE_API_BASE_URL` |
| CORS | Keep **5173** origins |
| nginx cache (add Phase 8) | Long cache fingerprinted `/_framework/*`; short/no cache `index.html`, `blazor.boot.json`; gzip; brotli only if publish emits `.br` |
| Deep-link smoke | Refresh `/tasks/{id}`, `/team/{id}/notes`, `/projects/{id}?tab=tasks` |
| Docs | Phase 8: `docs/docker.md`, Blazor README, `AGENTS.md` — path URLs |
| Rollback | Record last React image/tag/SHA; standalone delete commit; `git revert` restore |

---

## Legacy hash URL shim

Fragments (`/#/dashboard`) are **never sent to nginx** — only client code can read them.

**Phase 4:** early startup detects `#/…`, rewrites to path via `history.replaceState` / `NavigationManager`.

**Removal** (all required): path URLs in docs/Playwright under `tests/e2e/`; no `/#/` in repo docs; grace period sign-off; dedicated removal PR.

---

## Performance

Record React baseline (transfer size, time-to-interactive at `/dashboard`) during Phase 3 baseline work. Phase 8 compares Blazor cold load — **document variance**, no invented numeric budget. Evaluate trimming/lazy load if regressed.

---

## Phases

### Phase 1 — Umbrella + plan

- [x] Branch `cursor/blazor-wasm-frontend-82c4`; this plan committed.
- **Exit:** Plan accepted; React frozen.

### Phase 2 — Scaffold Blazor WASM + **umbrella CI**

- [x] `src/frontend/Atlas.Ui` in `src/Atlas.sln`; placeholder page; CSS stub; **target framework / SDK 10.0.x**
- [x] `ApiBaseUrl` config; **launchSettings / run command pinned to port 5173**
- [x] **`src/atlas.ui/e2e/flows/blazor-host.spec.ts`** — smoke spec verifying Blazor host on 5173
- [x] **`tests/e2e/playwright-ported.txt`** with first line `e2e/flows/blazor-host.spec.ts` (never empty)
- [x] **`tests/e2e/run-ported-playwright.sh`** — fails on empty manifest; no bare `playwright test`
- [x] **`playwright.config.ts`** in `src/atlas.ui`: `webServer.command` → Blazor host on 5173
- [x] **Umbrella CI workflow live:** `dotnet` **10.0.x** + Blazor build + Playwright job (`npm ci` in `src/atlas.ui`; `bash tests/e2e/run-ported-playwright.sh` from repo root)
- [ ] Optional umbrella-only Compose profile `blazor` *(skipped in Phase 2 PR)*

**Exit:**

- [x] `dotnet build` succeeds; WASM loads on **5173**; API + CORS ok
- [ ] **Umbrella CI green on Phase 2 PR** — `blazor-host.spec.ts` passes via runner script
- [x] Playwright config points at Blazor host (not React preview)

### Phase 3 — OpenAPI + cache skeleton + **React visual baseline**

- [ ] Export wired (`ExportSwaggerDocsAndExitAsync` after `UseFastEndpoints`; document name confirmed)
- [ ] `openapi/atlas.v1.json` + regenerate script + NSwag client + mapping stubs
- [ ] Cache skeleton with hydration dependency gates
- [ ] CI OpenAPI drift check added to umbrella workflow
- [ ] **Complete React visual baseline** (all checklist routes/states, 3 viewports, stored in repo or CI artifact with capture env documented)

**Exit:**

- [ ] Export + regenerate tested on fresh clone (Postgres or early-export path documented)
- [ ] Sample Blazor API call works; drift check green
- [ ] **React baseline artifact complete and linked in umbrella README or workflow** — **blocks Phase 4**

### Phase 4 — Shell, path routes, CSS, hash shim

- [ ] Routes from `src/atlas.ui/src/app/router.tsx` → `@page` (roughly two dozen patterns)
- [ ] Shell, nav, search/quick-add stubs; login + `/setup`; dark theme
- [ ] Hash shim
- [ ] **Path conversion:** `e2e/fixtures/app.ts` first, then `login-dashboard`, `shell-nav`, `dashboard-nav` (all hash refs updated)
- [ ] Append ported specs to `tests/e2e/playwright-ported.txt` (paths like `e2e/flows/login-dashboard.spec.ts`)
- [ ] Blazor screenshots vs **Phase 3 React baseline**

**Exit:** Path stubs + CSS recognizable; shim works; 3 Playwright flows green; parity evidence for shell routes.

### Phase 5 — Core CRUD

- [ ] **UI rollout order** (not hydration order): tasks → projects/risks → dashboard → settings/Azure — implement surfaces in this sequence for reviewability; cache hydration gates remain projects → risks → tasks regardless
- [ ] Modals; cache invalidation; native dialogs
- [ ] Playwright Phase 5 flows ported; append paths to `tests/e2e/playwright-ported.txt`
- [ ] Screenshots vs baseline for CRUD/modal/empty/error states

**Exit:** CRUD smoke parity; Phase 5 Playwright green.

### Phase 6 — Team hub *(blocks Phase 7)*

- [ ] Decomposed team pages; `team.ts` logic; `team-note-ado-pr` Playwright ported; append to manifest
- [ ] Team screenshots vs baseline

**Exit:** Full `/team/**` parity; **merged before Phase 7 starts**.

### Phase 7 — AI assistant

**Prerequisite:** Phase 6 merged.

- [ ] AI panel + EventSource interop; **sanitized Markdig** (document rendering diffs vs React in handoff)
- [ ] `ai-panel` Playwright ported; append to manifest

**Exit:** Streaming parity; markdown security accepted manually; `ai-panel` green; manifest lists `blazor-host.spec.ts` + all 14 ported flows.

### Phase 8 — Cutover to `main`

**Extends/finalizes CI and Docker — does not introduce umbrella CI.**

**Order is strict — do not delete `src/atlas.ui` until step 5 passes:**

1. [ ] Verify React baseline artifact complete (do not create anew)
2. [ ] **Relocate Playwright** from `src/atlas.ui` to **`tests/e2e/`**:
   - Move `e2e/` (flows, fixtures, **`blazor-host.spec.ts`**, and all ported specs), `playwright.config.ts`, and a **minimal** `package.json` / lockfile with only Playwright scripts/deps — **`package.json` must include `"test:e2e": "playwright test"`** (or equivalent full-suite script)
   - Fix `testDir`, import paths, and `webServer.command` (Blazor on 5173) in relocated config
   - Delete `tests/e2e/playwright-ported.txt` and **`tests/e2e/run-ported-playwright.sh`** — Phase 8 runs full suite via **`npm run test:e2e`** from `tests/e2e/` (allowed only after relocation)
3. [ ] **Update CI:** `npm ci` with **`working-directory: tests/e2e`**; `cache-dependency-path: tests/e2e/package-lock.json`; SDK **10.0.x**; add Blazor publish job; remove `src/atlas.ui` Playwright steps
4. [ ] Blazor `Dockerfile.ui`; compose env; nginx cache headers; docs path URL updates; performance comparison doc; rollback SHA recorded
5. [ ] **Full validation** (all must pass before React delete):
   - **`npm run test:e2e`** from `tests/e2e/` with **`working-directory: tests/e2e`** — requires relocated `package.json` **`test:e2e`** script; full suite green (including relocated `blazor-host.spec.ts`)
   - `docker compose up --build` smoke
   - Deep-link refresh cases
6. [ ] **`frontend-ci.yml` on `main`:** Blazor publish + full Playwright from `tests/e2e/`; drop React npm
7. [ ] **Standalone commit: delete `src/atlas.ui`** — Playwright/config must already live under `tests/e2e/`; nothing required for e2e may remain only in React tree
8. [ ] Merge umbrella → `main`; schedule hash shim removal

**Exit:** `main` Blazor-only via Docker/nginx; Playwright in `tests/e2e/`; full suite green; CI green; manual full checklist sign-off.

---

## Phase dependency graph

```text
Phase 1  plan + freeze React
    │
Phase 2  scaffold + umbrella CI (mandatory)
    │
Phase 3  OpenAPI + cache + React visual baseline ── blocks Phase 4
    │
Phase 4  shell + paths + hash shim + Playwright (3)
    │
Phase 5  CRUD + Playwright (9)
    │
Phase 6  Team + Playwright          ◄── MUST merge before Phase 7
    │
Phase 7  AI + Playwright
    │
Phase 8  relocate Playwright → tests/e2e/ → CI → full validation → delete React → main
         └── hash shim removal (follow-up)
```

---

## Immediate next step

Phase 2 scaffold is on `cursor/blazor-wasm-scaffold-82c4`. After merge to the umbrella: cut `cursor/blazor-wasm-openapi-82c4` (or equivalent) and execute **Phase 3**.
