# Atlas UI: React → Blazor WebAssembly migration plan

## Goal

Replace `src/atlas.ui` (React/Vite) with a Blazor WebAssembly frontend that talks to the existing ASP.NET Core API, preserving feature parity for core Atlas flows (tasks, projects, risks, team, settings, AI assistant) and the Docker/nginx hosting model.

## Branch strategy

| Branch | Role |
| --- | --- |
| `cursor/blazor-wasm-frontend-82c4` | **Umbrella feature branch.** All Blazor work merges here first, not directly to `main`. |
| `cursor/blazor-wasm-<phase>-82c4` | Short-lived phase branches cut from the umbrella (e.g. `cursor/blazor-wasm-scaffold-82c4`). PRs target the umbrella branch. |
| `main` | Receives the umbrella only when cutover is ready (or via stacked merges if we land early scaffolding behind a flag / parallel path). |

**Phase 1 (this change):** create `cursor/blazor-wasm-frontend-82c4` and land this plan. Later phases stem off this branch.

Until cutover, keep React as the production UI path unless a phase explicitly dual-hosts Blazor for comparison.

## Current baseline (why this plan looks like this)

- Small–medium SPA: ~14k LOC under `src/atlas.ui/src`, ~25 routes, ~9 shared components, Playwright e2e only.
- Thin stack: React Router hash routing, TanStack Query, native `fetch`, global CSS — no component library, charts, DnD, or real auth.
- Hotspots: `TeamView.tsx` (~2.2k LOC), React Query cache coherence, AI SSE (`EventSource`), markdown rendering.
- Backend already exposes FastEndpoints + Swagger; UI currently hand-maintains TS DTOs/mappers.
- Compose: `ui` service builds `Dockerfile.ui` → nginx on port 80 (`UI_PORT` → 5173); API on 5012.

## Non-goals (for this migration)

- Rewriting the API or domain model.
- Adding SSO/auth (still stub; design Blazor-friendly later).
- Frontend unit-test / coverage gates (match current Atlas conventions).
- SignalR rewrite of AI streaming unless SSE proves painful in WASM (prefer keep `/ai/sessions/{id}/events` first).
- Pixel-perfect redesign; port existing CSS variables / `App.css` structure where practical.

## Success criteria

- Blazor WASM app serves the same user journeys as React for: login/setup stub, dashboard, tasks, projects, risks, team (notes / work items / growth), settings, Azure import, AI panel.
- Hash URLs remain compatible where practical (`/#/dashboard`, etc.) so bookmarks and docs keep working.
- `docker compose up --build` builds and serves the Blazor UI (nginx or equivalent static host).
- Frontend CI builds Blazor and runs an updated Playwright suite against it.
- React UI removed or archived only after parity + CI green on the umbrella branch.

---

## Phase 1 — Umbrella branch + plan *(this PR)*

**Outcome:** Long-lived branch `cursor/blazor-wasm-frontend-82c4` exists; this document is the source of truth.

**Work:**

- Create branch from `main`.
- Add `docs/blazor-wasm-migration.md`.
- No application code changes.

**Exit:** Branch pushed; plan reviewed/accepted before scaffolding.

---

## Phase 2 — Scaffold Blazor WASM (parallel to React)

**Branch from:** `cursor/blazor-wasm-frontend-82c4` → e.g. `cursor/blazor-wasm-scaffold-82c4`

**Outcome:** Empty-but-runnable Blazor WASM project in the solution; React still default.

**Work:**

- Add project under e.g. `src/frontend/Atlas.Ui` (Blazor WASM, .NET aligned with API SDK).
- Add to `src/Atlas.sln`.
- Minimal shell: layout, one placeholder page, `wwwroot` CSS variables stub.
- Configuration: `ApiBaseUrl` via `appsettings.json` / `wwwroot/appsettings.json` (replaces `VITE_API_BASE_URL`).
- Local run story documented (alongside existing `dotnet run` API + optional React).
- Optional: second Docker target or compose profile `blazor` that builds WASM → nginx **without** removing `Dockerfile.ui` yet.

