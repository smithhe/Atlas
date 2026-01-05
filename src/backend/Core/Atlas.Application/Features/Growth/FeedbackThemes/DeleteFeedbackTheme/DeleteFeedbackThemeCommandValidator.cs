namespace Atlas.Application.Features.Growth.FeedbackThemes.DeleteFeedbackTheme;

public sealed class DeleteFeedbackThemeCommandValidator : AbstractValidator<DeleteFeedbackThemeCommand>
{
    public DeleteFeedbackThemeCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.ThemeId).NotEmpty();
    }
}

