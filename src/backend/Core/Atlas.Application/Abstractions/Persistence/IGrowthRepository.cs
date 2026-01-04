using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IGrowthRepository
{
    Task<Growth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Growth?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Growth?> GetByTeamMemberIdAsync(Guid teamMemberId, CancellationToken cancellationToken = default);
    Task<Growth?> GetByTeamMemberIdWithDetailsAsync(Guid teamMemberId, CancellationToken cancellationToken = default);

    Task AddAsync(Growth growth, CancellationToken cancellationToken = default);
    void Remove(Growth growth);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

