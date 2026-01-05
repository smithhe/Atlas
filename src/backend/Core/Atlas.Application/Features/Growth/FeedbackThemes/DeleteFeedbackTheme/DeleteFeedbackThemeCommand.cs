namespace Atlas.Application.Features.Growth.FeedbackThemes.DeleteFeedbackTheme;

public sealed record DeleteFeedbackThemeCommand(Guid GrowthId, Guid ThemeId) : IRequest<bool>;

