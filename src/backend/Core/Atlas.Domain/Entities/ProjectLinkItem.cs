namespace Atlas.Domain.Entities;

/// <summary>
/// Row entity for a project's link list (clean SQL: ProjectId + Label + Url).
/// </summary>
public sealed class ProjectLinkItem
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

