# Atlas OpenAPI (v1)

Committed FastEndpoints Swagger export used by NSwag for the Blazor WASM client.

## Regenerate (requires Postgres)

Default connection string:

`Host=localhost;Port=5432;Database=atlas;Username=atlas;Password=change-me`

```bash
# From repo root (Postgres must be accepting connections)
bash scripts/regenerate-openapi.sh
```

Equivalent manual steps:

```bash
dotnet run --project src/backend/Api/Atlas.Api/Atlas.Api.csproj --launch-profile http -- --export-swagger-docs true
# then normalize wwwroot/openapi/v1.json → openapi/atlas.v1.json and run NSwag (see script)
```

Export runs after DB `EnsureCreated()` / `Migrate()` — there is **no** early-export skip-DB path.

## Artifacts

| Path | Role |
| --- | --- |
| `openapi/atlas.v1.json` | Committed OpenAPI document (CI drift-checked) |
| `src/frontend/Atlas.Ui/nswag.json` | NSwag C# client config |
| `src/frontend/Atlas.Ui/Api/Generated/AtlasApiClient.cs` | Generated typed client |

`GET /ai/sessions/{id}/events` is stripped before NSwag generation (SSE stays hand-written EventSource interop).
