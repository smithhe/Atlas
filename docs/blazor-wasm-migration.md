# Atlas UI: React → Blazor WebAssembly migration plan

## Goal

Replace `src/atlas.ui` (React/Vite) with a Blazor WebAssembly frontend that talks to the existing ASP.NET Core API, preserving feature parity for core Atlas flows (tasks, projects, risks, team, settings, AI assistant) and the Docker/nginx hosting model.

## Decisions (locked)

| Topic | Decision |
| --- | --- |
| **Merge to `main`** | Only once React is fully removed and Blazor is the sole frontend. No early scaffolding merge to `main`. |
| **API types** | **OpenAPI codegen** via **NSwag** from FastEndpoints Swagger (see Phase 3). |
| **Routing** | **Path-based** Blazor routing (`/dashboard`, not `/#/dashboard`). Matches Blazor templates and conventions; nginx already has SPA `try_files` fallback. |
| **React removal** | **Hard delete** `src/atlas.ui` in a **standalone commit** (easy `git revert` / checkout restore). |

### Why path-based routing

Blazor’s default and documented model is path routing (`@page "/tasks"`, `NavigationManager`). Hash routing was a React SPA choice for static hosts without rewrite rules; Atlas nginx already serves `index.html` for unknown paths (`docker/nginx/default.conf`), so path routing is the better fit. Docs, Playwright, and bookmarks will move from `/#/...` to `/...` at cutover (no dual-mode hash shim unless we discover a hard requirement later).

## Branch strategy

| Branch | Role |
| --- | --- |
| `cursor/blazor-wasm-frontend-82c4` | **Umbrella feature branch.** All Blazor work merges here first. **Does not merge to `main` until Phase 8 cutover** (React deleted, Blazor-only). |
| `cursor/blazor-wasm-<phase>-82c4` | Short-lived phase branches cut from the umbrella (e.g. `cursor/blazor-wasm-scaffold-82c4`). PRs target the umbrella branch. |
| `main` | Unchanged until the umbrella lands as a single cutover (Blazor in, React gone). |

**Phase 1 (this change):** create `cursor/blazor-wasm-frontend-82c4` and land this plan. Later phases stem off this branch.

Until cutover on the umbrella, React remains the only path used by `main` / default Compose. Optional Blazor compose profile may exist **on the umbrella only** for local comparison — it must not ship to `main` early.

## Current baseline (why this plan looks like this)

- Small–medium SPA: ~14k LOC under `src/atlas.ui/src`, ~25 routes, ~9 shared components, Playwright e2e only.
- Thin stack: React Router **hash** routing, TanStack Query, native `fetch`, global CSS — no component library, charts, DnD, or real auth.
- Hotspots: `TeamView.tsx` (~2.2k LOC), React Query cache coherence, AI SSE (`EventSource`), markdown rendering.
- Backend: FastEndpoints + `FastEndpoints.Swagger` (NSwag-backed). Swagger UI/JSON only when `IsDevelopment()`; Compose runs Production (no live `/swagger`). UI hand-maintains TS DTOs/mappers today.
- Compose: `ui` service builds `Dockerfile.ui` → nginx on port 80 (`UI_PORT` → 5173); API on 5012.

## Non-goals (for this migration)

- Rewriting the API or domain model.
- Adding SSO/auth (still stub; design Blazor-friendly later).
- Frontend unit-test / coverage gates (match current Atlas conventions).
- SignalR rewrite of AI streaming unless SSE proves painful in WASM (prefer keep `/ai/sessions/{id}/events` first).
- Pixel-perfect redesign; port existing CSS variables / `App.css` structure where practical.
- Preserving hash URLs long-term (path routing is intentional).

## Success criteria

- Blazor WASM app serves the same user journeys as React for: login/setup stub, dashboard, tasks, projects, risks, team (notes / work items / growth), settings, Azure import, AI panel.
- Routes use path-based URLs (`/dashboard`, `/tasks`, `/team/...`, etc.).
- `docker compose up --build` builds and serves the Blazor UI (nginx static host).
- Frontend CI builds Blazor and runs an updated Playwright suite against it.
- `src/atlas.ui` hard-deleted in a standalone commit; umbrella then merges to `main`.

---

## Phase 1 — Umbrella branch + plan *(this PR)*

**Outcome:** Long-lived branch `cursor/blazor-wasm-frontend-82c4` exists; this document is the source of truth.

**Work:**

- Create branch from `main`.
- Add `docs/blazor-wasm-migration.md`.
- No application code changes.

**Exit:** Branch pushed; plan reviewed/accepted before scaffolding.

---

