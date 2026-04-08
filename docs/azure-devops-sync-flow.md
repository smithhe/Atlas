# Azure DevOps Setup + Sync Flow

```mermaid
flowchart TD
    user[User] --> settingsUi[Settings_UI]
    settingsUi --> saveSettings["PUT /settings"]
    settingsUi --> saveConnection["PUT /azure-devops/connection"]

    settingsUi --> listProjects["GET /azure-devops/projects"]
    settingsUi --> listUsers["GET /azure-devops/users"]
    listUsers --> importTeam["POST /azure-devops/team/import"]
    listUsers --> importProductOwners["POST /azure-devops/product-owners/import"]

    settingsUi --> syncNow["POST /azure-devops/sync"]
    syncNow --> wiql["WIQL Query IDs"]
    wiql --> batchFetch["WorkItems Batch Fetch"]
    batchFetch --> upsertDb["Upsert AzureWorkItems"]
    upsertDb --> updateState["Update Sync Watermark"]
    updateState --> syncState["GET /azure-devops/sync-state"]

    settingsUi --> importPage[AzureImport_UI]
    importPage --> listImport["GET /azure-devops/import/work-items"]
    importPage --> linkItems["POST /azure-devops/import/link"]

    nightly[NightlyService] --> syncNow
```

## Product Owner Mapping Note

- `AzureProductOwnerMapping` is intentionally many-to-one to `ProductOwner`.
- Import dedupes Product Owners by display name (case-insensitive), so two Azure identities can resolve to one local Product Owner record.
- `AzureUniqueName` is still unique per mapping row, so one Azure identity cannot be imported twice.
