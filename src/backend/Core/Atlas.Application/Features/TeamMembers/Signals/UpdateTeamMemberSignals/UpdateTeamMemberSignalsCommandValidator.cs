namespace Atlas.Application.Features.TeamMembers.Signals.UpdateTeamMemberSignals;

public sealed class UpdateTeamMemberSignalsCommandValidator : AbstractValidator<UpdateTeamMemberSignalsCommand>
{
    public UpdateTeamMemberSignalsCommandValidator()
    {
        RuleFor(x => x.TeamMemberId).NotEmpty();
    }
}

