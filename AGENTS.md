## Cursor Cloud specific instructions

- Atlas has two main development services: the ASP.NET Core API in `src/backend/Api/Atlas.Api` and the React/Vite UI in `src/atlas.ui`. Standard UI commands are documented in `src/atlas.ui/README.md` and `src/atlas.ui/package.json`; backend ports are documented in `src/backend/Api/Atlas.Api/Properties/launchSettings.json`.
- The API requires PostgreSQL on `localhost:5432` with the default `atlas` database, `atlas` user, and `change-me` password unless `ConnectionStrings:AtlasDb` is overridden. In `Development`, the API creates its schema on startup with `EnsureCreated()`.
- Azure DevOps and OpenAI are optional for local CRUD smoke tests. If Azure DevOps credentials are not configured, navigate directly to `/#/tasks`, `/#/projects`, or `/#/dashboard` to exercise core Atlas flows.
