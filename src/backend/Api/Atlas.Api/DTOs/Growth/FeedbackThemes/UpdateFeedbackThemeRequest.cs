namespace Atlas.Api.DTOs.Growth.FeedbackThemes;

public sealed record UpdateFeedbackThemeRequest(
    Guid GrowthId,
    Guid ThemeId,
    string Title,
    string Description,
    string? ObservedSinceLabel);

