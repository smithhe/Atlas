using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Persistence.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly AtlasDbContext _db;

    public TaskRepository(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Tasks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Tasks
            .Include(x => x.Project)
            .Include(x => x.Risk)
            .Include(x => x.BlockedBy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> ListAsync(IReadOnlyList<Guid>? ids = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Tasks.AsQueryable();

        if (ids is { Count: > 0 })
        {
            query = query.Where(x => ids.Contains(x.Id));
        }

        return await query
            .Include(x => x.Project)
            .Include(x => x.Risk)
            .Include(x => x.BlockedBy)
            .OrderByDescending(x => x.LastTouchedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Tasks.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetDirectBlockerIdsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _db.TaskDependencies
            .Where(d => d.DependentTaskId == taskId)
            .Select(d => d.BlockerTaskId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _db.Tasks.AddAsync(task, cancellationToken);
    }

    public void Remove(TaskItem task)
    {
        _db.Tasks.Remove(task);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

