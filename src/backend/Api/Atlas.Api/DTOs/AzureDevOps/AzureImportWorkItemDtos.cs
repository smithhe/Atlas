namespace Atlas.Api.DTOs.AzureDevOps;

public sealed record AzureImportWorkItemDto(
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

public sealed record LinkAzureWorkItemsRequest(
    IReadOnlyList<Guid> AzureWorkItemIds,
    Guid ProjectId,
    Guid? TeamMemberId);
