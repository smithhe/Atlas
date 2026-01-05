using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.TeamMembers.CreateTeamMember;

public sealed class CreateTeamMemberCommandHandler : IRequestHandler<CreateTeamMemberCommand, Guid>
{
    private readonly ITeamMemberRepository _team;
    private readonly IUnitOfWork _uow;

    public CreateTeamMemberCommandHandler(ITeamMemberRepository team, IUnitOfWork uow)
    {
        _team = team;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateTeamMemberCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Role = request.Role ?? string.Empty,
            StatusDot = request.StatusDot,
            CurrentFocus = string.Empty
        };

        await _team.AddAsync(member, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return member.Id;
    }
}

