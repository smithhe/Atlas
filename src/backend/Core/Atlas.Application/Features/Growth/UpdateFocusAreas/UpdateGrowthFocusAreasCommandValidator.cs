namespace Atlas.Application.Features.Growth.UpdateFocusAreas;

public sealed class UpdateGrowthFocusAreasCommandValidator : AbstractValidator<UpdateGrowthFocusAreasCommand>
{
    public UpdateGrowthFocusAreasCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.FocusAreasMarkdown).NotNull()
            .MaximumLength(10000);;
    }
}

