namespace Atlas.Application.Features.TeamMembers.CreateTeamMember;

public sealed class CreateTeamMemberCommandValidator : AbstractValidator<CreateTeamMemberCommand>
{
    public CreateTeamMemberCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Role)
            .MaximumLength(100);
    }
}

