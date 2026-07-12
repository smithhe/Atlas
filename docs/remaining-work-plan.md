# Atlas Remaining Work Plan

**Scope:** Finish Atlas as a local, single-user engineering-manager cockpit.  
**Out of scope:** Authentication, authorization, multi-tenant/multi-user hardening, and cloud hosting.  
**Target runtime:** Local Docker Compose (`ui` + `api` + `postgres`), with Azure DevOps PAT and **required** OpenAI API key via env/secrets (clear setup guidance when missing).

---

## Decisions (locked)

| Topic | Decision |
|---|---|
| Primary daily use | Equal mix of personal EM cockpit **and** Azure DevOps–centric team views |
| Team Pulse / `activitySnapshot` | **Derive** from notes, signals, `currentFocus`, and Azure work items (no new persisted snapshot fields) |
| Team note ADO / PR fields | **Persist** in backend; keep UI |
| Extra Azure WI fields (PRs, history, time) | **Leave as-is** (low priority) |
| Team member provenance | **Azure import only** — document; no manual create UI |
| Shell search + Quick Add | **Implement both** for v1 |
| AI “done” scope | **Full coverage** — Team, Risks, Projects, Settings (plus existing Dashboard/Tasks) |
| Insert Draft | **Wire into active editor** (task/note) |
| Sequencing | **Product honesty first**, then Docker Compose |
| Demo seed data | Optional Compose profile: `docker compose --profile demo up` |
| OpenAI in local v1 | **Required** — guide key setup clearly when missing |
| Frontend test bar | **Broader UI test coverage** (not smoke-only) |

### Team Pulse derivation rules (locked)

1. `lastUpdatedIso` = latest of: most recent note `LastModifiedAt`/`CreatedAt`, signal-related update timestamps if available, most recent linked Azure work item `ChangedDateUtc`.
2. `bullets` (max ~3–5):
   - Current focus (if set)
   - Load/delivery/support signal summary (if present)
   - Open / recent Azure work item count or top active item title
   - Latest note title/type (e.g. “1:1 · 3 days ago”)
3. If nothing meaningful exists, Dashboard Team Pulse shows an empty/quiet state (not fake data).

Do **not** add persisted curated snapshot fields in v1; revisit only if derived pulse feels too mechanical.

---

## Current state (as of `main`)

| Area | Status |
|---|---|
| Tasks / Risks / Projects CRUD | Built and wired |
| Team members, notes, signals, member risks, growth | Built; note ADO/PR fields persist; Team Pulse derived |
| Dashboard heuristics | Built; Team Pulse uses derived `activitySnapshot` |
| Azure DevOps setup, sync, import, linking | Built; config/naming and a few UX gaps remain |
| AI panel (Dashboard + Tasks) | Built; other views show unsupported actions |
| Backend tests (unit / functional / integration) + CI | In place |
| Frontend tests | None |
| Docker / Compose | Not started |
| Auth | Explicitly deferred (and excluded here) |

---

## Definition of done (local v1)

1. Every visible control either works end-to-end or is removed/disabled.
2. UI ↔ API ↔ DB round-trips are honest (no display-only fields that silently drop on reload).
3. Dashboard Team Pulse uses **derived** `activitySnapshot` (quiet empty state when no signal).
4. Team members are Azure-import-only; docs and UI match that.
5. Shell **search** and **Quick Add** work for core entities.
6. AI contexts exist for **Dashboard, Tasks, Team, Risks, Projects, Settings**; Insert Draft writes into the active editor.
7. OpenAI key is treated as required for the AI experience, with clear setup guidance when missing.
8. `docker compose up` brings up Postgres + API + UI; `--profile demo` optionally seeds data.
9. Backend CI stays green; frontend has **broader UI test coverage** in CI.

---

## Phase 1 — Product honesty (data integrity) — **do first**

**Goal:** Eliminate silent data loss and dashboard emptiness before packaging.

### 1.1 Team `activitySnapshot` / Team Pulse — **locked: derive**

- [x] Implement derivation (API mapper or UI mapper) per rules in Decisions.
- [x] Stop hardcoding empty `activitySnapshot` in `mappers.ts`.
- [x] Verify Dashboard Team Pulse / drift cards update when notes/signals/Azure data change.
- [x] Resolve `NotesForLater.txt` (delete after implementation).

### 1.2 Team note ADO / PR fields — **locked: persist**

- [x] Add optional `AdoWorkItemId` / `PrUrl` to `TeamNote` + EF config + migration.
- [x] Extend create/update note commands, validators, DTOs, mappers.
- [x] Wire UI create/edit forms to send/receive the fields.
- [x] Add functional tests for note create/update including these fields.

