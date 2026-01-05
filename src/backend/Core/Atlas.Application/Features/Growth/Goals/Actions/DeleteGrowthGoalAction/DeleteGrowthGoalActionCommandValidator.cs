namespace Atlas.Application.Features.Growth.Goals.Actions.DeleteGrowthGoalAction;

public sealed class DeleteGrowthGoalActionCommandValidator : AbstractValidator<DeleteGrowthGoalActionCommand>
{
    public DeleteGrowthGoalActionCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.GoalId).NotEmpty();
        RuleFor(x => x.ActionId).NotEmpty();
    }
}

