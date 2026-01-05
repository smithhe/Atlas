namespace Atlas.Application.Features.Growth.FeedbackThemes.AddFeedbackTheme;

public sealed class AddFeedbackThemeCommandValidator : AbstractValidator<AddFeedbackThemeCommand>
{
    public AddFeedbackThemeCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(2000);
        RuleFor(x => x.ObservedSinceLabel).MaximumLength(200);
    }
}

