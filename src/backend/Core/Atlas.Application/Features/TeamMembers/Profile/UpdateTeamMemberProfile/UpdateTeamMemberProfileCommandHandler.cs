using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.ValueObjects;

namespace Atlas.Application.Features.TeamMembers.Profile.UpdateTeamMemberProfile;

public sealed class UpdateTeamMemberProfileCommandHandler : IRequestHandler<UpdateTeamMemberProfileCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public UpdateTeamMemberProfileCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateTeamMemberProfileCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        var timeZone = request.TimeZone?.Trim();
        if (string.IsNullOrWhiteSpace(timeZone)) timeZone = null;

        var typicalHours = request.TypicalHours?.Trim();
        if (string.IsNullOrWhiteSpace(typicalHours)) typicalHours = null;

        member.Profile = new TeamMemberProfile
        {
            TimeZone = timeZone,
            TypicalHours = typicalHours
        };

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

