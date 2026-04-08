using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.UpdateTeamMember;

public sealed class UpdateTeamMemberCommandHandler : IRequestHandler<UpdateTeamMemberCommand, bool>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public UpdateTeamMemberCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateTeamMemberCommand request, CancellationToken cancellationToken)
    {
        await using IUnitOfWorkTransaction tx = await _uow.BeginTransactionAsync(cancellationToken);

        TeamMember? member = await _team.GetByIdAsync(request.Id, cancellationToken);
        if (member is null)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        member.Name = request.Name;
        member.Role = request.Role ?? string.Empty;
        member.StatusDot = request.StatusDot;

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

