namespace Atlas.Domain.Entities;

/// <summary>
/// Row entity for a project's tag list (clean SQL: ProjectId + Value).
/// </summary>
public sealed class ProjectTag
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Value { get; set; } = string.Empty;
}

