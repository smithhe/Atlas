namespace Atlas.Application.Features.Growth.EnsureGrowthForTeamMember;

public sealed class EnsureGrowthForTeamMemberCommandValidator : AbstractValidator<EnsureGrowthForTeamMemberCommand>
{
    public EnsureGrowthForTeamMemberCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
    }
}

