namespace Atlas.Application.Features.Growth.Goals.CheckIns.UpdateGrowthGoalCheckIn;

public sealed class UpdateGrowthGoalCheckInCommandValidator : AbstractValidator<UpdateGrowthGoalCheckInCommand>
{
    public UpdateGrowthGoalCheckInCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.GoalId).NotEmpty();
        RuleFor(x => x.CheckInId).NotEmpty();
        RuleFor(x => x.Note).NotEmpty().MaximumLength(5000);
    }
}

