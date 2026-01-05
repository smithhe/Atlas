namespace Atlas.Application.Features.Growth.FeedbackThemes.UpdateFeedbackTheme;

public sealed record UpdateFeedbackThemeCommand(
    Guid GrowthId,
    Guid ThemeId,
    string Title,
    string Description,
    string? ObservedSinceLabel) : IRequest<bool>;

