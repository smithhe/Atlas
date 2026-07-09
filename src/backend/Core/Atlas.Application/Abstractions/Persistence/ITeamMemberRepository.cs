using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface ITeamMemberRepository
{
    Task<TeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TeamMember?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamMember>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TeamMember member, CancellationToken cancellationToken = default);
    Task AddNoteAsync(TeamNote note, CancellationToken cancellationToken = default);
    Task AddRiskAsync(TeamMemberRisk risk, CancellationToken cancellationToken = default);
    Task AddAzureWorkItemLocalNoteAsync(AzureWorkItemLocalNote note, CancellationToken cancellationToken = default);
    void Remove(TeamMember member);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

