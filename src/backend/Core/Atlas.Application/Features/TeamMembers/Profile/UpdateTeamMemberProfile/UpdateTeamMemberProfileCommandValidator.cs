namespace Atlas.Application.Features.TeamMembers.Profile.UpdateTeamMemberProfile;

public sealed class UpdateTeamMemberProfileCommandValidator : AbstractValidator<UpdateTeamMemberProfileCommand>
{
    public UpdateTeamMemberProfileCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();

        RuleFor(x => x.TimeZone)
            .MaximumLength(50);

        RuleFor(x => x.TypicalHours)
            .MaximumLength(100);
    }
}

