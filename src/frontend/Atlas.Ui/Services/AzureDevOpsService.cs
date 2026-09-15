using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{

/// <summary>Azure DevOps API calls. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class AzureDevOpsService : IAzureDevOpsService
{
    private readonly IAtlasApiClient _api;

    public AzureDevOpsService(IAtlasApiClient api)
    {
        _api = api;
    }

    public async Task<AzureConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsAzureDevOpsAzureConnectionDto dto =
            await _api.AtlasApiEndpointsAzureDevOpsGetAzureConnectionEndpointAsync(cancellationToken);
        return ApiMappers.MapAzureConnection(dto);
    }

    public async Task<AzureConnection?> TryGetConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetConnectionAsync(cancellationToken);
        }
        catch (AtlasApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    public Task UpdateConnectionAsync(AzureUpdateConnection connection, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsUpdateAzureConnectionEndpointAsync(
            ApiMappers.ToUpdateAzureConnectionRequest(connection),
            cancellationToken);

    public async Task<IReadOnlyList<AzureProject>> ListProjectsAsync(string organization, CancellationToken cancellationToken = default)
    {
        ICollection<AtlasApiDTOsAzureDevOpsAzureProjectDto> list =
            await _api.AtlasApiEndpointsAzureDevOpsListAzureProjectsEndpointAsync(organization, cancellationToken);
        return list.Select(ApiMappers.MapAzureProject).ToList();
    }

    public async Task<IReadOnlyList<AzureTeam>> ListTeamsAsync(
        string organization,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        ICollection<AtlasApiDTOsAzureDevOpsAzureTeamDto> list =
            await _api.AtlasApiEndpointsAzureDevOpsListAzureTeamsEndpointAsync(organization, projectId, cancellationToken);
        return list.Select(ApiMappers.MapAzureTeam).ToList();
    }

    public async Task<IReadOnlyList<AzureUser>> ListUsersAsync(
        string organization,
        string projectId,
        string teamId,
        CancellationToken cancellationToken = default)
    {
        ICollection<AtlasApiDTOsAzureDevOpsAzureUserDto> list =
            await _api.AtlasApiEndpointsAzureDevOpsListAzureUsersEndpointAsync(organization, projectId, teamId, cancellationToken);
        return list.Select(ApiMappers.MapAzureUser).ToList();
    }

    public async Task<AzureTeamAreaPaths> ListTeamAreaPathsAsync(
        string organization,
        string projectId,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsAzureDevOpsAzureTeamAreaPathsDto dto =
            await _api.AtlasApiEndpointsAzureDevOpsListAzureTeamAreaPathsEndpointAsync(organization, projectId, teamName, cancellationToken);
        return ApiMappers.MapAzureTeamAreaPaths(dto);
    }

    public Task ImportTeamAsync(IReadOnlyList<AzureUser> users, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsImportAzureTeamEndpointAsync(
            ApiMappers.ToImportAzureTeamRequest(users),
            cancellationToken);

    public async Task<ImportProductOwnersResult> ImportProductOwnersAsync(
        IReadOnlyList<AzureUser> users,
        CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsAzureDevOpsImportAzureProductOwnersResultDto dto =
            await _api.AtlasApiEndpointsAzureDevOpsImportAzureProductOwnersEndpointAsync(
                ApiMappers.ToImportAzureProductOwnersRequest(users),
                cancellationToken);
        return ApiMappers.MapImportProductOwnersResult(dto);
    }

    public async Task<IReadOnlyList<AzureUser>> ListImportedUsersAsync(CancellationToken cancellationToken = default)
    {
        ICollection<AtlasApiDTOsAzureDevOpsAzureUserDto> list =
            await _api.AtlasApiEndpointsAzureDevOpsListImportedAzureUsersEndpointAsync(cancellationToken);
        return list.Select(ApiMappers.MapAzureUser).ToList();
    }

    public async Task<IReadOnlyList<AzureImportWorkItem>> ListImportWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        ICollection<AtlasApiDTOsAzureDevOpsAzureImportWorkItemDto> list =
            await _api.AtlasApiEndpointsAzureDevOpsListAzureImportWorkItemsEndpointAsync(cancellationToken);
        return list.Select(ApiMappers.MapAzureImportWorkItem).ToList();
    }

    public Task LinkWorkItemsAsync(LinkAzureWorkItemsRequest request, CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsLinkAzureWorkItemsEndpointAsync(
            ApiMappers.ToLinkAzureWorkItemsRequest(request),
            cancellationToken);

    public async Task<AzureSyncState> GetSyncStateAsync(CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsAzureDevOpsAzureSyncStateDto dto =
            await _api.AtlasApiEndpointsAzureDevOpsGetAzureSyncStateEndpointAsync(cancellationToken);
        return ApiMappers.MapAzureSyncState(dto);
    }

    public async Task<AzureSyncResult> RunSyncAsync(CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsAzureDevOpsAzureSyncResultDto dto =
            await _api.AtlasApiEndpointsAzureDevOpsRunAzureSyncEndpointAsync(cancellationToken);
        return ApiMappers.MapAzureSyncResult(dto);
    }
}
}