### 1.3 Azure work item enriched fields — **locked: defer**

- [x] No sync expansion for PRs/history/time in v1.
- [ ] Optional later: cosmetic cleanup so UI doesn’t imply fields Atlas never populates (only if it confuses during use).

### 1.4 Team member provenance — **locked: Azure import only**

- [x] Document that team members come from Azure user import (setup + settings/import flows).
- [x] Do **not** add manual create/delete UI.
- [x] Ensure UI copy / empty states don’t suggest manual member creation.
- [x] Keep backend create endpoint for import/tests; no need to remove API unless it causes confusion.

### 1.5 Settings consistency

- [x] Document `defaultAiPanelOpen` as localStorage-only (or move to `Settings` if you want cross-browser portability — not required for v1).
- [x] Keep theme “Dark (locked)” or remove the control until themes exist.

### Acceptance

- [x] Reload after editing team notes does not lose ADO/PR fields.
- [x] Dashboard team section matches the confirmed pulse strategy.
- [x] No UI path implies manual team-member creation.

---

## Phase 2 — Azure DevOps finish-up

**Goal:** Reliable Azure path for the ADO half of daily use.

### Work items

- [ ] Fix PAT config key mismatch (`AzureDevopsToken` vs error text `AzureDevOps:Pat`). One name across config, Compose env, Bruno, docs.
- [ ] Surface product-owner duplicate-name skips in UI (backend TODO).
- [ ] Improve sync UX: last sync time, in-progress state, clear errors when PAT/org/project misconfigured.
- [ ] Confirm area-path scoping + watermark vs `docs/azure-devops-sync-flow.md`; update doc if drifted.
- [ ] Expand Bruno beyond Settings/Azure to Tasks, Risks, Projects, Team, Growth, AI.

### Acceptance

- With PAT set, setup → sync → import → link works cleanly.
- Without PAT, non-Azure CRUD still works; Azure flows fail with actionable messages.

---

## Phase 3 — Shell: Search + Quick Add — **locked: implement both**

### Work items

- [ ] **Global search:** client-side search over hydrated tasks, risks, people (and projects if cheap); navigate to the selected entity.
- [ ] **Quick Add:** modal/flow to create Task, Risk, or Team Note from anywhere (member picker for notes).
- [ ] Remove Settings “command palette coming later” / placeholder help copy once search covers the need (or replace with a short “use search” hint).
- [ ] Clean `NotFoundView` AI placeholder.

### Acceptance

- Top bar search and Quick Add are real workflows, not placeholders.

---

## Phase 4 — AI full coverage — **locked: all main surfaces**

**Goal:** AI is a first-class assistant across the app; OpenAI key is required for that experience.

### Work items

- [ ] Extend `AiViewScope` with Team, Risks, Projects, Settings (keep Dashboard, Tasks).
- [ ] Implement `IAiPromptContextBuilder` for each new scope; register in `Program.cs`.
- [ ] Extend frontend `resolveView()` / AI state so those views are supported.
- [ ] Remove “(draft)” / “not supported” dead ends on shipped actions.
- [ ] **Insert Draft:** insert latest AI response into the active editor target (task description or note body); define clear fallback when no editor is focused (e.g. disable button or show “focus a field first”).
- [ ] **OpenAI required UX:** when API key missing, AI panel shows setup steps (env var / user-secrets / Compose `.env`) instead of a vague failure.
- [ ] Optional: true token streaming vs buffered chunking (polish; not blocking if UX is acceptable).

### Acceptance

- Every main nav surface can start a grounded AI conversation.
- Insert Draft mutates the focused editor content.
- Missing OpenAI key produces a clear local setup guide.

---

## Phase 5 — Frontend data-layer cleanup

- [ ] Add `growth` to `AppQueryScope` / `invalidateAppQueries` (or document optimistic-only growth updates).
- [ ] Reduce N+1 list→detail fetches for projects/risks/team members.
- [ ] Remove deprecated `loadInitialState.ts` when unused.
- [ ] Audit cache updates after Azure import/sync.

### Acceptance

- No stale growth/team data after common mutations; no dead deprecated loaders.

---

## Phase 6 — Local Docker packaging — **after product honesty**

**Goal:** One-command local run.

### Work items