**Exit:** `dotnet build` succeeds; WASM loads in browser against local API origin (CORS already allows UI origins — update list if Blazor uses a new port).

**Risks:** WASM download size vs Vite SPA; document expected first-load behavior early.

---

## Phase 3 — API client & shared contracts

**Branch from:** umbrella (after Phase 2 merge)

**Outcome:** Typed HTTP access to Atlas API without duplicating TS mappers.

**Work:**

- Prefer OpenAPI/Kiota/NSwag generation from FastEndpoints Swagger **or** a thin shared contracts assembly referenced by WASM (whichever fits existing layering without leaking Persistence).
- Blazor `HttpClient` registration with base address from config.
- Port problem-details / `HttpError` handling behavior from `src/atlas.ui/src/app/api/client.ts`.
- Enum-as-string JSON parity with API `JsonStringEnumConverter`.
- Replace React Query mental model with an explicit **app state / cache service** design note (scoped WASM services + invalidate/refetch helpers for linked entities: tasks ↔ projects ↔ risks). Implement the skeleton only; feature phases fill it in.

**Exit:** Sample call (e.g. list tasks or health/settings) works from a Blazor page; codegen or shared types checked into repo with a documented refresh command.

---

## Phase 4 — Shell, routing, styling port

**Branch from:** umbrella

**Outcome:** App chrome and route map match React shell; pages are stubs.

**Work:**

- Port `ShellLayout`, nav, global search/quick-add **shell slots** (stubs OK).
- Hash routing parity with `src/atlas.ui/src/app/router.tsx` (~25 paths), including nested `/team/**` and detail routes that reuse list+detail views.
- Port `index.css` / `App.css` class structure into Blazor `wwwroot` (keep class names to reduce churn).
- Login / setup stub pages equivalent to current “auth later” behavior.
- Theme hook (dark default; light still unavailable — match Settings behavior).

**Exit:** Navigating hash routes renders stubs inside the shell; CSS looks recognizably Atlas.

---

## Phase 5 — Core CRUD feature parity

**Branch from:** umbrella (may split into sub-PRs: tasks, projects, risks, dashboard, settings)

**Outcome:** Primary CRUD surfaces work without Team hub or AI.

**Suggested order:**

1. **Tasks** (list + detail routes) — template for forms, modals, cache invalidation.
2. **Projects** and **Risks** — same patterns; linked-entity updates exercise the cache service.
3. **Dashboard** — read-only aggregates/lists.
4. **Settings** + **Azure setup / import** — connection probe and import flows.

**Work per feature:**

- Port view behavior from corresponding `src/atlas.ui/src/views/*`.
- Reuse shared Blazor components (Modal, Spinner, LoadingButton) as needed.
- Keep server validation as source of truth; light client required-field checks only.
- Add/adjust Playwright flows for each area as it lands (or batch at end of phase if dual-hosting is awkward).

**Exit:** Manual smoke on `/#/tasks`, `/#/projects`, `/#/risks`, `/#/dashboard`, `/#/settings` (+ setup/import) matches React for happy paths and basic failure toasts/errors.

---

## Phase 6 — Team hub (highest rewrite surface)

**Branch from:** umbrella

**Outcome:** Team member overview, notes, Azure work items, member risks, growth goals/detail.

**Work:**

- Decompose React `TeamView.tsx` monolith into Blazor components/pages by tab/route (`notes`, `work-items`, `risks`, `growth`, goal detail).
- Port nested routing and deep links.
- Member overview and growth goal detail as separate components (avoid one 2k-line razor file).
- Ensure cache/state updates cover team-linked mutations.

**Exit:** Full `/#/team/**` journey parity; no dependency on React Team view.

**Risks:** Largest regression surface; prefer smaller PR slices per tab if the umbrella review gets unwieldy.

---

## Phase 7 — AI assistant (SSE + markdown)

**Branch from:** umbrella

**Outcome:** Shell-wide AI panel with streaming session events and markdown transcripts.

**Work:**