## Phase 2 — Scaffold Blazor WASM (umbrella only; React untouched on `main`)

**Branch from:** `cursor/blazor-wasm-frontend-82c4` → e.g. `cursor/blazor-wasm-scaffold-82c4`

**Outcome:** Empty-but-runnable Blazor WASM project in the solution on the umbrella branch; React still present and default for any `main` builds.

**Work:**

- Add project under e.g. `src/frontend/Atlas.Ui` (Blazor WASM, .NET aligned with API SDK).
- Add to `src/Atlas.sln`.
- Minimal shell: layout, one placeholder page, `wwwroot` CSS variables stub.
- Configuration: `ApiBaseUrl` via `appsettings.json` / `wwwroot/appsettings.json` (replaces `VITE_API_BASE_URL`).
- Local run story documented (API `dotnet run` + `dotnet run` on WASM project).
- Optional **on umbrella only**: compose profile `blazor` that builds WASM → nginx **without** changing default `ui` service behavior that `main` relies on.

**Exit:** `dotnet build` succeeds; WASM loads in browser against local API origin (update CORS allowlist if Blazor uses a new port).

**Risks:** WASM download size vs Vite SPA; document expected first-load behavior early.

---

## Phase 3 — OpenAPI (NSwag) client & cache skeleton

**Branch from:** umbrella (after Phase 2 merge)

**Outcome:** Typed HTTP access to Atlas API via generated client; no hand-duplicated TS-style mappers.

### Why NSwag (not Kiota / `dotnet-openapi`)

- API already uses **FastEndpoints.Swagger**, which is **NSwag-backed** — one OpenAPI pipeline, not a second Microsoft `MapOpenApi` stack (`Microsoft.AspNetCore.OpenApi` is referenced but unused today).
- Offline export is supported by FastEndpoints (`--export-swagger-docs` / `ExportSwaggerDocsAndExitAsync`), which matters because **Swagger is Development-only** and Compose/Production has no `/swagger/v1/swagger.json`.
- Generated C# clients fit Blazor WASM `HttpClient` registration cleanly.
- Kiota / `Microsoft.Extensions.ApiDescription.Client` would add a parallel toolchain with little benefit here.

### Work

- Add a documented regenerate path, e.g.:
  1. Export OpenAPI from the API project (Dev export or FastEndpoints export-on-build).
  2. Commit `openapi/atlas.v1.json` (or equivalent) under the frontend or a `contracts` folder.
  3. Run NSwag to generate C# client + DTOs into `Atlas.Ui` (or `Atlas.Ui.Client`).
- Align generator settings with API JSON: System.Text.Json, camelCase properties, **string enums** (`JsonStringEnumConverter` → values like `NotStarted`, `Medium`).
- Register generated client with Blazor `HttpClient` + `ApiBaseUrl`.
- Port problem-details / error parsing from `src/atlas.ui/src/app/api/client.ts` (wrapper around generated calls if needed).
- **SSE exception:** `GET /ai/sessions/{id}/events` (`text/event-stream`) stays hand-written (interop or streaming); do not expect NSwag CRUD generation to cover it.
- Replace React Query with an explicit **app state / cache service** skeleton (scoped WASM services + invalidate/refetch helpers for linked entities: tasks ↔ projects ↔ risks). Feature phases fill it in.

**Exit:** Sample call (e.g. list tasks) works from a Blazor page; OpenAPI artifact + generate script/docs checked in; regenerate instructions in the frontend README.

---

## Phase 4 — Shell, path routing, styling port

**Branch from:** umbrella

**Outcome:** App chrome and route map match React shell feature-for-feature; URLs are path-based; pages are stubs.

**Work:**

- Port `ShellLayout`, nav, global search/quick-add **shell slots** (stubs OK).
- Map routes from `src/atlas.ui/src/app/router.tsx` (~25 paths) to Blazor `@page` / `Router` **without** the hash prefix (`/tasks`, `/tasks/{taskId}`, `/team/...`, etc.).
- Port `index.css` / `App.css` class structure into Blazor `wwwroot` (keep class names to reduce churn).
- Login / setup stub pages equivalent to current “auth later” behavior.
- Theme hook (dark default; light still unavailable — match Settings behavior).
- Note deep-link / docs updates needed at cutover (`/#/x` → `/x`).

**Exit:** Navigating path routes renders stubs inside the shell; CSS looks recognizably Atlas.

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
- Prefer generated NSwag client for HTTP; keep UI-only view models thin.
- Reuse shared Blazor components (Modal, Spinner, LoadingButton) as needed.
- Keep server validation as source of truth; light client required-field checks only.
- Add/adjust Playwright flows against **path** URLs as features land (or batch late on the umbrella).

