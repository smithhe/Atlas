# Atlas Remaining Work Plan

**Scope:** Finish Atlas as a local, single-user engineering-manager cockpit.  
**Out of scope:** Authentication, authorization, multi-tenant/multi-user hardening, and cloud hosting.  
**Target runtime:** Local Docker Compose (`ui` + `api` + `postgres`), with optional Azure DevOps PAT and OpenAI API key via env/secrets.

---

## Current state (as of `main`)

Atlas is already a substantial app:

| Area | Status |
|---|---|
| Tasks / Risks / Projects CRUD | Built and wired |
| Team members, notes, signals, member risks, growth | Built; some UI fields not persisted |
| Dashboard heuristics | Built; limited by empty `activitySnapshot` |
| Azure DevOps setup, sync, import, linking | Built; config/naming and a few UX gaps remain |
| AI panel (Dashboard + Tasks) | Built; other views show unsupported actions |
| Backend tests (unit / functional / integration) + CI | In place |
| Frontend tests | None |
| Docker / Compose | Not started |
| Auth | Explicitly deferred (and excluded here) |

Known open note in repo (`NotesForLater.txt`):

> Current Focus is on team members but only used on the dashboard page, we need to update the backend to set this or remove it

---

## Definition of done (local v1)

Atlas is “finished” for local Docker use when:

1. Every visible control either works end-to-end or is removed/disabled.
2. UI ↔ API ↔ DB round-trips are honest (no display-only fields that silently drop on reload).
3. `docker compose up` brings up Postgres + API + UI with documented env vars.
4. Azure DevOps and OpenAI remain **optional**; core CRUD/dashboard works without them.
5. Dashboard team/pulse signals are meaningful (or clearly not shown).
6. AI actions only appear where backend context exists (or those contexts are implemented).
7. Backend CI stays green; critical UI flows have at least smoke coverage or Bruno coverage.

---

## Phase 0 — Local Docker packaging

**Goal:** One-command local run without relying on host-installed Postgres/Node/.NET beyond Docker.

### Work items

- [ ] Add `Dockerfile` for `Atlas.Api` (multi-stage publish).
- [ ] Add `Dockerfile` for `atlas.ui` (Vite build + static serve, or Vite preview; prefer nginx/caddy serving `dist`).
- [ ] Add `docker-compose.yml` with services:
  - `db` — Postgres 16, volume, healthcheck
  - `api` — depends on healthy `db`, env for connection string / CORS / secrets
  - `ui` — depends on `api`, `VITE_API_BASE_URL` (or runtime config) pointed at API
- [ ] Add `.env.example` documenting:
  - `ConnectionStrings__AtlasDb`
  - `Cors__AllowedOrigins`
  - `AzureDevopsToken` (or renamed key — see Phase 2)
  - `OpenAI__ApiKey`, `OpenAI__Model`, `OpenAI__BaseUrl`
  - `Ai__*` overrides if needed
- [ ] Decide schema strategy for containers:
  - **Preferred for local Compose:** run EF migrations on API startup (or an init container/`dotnet ef database update`), not `EnsureCreated()`.
  - Keep `EnsureCreated()` only for Development-without-migrations if still useful; document the difference.
- [ ] Wire health endpoints (`/health` or `/healthz`) for Compose healthchecks.
- [ ] Update root docs (`README` or `AGENTS.md`) with:
  - `docker compose up --build`
  - how to run without Azure/OpenAI
  - hash-router entry points (`/#/dashboard`, `/#/tasks`, etc.)
- [ ] Optional: seed path via `DevDatabaseSeeder` for first-run demo data (empty stub today).

### Acceptance

- Fresh machine with only Docker can start Atlas and use Tasks/Projects/Dashboard.
- API connects to Compose Postgres; UI can call API across containers (CORS + base URL correct).

---

## Phase 1 — Make existing UI honest (data integrity)

**Goal:** Eliminate silent data loss and dashboard fake emptiness. Highest product leverage.

### 1.1 Team `activitySnapshot` / current focus

**Problem:** Frontend always maps `activitySnapshot` to empty (`mappers.ts`). Dashboard Team Pulse / drift freshness depends on it. `NotesForLater.txt` flags current-focus/team-member dashboard usage.

**Decide one approach (pick A or B):**

| Option | Approach |
|---|---|
| **A — Derive** | Compute snapshot from notes, signals, Azure work items, last reviewed dates (API or UI). Persist nothing new. |
| **B — Persist** | Add backend fields (or reuse signals/profile) and edit UI on member overview; return in team-member DTOs. |

