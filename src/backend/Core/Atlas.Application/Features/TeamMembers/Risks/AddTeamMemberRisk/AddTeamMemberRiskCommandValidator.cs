namespace Atlas.Application.Features.TeamMembers.Risks.AddTeamMemberRisk;

public sealed class AddTeamMemberRiskCommandValidator : AbstractValidator<AddTeamMemberRiskCommand>
{
    public AddTeamMemberRiskCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.RiskType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ImpactArea)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(20000);

        RuleFor(x => x.CurrentAction)
            .NotEmpty()
            .MaximumLength(20000);
    }
}

