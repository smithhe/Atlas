namespace Atlas.Application.Features.Growth.Skills.SetGrowthSkillsInProgress;

public sealed class SetGrowthSkillsInProgressCommandValidator : AbstractValidator<SetGrowthSkillsInProgressCommand>
{
    public SetGrowthSkillsInProgressCommandValidator()
    {
        RuleFor(x => x.GrowthId).NotEmpty();

        RuleFor(x => x.SkillsInProgress)
            .NotNull();

        RuleForEach(x => x.SkillsInProgress)
            .NotEmpty()
            .MaximumLength(200);
    }
}

