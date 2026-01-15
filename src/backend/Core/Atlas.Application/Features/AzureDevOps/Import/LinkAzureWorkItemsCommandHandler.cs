using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Abstractions.Time;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.AzureDevOps.Import;

public sealed class LinkAzureWorkItemsCommandHandler : IRequestHandler<LinkAzureWorkItemsCommand, int>
{
    private readonly IAzureWorkItemLinkRepository _links;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public LinkAzureWorkItemsCommandHandler(IAzureWorkItemLinkRepository links, IUnitOfWork uow, IDateTimeProvider clock)
    {
        _links = links;
        _uow = uow;
        _clock = clock;
    }

    public async Task<int> Handle(LinkAzureWorkItemsCommand request, CancellationToken cancellationToken)
    {
        if (request.AzureWorkItemIds.Count == 0) return 0;

        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var existing = await _links.GetByWorkItemIdsAsync(request.AzureWorkItemIds, cancellationToken);
        var existingById = existing.ToDictionary(x => x.AzureWorkItemId);

        var updated = 0;
        foreach (var workItemId in request.AzureWorkItemIds.Distinct())
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
            link.TeamMemberId = request.TeamMemberId;
            link.LinkedAtUtc = _clock.UtcNow;
            updated++;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return updated;
    }
}