- [ ] `Dockerfile` for `Atlas.Api` (multi-stage publish).
- [ ] `Dockerfile` for `atlas.ui` (build + static serve via nginx/caddy).
- [ ] `docker-compose.yml`: `db`, `api`, `ui` with healthchecks and env wiring.
- [ ] `.env.example` for DB, CORS, Azure PAT, **required** `OpenAI__ApiKey`, model/base URL, `Ai__*`.
- [ ] Schema strategy: apply EF migrations on API startup (or init container) for Compose; document vs `EnsureCreated()` in bare Development.
- [ ] Health endpoint(s) for Compose.
- [ ] Docs: `docker compose up --build`, Azure optional, OpenAI required for AI, hash routes.
- [ ] **`--profile demo`:** wire `DevDatabaseSeeder` (currently empty) to seed demo tasks/risks/projects/members/notes when profile enabled.

### Acceptance

- Fresh machine with Docker runs Atlas.
- `docker compose --profile demo up` yields usable demo data.
- Default (no demo profile) starts empty aside from schema.

---

## Phase 7 — Quality & operability

### Already done

- Backend unit / functional / integration tests
- GitHub Actions backend CI with Postgres

### Remaining — **broader UI coverage**

- [ ] Frontend lint + `npm run build` in CI.
- [ ] Playwright (or equivalent) suite covering broader flows, not just one smoke:
  - Continue → dashboard
  - Create/edit task, risk, project
  - Team note with ADO/PR fields round-trip
  - Search navigates to an entity
  - Quick Add creates an item
  - AI panel: missing-key guidance; with key mocked/stubbed, conversation start on multiple views
  - Insert Draft into a focused editor (stubbed AI response OK)
- [ ] Compose smoke script (health + one CRUD path) optional alongside UI suite.
- [ ] Secrets only via Compose/env — never baked into images.

### Explicitly deferred

- Authentication / authorization
- Multi-user tenancy / RBAC
- Cloud deploy / TLS / managed identity
- Extending Azure WI sync with PR/history/time fields

---

## Sequencing (updated)

```text
Phase 1  Product honesty (notes ADO/PR; Team Pulse after confirmation; Azure-import-only members)
   │
Phase 2  Azure polish (PAT key, sync UX, PO warning, Bruno)
   │
Phase 3  Search + Quick Add
   │
Phase 4  AI full coverage + Insert Draft + OpenAI setup guidance
   │
Phase 5  Query-layer cleanup
   │
Phase 6  Docker Compose + optional --profile demo seed
   │
Phase 7  Broader frontend UI tests + CI
```

---

## Concrete first tickets

1. ~~**Derive `activitySnapshot` / Team Pulse**; clear `NotesForLater.txt`.~~ **Done (Phase 1.1)**
2. ~~**Persist team note `adoWorkItemId` / `prUrl`** (entity → API → UI → tests).~~ **Done (Phase 1.2)**
3. ~~**Document Azure-import-only members**; align empty states / copy.~~ **Done (Phase 1.4)**
4. **Normalize Azure PAT config key** + sync UX improvements.
5. **Implement global search + Quick Add**.
6. **AI scopes for Team / Risks / Projects / Settings** + Insert Draft + missing-key setup UX.
7. **Docker Compose** + `--profile demo` seeder.
8. **Broader Playwright UI suite** + frontend CI job.

---

## Non-goals

- Auth / multi-user / cloud deploy
- New product areas (OKRs, calendars, Slack bots, etc.)
- Visual redesign of the shell
- Azure WI PR/history/time sync expansion (deferred)

---

## Reference map

| Topic | Location |
|---|---|
| Cloud/local agent notes | `AGENTS.md` |
| Azure sync flow + team member provenance | `docs/azure-devops-sync-flow.md` |
| API entry | `src/backend/Api/Atlas.Api/Program.cs` |
| AI scopes | `src/backend/Core/Atlas.Application/Abstractions/Ai/AiViewScope.cs` |
| Derived activity snapshot | `src/atlas.ui/src/app/team.ts` (`deriveActivitySnapshot`) |
| Team note entity (ADO/PR fields) | `src/backend/Core/Atlas.Domain/Entities/TeamNote.cs` |
| Azure WI entity (core fields only) | `src/backend/Core/Atlas.Domain/Entities/AzureWorkItem.cs` |
| Browser-local AI panel pref | `src/atlas.ui/src/app/localSettings.ts` |
| Query invalidation scopes | `src/atlas.ui/src/app/queries/invalidateAppQueries.ts` |
| Backend CI | `.github/workflows/backend-tests.yml` |
| Bruno collections | `bruno/Atlas/` |
| Seeder stub | `src/backend/Infrastructure/Atlas.Persistence/Seeding/DevDatabaseSeeder.cs` |
