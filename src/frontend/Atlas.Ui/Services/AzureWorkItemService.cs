using Atlas.Ui.Api.Generated;

namespace Atlas.Ui.Services;

/// <summary>Azure work-item local notes. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class AzureWorkItemService
{
    private readonly IAtlasApiClient _api;

    public AzureWorkItemService(IAtlasApiClient api)
    {
        _api = api;
    }

    public Task<AtlasApiDTOsTeamMembersAzureWorkItemsAddAzureWorkItemLocalNoteResponse> AddLocalNoteAsync(
        Guid teamMemberId,
        int workItemId,
        string text,
        CancellationToken cancellationToken = default) =>
        _api.AtlasApiEndpointsTeamMembersAzureWorkItemsAddAzureWorkItemLocalNoteEndpointAsync(
            teamMemberId,
            workItemId,
            new AtlasApiDTOsTeamMembersAzureWorkItemsAddAzureWorkItemLocalNoteRequest { Text = text },
            cancellationToken);
}
