using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class TeamMemberRepository : ITeamMemberRepository
{
    private readonly AtlasDbContext _db;

    public TeamMemberRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<TeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.TeamMembers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<TeamMember?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.TeamMembers
            .Include(x => x.Notes)
            .Include(x => x.Risks)
            .Include(x => x.Projects)
            .Include(x => x.LinkedRisks)
            .Include(x => x.AzureWorkItemLinks)
                .ThenInclude(x => x.AzureWorkItem)
            .Include(x => x.AzureWorkItemLocalNotes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamMember>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.TeamMembers
            .Include(x => x.Notes)
            .Include(x => x.Risks)
            .Include(x => x.Projects)
            .Include(x => x.LinkedRisks)
            .Include(x => x.AzureWorkItemLinks)
                .ThenInclude(x => x.AzureWorkItem)
            .Include(x => x.AzureWorkItemLocalNotes)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.TeamMembers.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(TeamMember member, CancellationToken cancellationToken = default)
    {
        await _db.TeamMembers.AddAsync(member, cancellationToken);
    }

    public Task AddNoteAsync(TeamNote note, CancellationToken cancellationToken = default) =>
        _db.TeamNotes.AddAsync(note, cancellationToken).AsTask();

    public Task AddRiskAsync(TeamMemberRisk risk, CancellationToken cancellationToken = default) =>
        _db.TeamMemberRisks.AddAsync(risk, cancellationToken).AsTask();

    public Task AddAzureWorkItemLocalNoteAsync(AzureWorkItemLocalNote note, CancellationToken cancellationToken = default) =>
        _db.AzureWorkItemLocalNotes.AddAsync(note, cancellationToken).AsTask();

    public void Remove(TeamMember member)
    {
        _db.TeamMembers.Remove(member);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

