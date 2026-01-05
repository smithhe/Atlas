namespace Atlas.Application.Features.Growth.Goals.Actions.AddGrowthGoalAction;

public sealed class AddGrowthGoalActionCommandValidator : AbstractValidator<AddGrowthGoalActionCommand>
{
    public AddGrowthGoalActionCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.GoalId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(5000);
        RuleFor(x => x.Evidence).MaximumLength(5000);
    }
}

