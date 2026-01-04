using Atlas.Domain.Abstractions;

namespace Atlas.Domain.Entities;

public sealed class Growth : AggregateRoot
{
    public Guid TeamMemberId { get; set; }

    public List<GrowthGoal> Goals { get; set; } = [];

    public List<GrowthSkillInProgress> SkillsInProgress { get; set; } = [];
    public List<GrowthFeedbackTheme> FeedbackThemes { get; set; } = [];

    public string FocusAreasMarkdown { get; set; } = string.Empty;
}

