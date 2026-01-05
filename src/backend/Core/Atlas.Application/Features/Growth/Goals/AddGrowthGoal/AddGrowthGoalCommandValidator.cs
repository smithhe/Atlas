namespace Atlas.Application.Features.Growth.Goals.AddGrowthGoal;

public sealed class AddGrowthGoalCommandValidator : AbstractValidator<AddGrowthGoalCommand>
{
    public AddGrowthGoalCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(5000);
        RuleFor(x => x.Category).MaximumLength(200);

        RuleFor(x => x.TargetDate)
            .Must((cmd, target) => target is null || cmd.StartDate is null || target >= cmd.StartDate)
            .WithMessage("TargetDate must be on or after StartDate.");
    }
}

