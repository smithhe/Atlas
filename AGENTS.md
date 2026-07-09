## Cursor Cloud specific instructions

- Atlas has two main development services: the ASP.NET Core API in `src/backend/Api/Atlas.Api` and the React/Vite UI in `src/atlas.ui`. Standard UI commands are documented in `src/atlas.ui/README.md` and `src/atlas.ui/package.json`; backend ports are documented in `src/backend/Api/Atlas.Api/Properties/launchSettings.json`.
- The API requires PostgreSQL on `localhost:5432` with the default `atlas` database, `atlas` user, and `change-me` password unless `ConnectionStrings:AtlasDb` is overridden. In `Development`, the API creates its schema on startup with `EnsureCreated()`.
- Azure DevOps and OpenAI are optional for local CRUD smoke tests. If Azure DevOps credentials are not configured, navigate directly to `/#/tasks`, `/#/projects`, or `/#/dashboard` to exercise core Atlas flows.

## Backend testing conventions

### Pyramid

| Layer | Project | Purpose |
| --- | --- | --- |
| **Unit** | `Atlas.Tests.Unit` | MediatR handlers, FluentValidation validators, behaviors — mocked/faked dependencies, no HTTP, no DB |
| **Functional** | `Atlas.Tests.Functional` | HTTP endpoint contracts via `WebApplicationFactory` — InMemory EF, fake Azure/AI |
| **Integration** | `Atlas.Tests.Integration` | Persistence/repository behavior and multi-step workflows — InMemory locally; **Postgres in CI** when `ConnectionStrings__AtlasDb` / `ATLAS_TEST_DB` is set |

### Placement rules

- Prefer a unit test for each MediatR handler under `Atlas.Tests.Unit/Features/...`, mirroring `Atlas.Application/Features/...`.
- Prefer a functional `*EndpointTests.cs` for each public HTTP route under `Atlas.Tests.Functional/Endpoints/...`.
- Prefer integration tests for complex repository queries (includes, filters, ordering, link/unlink) under `Atlas.Tests.Integration/Persistence/...`.
- Do **not** add domain entity unit tests, frontend tests, auth tests, Testcontainers, or a coverage % gate.

### Postgres scope

- **Integration only** may use Postgres (CI service container). Functional stays InMemory; Unit stays mocked.
- Local default for Integration remains InMemory. When `ConnectionStrings__AtlasDb` or `ATLAS_TEST_DB` is set, the Integration test host switches to Npgsql.

### Handler unit test style

- **New** handler tests should prefer **Moq** + **FluentAssertions**.
- Existing hand-rolled fakes under `Atlas.Tests.Unit/Fakes/` may remain (legacy tests are not required to migrate to Moq+FA); extend fakes only when Moq alone is awkward for a repository interface.
- Cover happy path plus important failure paths (missing entity, invalid dependency).
- Integration Postgres: each `AtlasIntegrationApplicationFactory` creates an isolated `atlas_it_{guid}` database (never share the base `atlas` DB across factories).
