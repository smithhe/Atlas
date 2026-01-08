namespace Atlas.Api.DTOs.Growth;

public sealed record GrowthDto(
    Guid Id,
    Guid TeamMemberId,
    IReadOnlyList<GrowthGoalDto> Goals,
    IReadOnlyList<string> SkillsInProgress,
    IReadOnlyList<GrowthFeedbackThemeDto> FeedbackThemes,
    string FocusAreasMarkdown);

