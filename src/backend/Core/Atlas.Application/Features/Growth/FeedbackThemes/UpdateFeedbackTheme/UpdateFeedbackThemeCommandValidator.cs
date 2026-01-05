namespace Atlas.Application.Features.Growth.FeedbackThemes.UpdateFeedbackTheme;

public sealed class UpdateFeedbackThemeCommandValidator : AbstractValidator<UpdateFeedbackThemeCommand>
{
    public UpdateFeedbackThemeCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.ThemeId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(2000);
        RuleFor(x => x.ObservedSinceLabel).MaximumLength(200);
    }
}

