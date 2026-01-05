namespace Atlas.Application.Features.TeamMembers.Risks.DeleteTeamMemberRisk;

public sealed class DeleteTeamMemberRiskCommandValidator : AbstractValidator<DeleteTeamMemberRiskCommand>
{
    public DeleteTeamMemberRiskCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
        RuleFor(x => x.TeamMemberRiskId).NotEmpty();
    }
}

