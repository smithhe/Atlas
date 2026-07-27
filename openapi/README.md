# Atlas OpenAPI (v1)

Committed FastEndpoints Swagger export used by NSwag for the Blazor WASM client.

## Regenerate

No Postgres required — export mode uses an in-memory EF store and skips startup migrations.

```bash
# From repo root
bash scripts/regenerate-openapi.sh
```

Equivalent manual steps:

```bash
dotnet run --project src/backend/Api/Atlas.Api/Atlas.Api.csproj --launch-profile http -- --export-swagger-docs true
# then normalize wwwroot/openapi/v1.json → openapi/atlas.v1.json and run NSwag (see script)
```

## Artifacts

| Path | Role |
| --- | --- |
| `openapi/atlas.v1.json` | Committed OpenAPI document (CI drift-checked) |
| `src/frontend/Atlas.Ui/nswag.json` | NSwag C# client config |
| `src/frontend/Atlas.Ui/Api/Generated/AtlasApiClient.cs` | Generated typed client |

`GET /ai/sessions/{id}/events` is stripped before NSwag generation (SSE stays hand-written EventSource interop).
