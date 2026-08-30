namespace Atlas.Ui.Models;

public sealed class AzureConnection
{
    public string? Organization { get; set; }
    public string? Project { get; set; }
    public string? AreaPath { get; set; }
    public string? TeamName { get; set; }
    public string? ProjectId { get; set; }
    public string? TeamId { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class AzureUpdateConnection
{
    public string Organization { get; set; } = "";
    public string? Project { get; set; }
    public string? AreaPath { get; set; }
    public string? TeamName { get; set; }
    public string? ProjectId { get; set; }
    public string? TeamId { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class AzureProject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class AzureTeam
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class AzureUser
{
    public string? DisplayName { get; set; }
    public string? UniqueName { get; set; }
    public string? Descriptor { get; set; }
}

public sealed class AzureTeamAreaPaths
{
    public string? DefaultValue { get; set; }
    public IReadOnlyList<AzureAreaPathValue> Values { get; set; } = Array.Empty<AzureAreaPathValue>();
}

public sealed class AzureAreaPathValue
{
    public string? Value { get; set; }
}

public sealed class AzureImportWorkItem
{
    public Guid Id { get; set; }
    public int? WorkItemId { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? State { get; set; }
    public string? WorkItemType { get; set; }
    public Guid? SuggestedTeamMemberId { get; set; }
}

public sealed class AzureSyncState
{
    public string? LastRunStatus { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? LastCompletedAtUtc { get; set; }
    public DateTimeOffset? LastAttemptedAtUtc { get; set; }
}

public sealed class AzureSyncResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public int? ItemsUpserted { get; set; }
}

public sealed class ImportProductOwnersResult
{
    public IReadOnlyList<ReusedProductOwnerName> ReusedProductOwnerNames { get; set; } = Array.Empty<ReusedProductOwnerName>();
}

public sealed class ReusedProductOwnerName
{
    public string DisplayName { get; set; } = "";
    public string AzureUniqueName { get; set; } = "";
}

public sealed class LinkAzureWorkItemsRequest
{
    public IReadOnlyList<Guid> AzureWorkItemIds { get; set; } = Array.Empty<Guid>();
    public Guid ProjectId { get; set; }
    public Guid? TeamMemberId { get; set; }
}
