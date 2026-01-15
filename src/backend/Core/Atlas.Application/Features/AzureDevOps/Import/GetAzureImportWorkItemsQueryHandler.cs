using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.AzureDevOps.Import;

public sealed class GetAzureImportWorkItemsQueryHandler : IRequestHandler<GetAzureImportWorkItemsQuery, IReadOnlyList<AzureImportWorkItem>>
{
    private readonly IAzureConnectionRepository _connections;
    private readonly IAzureWorkItemRepository _workItems;
    private readonly IAzureUserMappingRepository _mappings;

    public GetAzureImportWorkItemsQueryHandler(
        IAzureConnectionRepository connections,
        IAzureWorkItemRepository workItems,
        IAzureUserMappingRepository mappings)
    {
        _connections = connections;
        _workItems = workItems;
        _mappings = mappings;
    }

    public async Task<IReadOnlyList<AzureImportWorkItem>> Handle(GetAzureImportWorkItemsQuery request, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetSingletonAsync(cancellationToken);
        if (connection is null) return [];

        var items = await _workItems.ListUnlinkedAsync(connection.Id, request.Take, cancellationToken);
        if (items.Count == 0) return [];

        var assigned = items
            .Select(x => NormalizeUniqueName(x.AssignedToUniqueName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var mappings = await _mappings.GetByUniqueNamesAsync(assigned, cancellationToken);
        var mapByUnique = mappings.ToDictionary(x => x.AzureUniqueName, StringComparer.OrdinalIgnoreCase);

        return items.Select(x =>
        {
            var assignedTo = NormalizeUniqueName(x.AssignedToUniqueName);
            mapByUnique.TryGetValue(assignedTo, out var mapping);

            return new AzureImportWorkItem(
                x.Id,
                x.WorkItemId,
                x.Title,
                x.State,
                x.WorkItemType,
                x.AreaPath,
                x.IterationPath,
                x.ChangedDateUtc,
                x.AssignedToUniqueName,
                x.Url,
                mapping?.TeamMemberId);
        }).ToList();
    }

    private static string NormalizeUniqueName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
