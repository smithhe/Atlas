using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface ITeamMemberRepository
{
    Task<TeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamMember?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TeamMember member, CancellationToken cancellationToken = default);
    void Remove(TeamMember member);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

