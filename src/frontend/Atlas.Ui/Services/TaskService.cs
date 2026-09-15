using System.Collections.Concurrent;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Contracts;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services
{

    /// <summary>Task mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
    public sealed class TaskService : ITaskService
    {
        private readonly IAtlasApiClient _api;
        private readonly IAppCacheService _cache;
        private readonly ConcurrentDictionary<Guid, EntityAutosaveCoordinator<AtlasTask>> _coordinators = new();

        public event Action<Guid>? SaveStateChanged;

        public TaskService(IAtlasApiClient api, IAppCacheService cache)
        {
            _api = api;
            _cache = cache;
        }

        public EntitySaveState GetSaveState(Guid taskId) =>
            _coordinators.TryGetValue(taskId, out EntityAutosaveCoordinator<AtlasTask>? coordinator)
                ? coordinator.State
                : EntitySaveState.Idle;

        public async Task<AtlasTask> CreateAsync(AtlasTask draft, CancellationToken cancellationToken = default)
        {
            AtlasApiDTOsTasksCreateTaskResponse res =
                await _api.AtlasApiEndpointsTasksCreateTaskEndpointAsync(
                    EntityRequestMappers.ToCreateTaskRequest(draft),
                    cancellationToken);
            draft.Id = res.Id ?? Guid.Empty;
            _cache.AddTask(draft);
            return draft;
        }

        public Task UpdateAsync(Guid taskId, Func<AtlasTask, AtlasTask> edit, bool debounce = false, CancellationToken cancellationToken = default)
        {
            _ = _cache.TryGetTask(taskId)
                ?? throw new InvalidOperationException($"Task {taskId} is not in the cache.");
            EntityAutosaveCoordinator<AtlasTask> coordinator = GetCoordinator(taskId);
            return coordinator.SaveAsync(
                edit,
                () => _cache.TryGetTask(taskId),
                _cache.UpdateTask,
                ct => PersistAsync(taskId, ct),
                debounce,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            await _api.AtlasApiEndpointsTasksDeleteTaskEndpointAsync(taskId, cancellationToken);
            _cache.RemoveTask(taskId);
            _coordinators.TryRemove(taskId, out _);
        }

        private EntityAutosaveCoordinator<AtlasTask> GetCoordinator(Guid taskId) =>
            _coordinators.GetOrAdd(taskId, _ =>
            {
                EntityAutosaveCoordinator<AtlasTask> coordinator = new();
                coordinator.StateChanged += () => SaveStateChanged?.Invoke(taskId);
                return coordinator;
            });

        private Task PersistAsync(Guid taskId, CancellationToken cancellationToken)
        {
            AtlasTask task = _cache.TryGetTask(taskId)
                ?? throw new InvalidOperationException($"Task {taskId} is not in the cache.");
            return _api.AtlasApiEndpointsTasksUpdateTaskEndpointAsync(
                task.Id,
                EntityRequestMappers.ToUpdateTaskRequest(task),
                cancellationToken);
        }
    }
}