**Recommended:** **A for v1** (derive from existing notes/signals/Azure items + `currentFocus`), then optionally persist curated bullets later.

Work items:

- [ ] Define snapshot rules (e.g. last note date, signal summary, open ADO count, current focus).
- [ ] Implement mapping in API DTO or UI mapper; stop hardcoding empty objects.
- [ ] Verify Dashboard Team Pulse / drift cards change when notes/signals update.
- [ ] Resolve `NotesForLater.txt` (implement or delete the note).

### 1.2 Team note ADO / PR fields

**Problem:** UI types and views show `adoWorkItemId` / `prUrl` on notes; `TeamNote` entity has neither. Values cannot survive reload.

Work items:

- [ ] Add optional `AdoWorkItemId` / `PrUrl` (or equivalent) to `TeamNote` + EF config + migration.
- [ ] Extend create/update note commands, validators, DTOs, mappers.
- [ ] Wire UI create/edit forms to send/receive the fields.
- [ ] Add functional test coverage for note create/update with these fields.

### 1.3 Azure work item enriched fields

**Problem:** UI expects `prUrls`, history, time taken, commits URL, start date, etc.; synced `AzureWorkItem` only stores core WIQL fields.

Work items:

- [ ] Inventory which UI fields are shown vs decorative in `TeamWorkItemDetailView`.
- [ ] Either:
  - **Extend sync** to fetch/store the needed fields, or
  - **Trim UI** to fields Atlas actually syncs (title, state, type, area, iteration, assigned, url) plus local notes.
- [ ] Prefer trimming unused chrome for v1 unless PR links are required daily.

### 1.4 Settings consistency

Work items:

- [ ] Document that `defaultAiPanelOpen` is localStorage-only (or move to `Settings` if you want it portable across browsers).
- [ ] Keep theme “Dark (locked)” or remove the control until themes exist — avoid fake settings.

### Acceptance

- Reload after editing team notes/member overview does not lose displayed fields.
- Dashboard team section reflects real activity or is explicitly hidden when empty.

---

## Phase 2 — Azure DevOps finish-up

**Goal:** Reliable optional integration for local use.

### Work items

- [ ] Fix PAT config key mismatch: code reads `AzureDevopsToken`, error text says `AzureDevOps:Pat`. Pick one name, update config, Compose env, Bruno, docs.
- [ ] Surface product-owner duplicate-name skips in UI (backend TODO in import handler).
- [ ] Improve sync UX in Settings: last sync time, in-progress state, clear error messages when PAT/org/project misconfigured.
- [ ] Confirm area-path scoping + watermark behavior still matches `docs/azure-devops-sync-flow.md`; update doc if drifted.
- [ ] Manual team member create/delete: API exists; add UI affordances **or** document “members come from Azure import only” and hide dead ends.
- [ ] Expand Bruno collection beyond Settings/Azure to Tasks, Risks, Projects, Team, Growth, AI (smoke scripts for local Compose).

### Acceptance

- With PAT set in Compose env, setup → sync → import → link works without code changes.
- Without PAT, core app still usable; Azure pages fail gracefully.

---

## Phase 3 — Shell & navigation polish

**Goal:** Remove placeholder chrome that makes the app feel unfinished.

### Work items

- [ ] **Global search** (`ShellLayout`): implement client-side search over hydrated tasks/risks/team members (good enough for local single-user), or remove the input until ready.
- [ ] **Quick Add**: modal/flow to create Task, Risk, or Team Note from anywhere; or remove the button.
- [ ] Settings copy: remove “command palette coming later” / placeholder AI help, or implement minimal equivalents.
- [ ] `NotFoundView` AI placeholder: disable or provide a static help message.

### Acceptance

- Top bar and settings contain no “placeholder” / “coming later” dead controls.

---

## Phase 4 — AI completion (optional but high polish)

**Goal:** AI is either fully useful on main surfaces or visibly scoped.

### Minimum (recommended for v1)

- [ ] Hide/disable AI quick actions on Team / Risks / Projects / Settings until backends exist.
- [ ] Remove “(draft)” labels from actions that already work; keep draft only for unimplemented ones (ideally none visible).
- [ ] Wire **Insert Draft** to insert the latest AI response into a known target (task description, note body, or clipboard) — or remove the button.

### Stretch