- Port `AiState` / `AiPanel` behavior: open/close, resize, session lifecycle, transcript assembly.
- Consume `/ai/sessions/{id}/events` via JS interop `EventSource` **or** `HttpClient` streaming — pick one approach and document it; prefer minimal backend change.
- Markdown: Markdig (or equivalent) + GFM-ish subset + syntax highlighting strategy; explicit HTML sanitization.
- `localStorage` (or Blazor protected local storage) for panel default-open setting.

**Exit:** AI panel streams and renders comparable to React; e2e stubs for SSE updated.

**Risks:** Reconnect/terminal event edge cases; XSS if markdown sanitization is incomplete.

---

## Phase 8 — CI, Docker cutover, remove React

**Branch from:** umbrella → merge to `main` when ready

**Outcome:** Blazor is the only frontend; React tree removed or moved to archive.

**Work:**

- Point `Dockerfile.ui` (or replace it) at the Blazor publish output (`dotnet publish` → `wwwroot` / static files) + nginx `try_files` SPA fallback (hash routing still needs `index.html` fallback).
- Update `docker-compose.yml` build args (`ApiBaseUrl` instead of `VITE_API_BASE_URL`).
- Update CORS allowed origins if the UI origin/port changes.
- Rewrite `.github/workflows/frontend-ci.yml`: restore/build WASM; Playwright against Blazor DOM; drop `npm ci` for UI (unless leftover tooling).
- Update `docs/docker.md`, `src/atlas.ui/README.md` → new frontend README, `AGENTS.md` pointers.
- Delete or archive `src/atlas.ui` after CI green on umbrella.
- Final Playwright full suite + compose smoke (`docker compose up --build`, optional `--profile demo`).

**Exit:** `main` serves Blazor only; React gone; docs and CI consistent.

---

## Cross-cutting concerns

### State instead of TanStack Query

Design a small **Atlas UI store** (scoped services in WASM):

- Query-like methods: `GetTasksAsync`, etc., with in-memory lists.
- Explicit `Invalidate*` / patch helpers for linked entities (port intent of `cacheUpdates.ts` / `useAppCache.ts`, not the React API).
- Prefer correctness (refetch after mutation) over clever optimistic graphs until parity is proven.

### Auth (future)

Login remains a stub. When SSO arrives, decide WASM-friendly pattern (tokens in memory vs BFF) and CORS/`AllowCredentials` once — do not bolt cookie auth onto anonymous `fetch` assumptions.

### Testing strategy (Atlas conventions)

| Layer | Expectation |
| --- | --- |
| Handler unit / functional / integration | Unchanged (API). |
| Frontend unit tests | Not required for migration. |
| Playwright | Rewrite selectors/flows against Blazor; keep API/AI fixtures pattern from `src/atlas.ui/e2e`. |
| Manual smoke | Per-phase route checklist in each PR’s **Testing instructions**. |

### Rollback

Until Phase 8 cutover, React remains buildable. After cutover, roll back by reverting the cutover commit(s) on `main` or redeploying the previous image tag.

---

## Phase dependency graph

```text
Phase 1  umbrella + plan
    │
Phase 2  Blazor scaffold (+ optional compose profile)
    │
Phase 3  HttpClient + contracts + cache skeleton
    │
Phase 4  Shell + hash routes + CSS port
    │
    ├─► Phase 5  CRUD features (tasks → projects/risks → dashboard/settings)
    │
    ├─► Phase 6  Team hub          (after shell + cache; can overlap late Phase 5)
    │
    └─► Phase 7  AI panel + SSE    (after shell; independent of Team if needed)
            │
        Phase 8  CI + Docker cutover + remove React
```

Phases 6 and 7 may proceed in parallel after Phase 4–5 foundations land, as long as both merge into the umbrella and resolve shell conflicts deliberately.

---

## Immediate next step

After this Phase 1 PR is accepted: cut `cursor/blazor-wasm-scaffold-82c4` from `cursor/blazor-wasm-frontend-82c4` and execute **Phase 2**.
