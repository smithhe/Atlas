namespace Atlas.Api.DTOs.Growth.FeedbackThemes;

public sealed record AddFeedbackThemeRequest(
    Guid GrowthId,
    string Title,
    string Description,
    string? ObservedSinceLabel);

