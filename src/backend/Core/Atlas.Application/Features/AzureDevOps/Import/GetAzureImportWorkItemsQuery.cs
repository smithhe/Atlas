namespace Atlas.Application.Features.AzureDevOps.Import;

public sealed record AzureImportWorkItem(
    Guid Id,
    int WorkItemId,
    string Title,
    string State,
    string WorkItemType,
    string AreaPath,
    string IterationPath,
    DateTimeOffset ChangedDateUtc,
    string? AssignedToUniqueName,
    string Url,
    Guid? SuggestedTeamMemberId);

public sealed record GetAzureImportWorkItemsQuery(int Take = 200) : IRequest<IReadOnlyList<AzureImportWorkItem>>;
