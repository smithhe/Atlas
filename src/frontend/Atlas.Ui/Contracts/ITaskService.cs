using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Contracts
{
    public interface ITaskService
    {
        event Action<Guid>? SaveStateChanged;

        EntitySaveState GetSaveState(Guid taskId);

        Task<AtlasTask> CreateAsync(AtlasTask draft, CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Guid taskId,
            Func<AtlasTask, AtlasTask> edit,
            bool debounce = false,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default);
    }
}