**Exit:** Manual smoke on `/tasks`, `/projects`, `/risks`, `/dashboard`, `/settings` (+ setup/import) matches React for happy paths and basic errors.

---

## Phase 6 — Team hub (highest rewrite surface)

**Branch from:** umbrella

**Outcome:** Team member overview, notes, Azure work items, member risks, growth goals/detail.

**Work:**

- Decompose React `TeamView.tsx` monolith into Blazor components/pages by tab/route (`notes`, `work-items`, `risks`, `growth`, goal detail).
- Port nested **path** routing and deep links.
- Member overview and growth goal detail as separate components (avoid one 2k-line razor file).
- Ensure cache/state updates cover team-linked mutations.

**Exit:** Full `/team/**` journey parity; no dependency on React Team view.

**Risks:** Largest regression surface; prefer smaller PR slices per tab if the umbrella review gets unwieldy.

---

## Phase 7 — AI assistant (SSE + markdown)

**Branch from:** umbrella

**Outcome:** Shell-wide AI panel with streaming session events and markdown transcripts.

**Work:**

- Port `AiState` / `AiPanel` behavior: open/close, resize, session lifecycle, transcript assembly.
- Consume `/ai/sessions/{id}/events` via JS interop `EventSource` **or** `HttpClient` streaming — pick one approach and document it; prefer minimal backend change. **Outside NSwag generation.**
- Markdown: Markdig (or equivalent) + GFM-ish subset + syntax highlighting strategy; explicit HTML sanitization.
- `localStorage` (or Blazor protected local storage) for panel default-open setting.

**Exit:** AI panel streams and renders comparable to React; e2e stubs for SSE updated.

**Risks:** Reconnect/terminal event edge cases; XSS if markdown sanitization is incomplete.

---

## Phase 8 — CI, Docker cutover, delete React, merge to `main`

**Branch from:** umbrella → **one merge to `main` when ready**

**Outcome:** Blazor is the only frontend on `main`; React hard-deleted.

**Work (order matters for restore-friendliness):**

1. Point `Dockerfile.ui` (or replace it) at Blazor publish output + nginx SPA fallback (**required** for path routing).
2. Update `docker-compose.yml` (`ApiBaseUrl` instead of `VITE_API_BASE_URL`).
3. Update CORS allowed origins if the UI origin/port changes.
4. Rewrite `.github/workflows/frontend-ci.yml`: restore/build WASM; Playwright against Blazor DOM; drop UI `npm ci`.
5. Update `docs/docker.md`, frontend README, `AGENTS.md` (path URLs, Blazor commands).
6. Final Playwright full suite + compose smoke on the umbrella.
7. **Standalone commit:** hard-delete `src/atlas.ui` (and React-only Docker/npm leftovers). Message clearly e.g. `Remove React UI (src/atlas.ui).` — restore via `git revert <sha>` or `git checkout <sha>^ -- src/atlas.ui`.
8. Merge umbrella → `main`.

**Exit:** `main` serves Blazor only; React gone in one revertible commit; docs and CI consistent.

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
| Playwright | Rewrite against Blazor DOM and **path** URLs; keep API/AI fixtures pattern from current e2e. |
| Manual smoke | Per-phase route checklist in each PR’s **Testing instructions**. |

### Rollback

- **Before merge to `main`:** React remains on `main`; discard or fix the umbrella.
- **After merge:** Revert the umbrella merge, or revert the standalone React-delete commit and temporarily restore the old `ui` image/tag if needed.

---

## Phase dependency graph

```text
Phase 1  umbrella + plan
    │
Phase 2  Blazor scaffold (umbrella only)
    │
Phase 3  NSwag OpenAPI client + cache skeleton
    │
Phase 4  Shell + path routes + CSS port
    │
    ├─► Phase 5  CRUD features (tasks → projects/risks → dashboard/settings)
    │
    ├─► Phase 6  Team hub          (after shell + cache; can overlap late Phase 5)
    │
    └─► Phase 7  AI panel + SSE    (after shell; independent of Team if needed)
            │
        Phase 8  CI + Docker cutover + hard-delete React (standalone commit) → merge umbrella to main
```

Phases 6 and 7 may proceed in parallel after Phase 4–5 foundations land, as long as both merge into the umbrella and resolve shell conflicts deliberately.

---

## Immediate next step

After this Phase 1 PR is accepted: cut `cursor/blazor-wasm-scaffold-82c4` from `cursor/blazor-wasm-frontend-82c4` and execute **Phase 2**.
