namespace Atlas.Application.Features.Growth.FeedbackThemes.AddFeedbackTheme;

public sealed record AddFeedbackThemeCommand(
    Guid GrowthId,
    string Title,
    string Description,
    string? ObservedSinceLabel) : IRequest<Guid>;

