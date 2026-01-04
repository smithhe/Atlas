using Atlas.Domain.Entities;

namespace Atlas.Application.Abstractions.Persistence;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    void Remove(Project project);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

