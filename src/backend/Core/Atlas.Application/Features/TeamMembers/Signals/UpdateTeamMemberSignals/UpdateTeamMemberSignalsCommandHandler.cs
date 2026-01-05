using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.ValueObjects;

namespace Atlas.Application.Features.TeamMembers.Signals.UpdateTeamMemberSignals;

public sealed class UpdateTeamMemberSignalsCommandHandler : IRequestHandler<UpdateTeamMemberSignalsCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public UpdateTeamMemberSignalsCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateTeamMemberSignalsCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = await _team.GetByIdAsync(request.TeamMemberId, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        member.Signals = new TeamMemberSignals
        {
            Load = request.Load,
            Delivery = request.Delivery,
            SupportNeeded = request.SupportNeeded
        };

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

