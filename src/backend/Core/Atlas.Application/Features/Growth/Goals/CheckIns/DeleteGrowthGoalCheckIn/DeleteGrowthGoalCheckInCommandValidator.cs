namespace Atlas.Application.Features.Growth.Goals.CheckIns.DeleteGrowthGoalCheckIn;

public sealed class DeleteGrowthGoalCheckInCommandValidator : AbstractValidator<DeleteGrowthGoalCheckInCommand>
{
    public DeleteGrowthGoalCheckInCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.GoalId).NotEmpty();
        RuleFor(x => x.CheckInId).NotEmpty();
    }
}