- [ ] Add `AiViewScope` values + `IAiPromptContextBuilder` implementations for Team, Risks, Projects.
- [ ] Register builders in `Program.cs`; extend frontend `resolveView()`.
- [ ] Replace buffered fake streaming in `OpenAiChatModelClient` with true token streaming if latency matters.
- [ ] Prompt/context size tuning against real Dashboard/Tasks payloads.

### Acceptance

- User never clicks an AI action that only prints “not supported.”
- Dashboard/Tasks AI remains the supported path unless stretch scopes ship.

---

## Phase 5 — Frontend data-layer cleanup

**Goal:** Keep TanStack Query solid after the AppState migration.

### Work items

- [ ] Add `growth` to `AppQueryScope` / `invalidateAppQueries` (or document that growth is only updated via `useAppCache` optimistic paths).
- [ ] Reduce N+1 list→detail fetches for projects/risks/team members (batch/detail endpoints or richer list DTOs).
- [ ] Remove deprecated `loadInitialState.ts` once nothing imports it.
- [ ] Audit optimistic cache updates for cross-entity links after Azure import/sync.

### Acceptance

- No stale growth/team data after common mutations; no dead deprecated loaders.

---

## Phase 6 — Quality & operability (local)

**Goal:** Confidence without turning this into a SaaS hardening project.

### Already done (do not redo)

- Backend unit / functional / integration tests
- GitHub Actions backend CI with Postgres for integration tests

### Remaining

- [ ] Frontend lint + `npm run build` in CI (add workflow or job).
- [ ] Optional Playwright smoke: login/continue → dashboard → create task → appears in list (no auth).
- [ ] Compose integration smoke script (curl health + one CRUD path).
- [ ] Ensure non-Development container path applies migrations (see Phase 0).
- [ ] Keep secrets out of images; pass PAT/OpenAI via Compose env only.

### Explicitly deferred (per product choice)

- Authentication / authorization
- Multi-user tenancy / RBAC
- Cloud deploy, TLS termination, managed identity
- Public production CORS / hardening beyond local network use

---

## Suggested sequencing

```text
Phase 0  Docker Compose (unblocks daily use)
   │
Phase 1  Honest data (activitySnapshot, team note fields, trim ADO extras)
   │
Phase 2  Azure polish (PAT key, sync UX, PO warning, Bruno)
   │
Phase 3  Remove shell placeholders (search / quick add)
   │
Phase 4  AI scope hygiene (+ optional new contexts)
   │
Phase 5  Query-layer cleanup
   │
Phase 6  UI CI + smoke tests
```

Phases 0–1 are the real “finish line” for a local tool.  
Phases 2–4 make it feel intentional day-to-day.  
Phases 5–6 protect against regressions.

---

## Concrete first tickets (ready to cut)

1. **Docker Compose stack** for `db` + `api` + `ui` with `.env.example` and migration-on-start.
2. **Derive or persist `activitySnapshot`** so Dashboard Team Pulse is real; clear `NotesForLater.txt`.
3. **Persist team note `adoWorkItemId` / `prUrl`** (entity → API → UI → test).
4. **Normalize Azure PAT config key** + docs/Compose env.
5. **Remove or implement** Shell search + Quick Add + Insert Draft.
6. **Disable unsupported AI contexts** in the UI.
7. **Add frontend build to CI**; optional Playwright happy path.

---

## Non-goals for this plan

- Adding Clerk/Entra/Identity auth
- Multi-user accounts or org isolation
- Deploying to cloud/Kubernetes
- Large new product areas (OKRs, calendars, Slack bots, etc.)
- Visual redesign of the existing shell

---

## Reference map

| Topic | Location |
|---|---|
| Cloud/local agent notes | `AGENTS.md` |
| Azure sync flow | `docs/azure-devops-sync-flow.md` |
| Open product note | `NotesForLater.txt` |
| API entry | `src/backend/Api/Atlas.Api/Program.cs` |
| AI scopes | `src/backend/Core/Atlas.Application/Abstractions/Ai/AiViewScope.cs` |
| Empty activity snapshot | `src/atlas.ui/src/app/api/mappers.ts` |
| Team note entity (missing ADO/PR) | `src/backend/Core/Atlas.Domain/Entities/TeamNote.cs` |
| Azure WI entity (core fields only) | `src/backend/Core/Atlas.Domain/Entities/AzureWorkItem.cs` |
| Query invalidation scopes | `src/atlas.ui/src/app/queries/invalidateAppQueries.ts` |
| Backend CI | `.github/workflows/backend-tests.yml` |
| Bruno collections | `bruno/Atlas/` |
