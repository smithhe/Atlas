namespace Atlas.Domain.Entities;

/// <summary>
/// Row entity for a growth plan's "skills in progress" list (clean SQL: GrowthId + Value).
/// </summary>
public sealed class GrowthSkillInProgress
{
    public Guid GrowthId { get; set; }
    public Growth? Growth { get; set; }

    /// <summary>
    /// UI-driven ordering (0-based). The UI edits skills as an ordered list.
    /// </summary>
    public int SortOrder { get; set; }

    public string Value { get; set; } = string.Empty;
}

