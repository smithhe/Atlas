using Atlas.Ui.Api.Generated;

namespace Atlas.Ui.Services;

/// <summary>Azure DevOps API calls. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class AzureDevOpsService
{
    private readonly IAtlasApiClient _api;

    public AzureDevOpsService(IAtlasApiClient api)
    {
        _api = api;
    }

    public Task<AtlasApiDTOsAzureDevOpsAzureConnectionDto> GetConnectionAsync(CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsGetAzureConnectionEndpointAsync(cancellationToken);

    public Task UpdateConnectionAsync(
        AtlasApiDTOsAzureDevOpsUpdateAzureConnectionRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsUpdateAzureConnectionEndpointAsync(request, cancellationToken);

    public Task<ICollection<AtlasApiDTOsAzureDevOpsAzureProjectDto>> ListProjectsAsync(
        string organization,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsListAzureProjectsEndpointAsync(organization, cancellationToken);

    public Task<ICollection<AtlasApiDTOsAzureDevOpsAzureTeamDto>> ListTeamsAsync(
        string organization,
        string projectId,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsListAzureTeamsEndpointAsync(organization, projectId, cancellationToken);

    public Task<ICollection<AtlasApiDTOsAzureDevOpsAzureUserDto>> ListUsersAsync(
        string organization,
        string projectId,
        string teamId,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsListAzureUsersEndpointAsync(organization, projectId, teamId, cancellationToken);

    public Task<AtlasApiDTOsAzureDevOpsAzureTeamAreaPathsDto> ListTeamAreaPathsAsync(
        string organization,
        string projectId,
        string teamName,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsListAzureTeamAreaPathsEndpointAsync(organization, projectId, teamName, cancellationToken);

    public Task ImportTeamAsync(
        AtlasApiDTOsAzureDevOpsImportAzureTeamRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsImportAzureTeamEndpointAsync(request, cancellationToken);

    public Task<AtlasApiDTOsAzureDevOpsImportAzureProductOwnersResultDto> ImportProductOwnersAsync(
        AtlasApiDTOsAzureDevOpsImportAzureProductOwnersRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsImportAzureProductOwnersEndpointAsync(request, cancellationToken);

    public Task<ICollection<AtlasApiDTOsAzureDevOpsAzureUserDto>> ListImportedUsersAsync(
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsListImportedAzureUsersEndpointAsync(cancellationToken);

    public Task<ICollection<AtlasApiDTOsAzureDevOpsAzureImportWorkItemDto>> ListImportWorkItemsAsync(
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsListAzureImportWorkItemsEndpointAsync(cancellationToken);

    public Task LinkWorkItemsAsync(
        AtlasApiDTOsAzureDevOpsLinkAzureWorkItemsRequest request,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsLinkAzureWorkItemsEndpointAsync(request, cancellationToken);

    public Task<AtlasApiDTOsAzureDevOpsAzureSyncStateDto> GetSyncStateAsync(CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsGetAzureSyncStateEndpointAsync(cancellationToken);

    public Task<AtlasApiDTOsAzureDevOpsAzureSyncResultDto> RunSyncAsync(CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsAzureDevOpsRunAzureSyncEndpointAsync(cancellationToken);
}
