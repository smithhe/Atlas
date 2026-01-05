namespace Atlas.Application.Features.Growth.Goals.DeleteGrowthGoal;

public sealed class DeleteGrowthGoalCommandValidator : AbstractValidator<DeleteGrowthGoalCommand>
{
    public DeleteGrowthGoalCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.GoalId).NotEmpty();
    }
}

