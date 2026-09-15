using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{

    /// <summary>Azure work-item local notes. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
    public sealed class AzureWorkItemService : IAzureWorkItemService
    {
        private readonly IAtlasApiClient _api;
        private readonly IAppCacheService _cache;

        public AzureWorkItemService(IAtlasApiClient api, IAppCacheService cache)
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

            if (saved.Id is not Guid noteId || noteId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Add Azure work item local note response did not include a note id.");
            }

            var note = new WorkItemNote
            {
                Id = noteId,
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

        public async Task SetProjectAsync(
            Guid teamMemberId,
            string workItemId,
            Guid? projectId,
            CancellationToken cancellationToken = default)
        {
            TeamMember member = _cache.Team.FirstOrDefault(m => m.Id == teamMemberId)
                ?? throw new InvalidOperationException($"Team member {teamMemberId} is not in the cache.");

            AzureItem item = member.AzureItems.FirstOrDefault(a => a.Id == workItemId)
                ?? throw new InvalidOperationException(
                    $"Azure work item '{workItemId}' is not in the cache for team member {teamMemberId}.");

            if (!int.TryParse(workItemId, out int workItemIdInt))
            {
                throw new ArgumentException($"Invalid Azure work item id '{workItemId}'.", nameof(workItemId));
            }

            string? projectIdText = projectId?.ToString();
            AzureItem next = EntityClone.AzureItem(item, projectId: projectIdText, setProjectId: true);
            TeamMember optimistic = EntityClone.TeamMember(
                member,
                azureItems: member.AzureItems.Select(a => a.Id == workItemId ? next : a).ToList());

            await OptimisticCache.ApplyAsync(
                member,
                optimistic,
                m => EntityClone.TeamMember(m),
                _cache.UpdateTeamMember,
                () => _api.AtlasApiEndpointsTeamMembersAzureWorkItemsSetAzureWorkItemProjectEndpointAsync(
                    teamMemberId,
                    workItemIdInt,
                    EntityRequestMappers.ToSetAzureWorkItemProjectRequest(projectId),
                    cancellationToken));
        }
    }
}
