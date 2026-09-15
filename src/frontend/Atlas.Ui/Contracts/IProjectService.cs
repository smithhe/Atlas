using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface IProjectService
    {
        Task<Project> CreateAsync(Project draft, CancellationToken cancellationToken = default);

        Task UpdateAsync(Project project, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default);
    }
}
