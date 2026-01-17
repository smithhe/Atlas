namespace Atlas.Application.Abstractions.AzureDevOps;

public sealed record AzureProjectSummary(string Id, string Name);
public sealed record AzureTeamSummary(string Id, string Name);
public sealed record AzureUserSummary(string DisplayName, string UniqueName, string? Descriptor);

public sealed record AzureWorkItemDetails(
    int Id,
    int Rev,
    DateTimeOffset ChangedDateUtc,
    string Title,
    string State,
    string WorkItemType,
    string AreaPath,
    string IterationPath,
    string? AssignedToUniqueName,
    string Url);
