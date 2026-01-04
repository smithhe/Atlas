namespace Atlas.Domain.Entities;

/// <summary>
/// Row entity for a growth plan's "skills in progress" list (clean SQL: GrowthId + Value).
/// </summary>
public sealed class GrowthSkillInProgress
{
    public Guid GrowthId { get; set; }
    public Growth? Growth { get; set; }

    public string Value { get; set; } = string.Empty;
}

