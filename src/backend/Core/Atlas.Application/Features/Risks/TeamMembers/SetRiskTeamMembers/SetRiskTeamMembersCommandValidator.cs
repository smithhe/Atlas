namespace Atlas.Application.Features.Risks.TeamMembers.SetRiskTeamMembers;

public sealed class SetRiskTeamMembersCommandValidator : AbstractValidator<SetRiskTeamMembersCommand>
{
    public SetRiskTeamMembersCommandValidator()
    {
        RuleFor(x => x.RiskId).NotEmpty();
    }
}
