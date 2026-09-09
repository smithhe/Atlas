using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;

namespace Atlas.Ui.Services;

/// <summary>Task mutations. Pages talk to this instead of <see cref="IAtlasApiClient"/>.</summary>
public sealed class TaskService
{
    private readonly IAtlasApiClient _api;
    private readonly AppCacheService _cache;

    public TaskService(IAtlasApiClient api, AppCacheService cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<AtlasTask> CreateAsync(AtlasTask draft, CancellationToken cancellationToken = default)
    {
        AtlasApiDTOsTasksCreateTaskResponse res =
            await _api.AtlasApiEndpointsTasksCreateTaskEndpointAsync(
                EntityRequestMappers.ToCreateTaskRequest(draft, _cache.Projects, _cache.Risks),
                cancellationToken);
        draft.Id = res.Id ?? Guid.Empty;
        _cache.AddTask(draft);
        return draft;
    }

    public Task UpdateAsync(AtlasTask task, CancellationToken cancellationToken = default)
    {
        AtlasTask? previous = _cache.Tasks.FirstOrDefault(t => t.Id == task.Id);
        return OptimisticCache.ApplyAsync(
            previous,
            task,
            t => EntityClone.Task(t),
            _cache.UpdateTask,
            () => _api.AtlasApiEndpointsTasksUpdateTaskEndpointAsync(
                task.Id,
                EntityRequestMappers.ToUpdateTaskRequest(task, _cache.Projects, _cache.Risks),
                cancellationToken));
    }

    public async Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        await _api.AtlasApiEndpointsTasksDeleteTaskEndpointAsync(taskId, cancellationToken);
        _cache.RemoveTask(taskId);
    }
}
