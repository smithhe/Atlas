namespace Atlas.Application.Features.Growth.Goals.UpdateGrowthGoal;

public sealed class UpdateGrowthGoalCommandValidator : AbstractValidator<UpdateGrowthGoalCommand>
{
    public UpdateGrowthGoalCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.GoalId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(5000);
        RuleFor(x => x.Category).MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(5000);
        RuleFor(x => x.SuccessCriteria).MaximumLength(5000);

        RuleFor(x => x.ProgressPercent)
            .InclusiveBetween(0, 100)
            .When(x => x.ProgressPercent is not null);

        RuleFor(x => x.TargetDate)
            .Must((cmd, target) => target is null || cmd.StartDate is null || target >= cmd.StartDate)
            .WithMessage("TargetDate must be on or after StartDate.");
    }
}

