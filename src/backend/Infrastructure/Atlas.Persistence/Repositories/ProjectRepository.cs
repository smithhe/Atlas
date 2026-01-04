using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly AtlasDbContext _db;

    public ProjectRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Project?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Projects
            .Include(x => x.ProductOwner)
            .Include(x => x.Tags)
            .Include(x => x.Links)
            .Include(x => x.TeamMembers)
            .Include(x => x.Tasks)
            .Include(x => x.Risks)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Projects
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _db.Projects.AddAsync(project, cancellationToken);
    }

    public void Remove(Project project)
    {
        _db.Projects.Remove(project);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

