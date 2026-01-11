namespace Atlas.Application.Features.Projects.TeamMembers.SetProjectTeamMembers;

public sealed class SetProjectTeamMembersCommandValidator : AbstractValidator<SetProjectTeamMembersCommand>
{
    public SetProjectTeamMembersCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TeamMemberIds).NotNull();
    }
}

