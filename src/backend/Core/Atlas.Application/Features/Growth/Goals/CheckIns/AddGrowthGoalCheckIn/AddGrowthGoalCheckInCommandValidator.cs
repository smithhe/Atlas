namespace Atlas.Application.Features.Growth.Goals.CheckIns.AddGrowthGoalCheckIn;

public sealed class AddGrowthGoalCheckInCommandValidator : AbstractValidator<AddGrowthGoalCheckInCommand>
{
    public AddGrowthGoalCheckInCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();
        RuleFor(x => x.GoalId).NotEmpty();
        RuleFor(x => x.Note).NotEmpty().MaximumLength(5000);
    }
}

