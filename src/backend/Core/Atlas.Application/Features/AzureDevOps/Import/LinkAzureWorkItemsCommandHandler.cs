using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Abstractions.Time;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.Import;

public sealed class LinkAzureWorkItemsCommandHandler : IRequestHandler<LinkAzureWorkItemsCommand, int>
{
    private readonly IAzureWorkItemLinkRepository _links;
    private readonly IAzureWorkItemRepository _workItems;
    private readonly IAzureUserMappingRepository _mappings;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public LinkAzureWorkItemsCommandHandler(
        IAzureWorkItemLinkRepository links,
        IAzureWorkItemRepository workItems,
        IAzureUserMappingRepository mappings,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _links = links;
        _workItems = workItems;
        _mappings = mappings;
        _uow = uow;
        _clock = clock;
    }

    public async Task<int> Handle(LinkAzureWorkItemsCommand request, CancellationToken cancellationToken)
    {
        if (request.AzureWorkItemIds.Count == 0) return 0;

        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var existing = await _links.GetByWorkItemIdsAsync(request.AzureWorkItemIds, cancellationToken);
        var existingById = existing.ToDictionary(x => x.AzureWorkItemId);

        var workItemIds = request.AzureWorkItemIds.Distinct().ToList();
        var mappedTeamMemberByWorkItemId = new Dictionary<Guid, Guid?>();
        if (!request.TeamMemberId.HasValue)
        {
            var workItems = await _workItems.GetByIdsAsync(workItemIds, cancellationToken);
            var assignedUniqueNames = workItems
                .Select(x => NormalizeUniqueName(x.AssignedToUniqueName))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var mappings = await _mappings.GetByUniqueNamesAsync(assignedUniqueNames, cancellationToken);
            var mapByUnique = mappings.ToDictionary(x => x.AzureUniqueName, StringComparer.OrdinalIgnoreCase);

            foreach (var workItem in workItems)
            {
                var assignedTo = NormalizeUniqueName(workItem.AssignedToUniqueName);
                mapByUnique.TryGetValue(assignedTo, out var mapping);
                mappedTeamMemberByWorkItemId[workItem.Id] = mapping?.TeamMemberId;
            }
        }

        var updated = 0;
        foreach (var workItemId in workItemIds)
        {
            if (!existingById.TryGetValue(workItemId, out var link))
            {
                link = new AzureWorkItemLink
                {
                    Id = Guid.NewGuid(),
                    AzureWorkItemId = workItemId,
                    LinkedAtUtc = _clock.UtcNow
                };
                await _links.AddAsync(link, cancellationToken);
            }

            link.ProjectId = request.ProjectId;
            link.TeamMemberId = request.TeamMemberId ?? mappedTeamMemberByWorkItemId.GetValueOrDefault(workItemId);
            link.LinkedAtUtc = _clock.UtcNow;
            updated++;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return updated;
    }

    private static string NormalizeUniqueName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
