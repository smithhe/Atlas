using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Azure work-item local notes. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class AzureWorkItemService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;

    public AzureWorkItemService(IAtlasApiClient api, AppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<WorkItemNote> AddLocalNoteAsync(
        Guid teamMemberId,
        int workItemId,
        string text,
        CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsTeamMembersAzureWorkItemsAddAzureWorkItemLocalNoteResponse saved =
            await _api.AtlasApiEndpointsTeamMembersAzureWorkItemsAddAzureWorkItemLocalNoteEndpointAsync(
                teamMemberId,
                workItemId,
                EntityRequestMappers.ToAddAzureWorkItemLocalNoteRequest(text),
                cancellationToken);

        var note = new WorkItemNote
        {
            Id = saved.Id ?? Guid.NewGuid(),
            CreatedIso = saved.CreatedAt?.ToString("o") ?? DateTimeOffset.UtcNow.ToString("o"),
            Text = text
        };

        TeamMember? member = _cache.Team.FirstOrDefault(m => m.Id == teamMemberId);
        if (member is not null)
        {
            var workItemKey = workItemId.ToString();
            _cache.UpdateTeamMember(EntityClone.TeamMember(
                member,
                azureItems: member.AzureItems.Select(item =>
                {
                    if (item.Id != workItemKey)
                    {
                        return item;
                    }

                    return EntityClone.AzureItem(item, localNotes: new[] { note }.Concat(item.LocalNotes).ToList());
                }).ToList()));
        }

        return note;
    }

    public void SetProject(Guid teamMemberId, string workItemId, string? projectId)
    {
        TeamMember? member = _cache.Team.FirstOrDefault(m => m.Id == teamMemberId);
        if (member is null)
        {
            return;
        }

        AzureItem? item = member.AzureItems.FirstOrDefault(a => a.Id == workItemId);
        if (item is null)
        {
            return;
        }

        AzureItem next = EntityClone.AzureItem(item, projectId: projectId, setProjectId: true);
        _cache.UpdateTeamMember(EntityClone.TeamMember(
            member,
            azureItems: member.AzureItems.Select(a => a.Id == workItemId ? next : a).ToList()));
    }
}
