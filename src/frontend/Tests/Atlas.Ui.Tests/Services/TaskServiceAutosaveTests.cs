using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;
using FluentAssertions;
using Moq;

namespace Atlas.Ui.Tests.Services;

public sealed class TaskServiceAutosaveTests
{
    [Fact]
    public async Task UpdateAsync_WhenDebouncedNotesThenImmediateStatusFromCache_PreservesBothChanges()
    {
        var taskId = Guid.NewGuid();
        AtlasTask initial = new()
        {
            Id = taskId,
            Title = "Original",
            Notes = "initial",
            Status = Models.TaskStatus.NotStarted,
            Priority = Priority.Medium
        };

        List<AtlasApiDTOsTasksUpdateTaskRequest> persisted = [];
        Mock<IAtlasApiClient> api = new();
        api.Setup(x => x.AtlasApiEndpointsTasksUpdateTaskEndpointAsync(
                taskId,
                It.IsAny<AtlasApiDTOsTasksUpdateTaskRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AtlasApiDTOsTasksUpdateTaskRequest, CancellationToken>((_, req, _) => persisted.Add(req))
            .Returns(Task.CompletedTask);

        AppCacheService cache = AutosaveTestSupport.CreateCache(api.Object);
        cache.AddTask(initial);
        TaskService service = new(api.Object, cache);

        Task debouncedNotes = service.UpdateAsync(
            taskId,
            t => EntityClone.Task(t, notes: "typed notes"),
            debounce: true);

        await service.UpdateAsync(
            taskId,
            t => EntityClone.Task(t, status: Models.TaskStatus.InProgress, setStatus: true));

        await debouncedNotes;

        AtlasTask cached = cache.TryGetTask(taskId)!;
        cached.Notes.Should().Be("typed notes");
        cached.Status.Should().Be(Models.TaskStatus.InProgress);
        persisted.Last().Notes.Should().Be("typed notes");
        persisted.Last().Status.Should().Be(AtlasDomainEnumsTaskStatus.InProgress);
        service.GetSaveState(taskId).Should().Be(EntitySaveState.Saved);
    }

    [Fact]
    public async Task UpdateAsync_WhenDebouncedTitleThenImmediatePriorityFromCache_PreservesBothChanges()
    {
        var taskId = Guid.NewGuid();
        AtlasTask initial = new()
        {
            Id = taskId,
            Title = "Original",
            Notes = "",
            Status = Models.TaskStatus.NotStarted,
            Priority = Priority.Low
        };

        List<AtlasApiDTOsTasksUpdateTaskRequest> persisted = [];
        Mock<IAtlasApiClient> api = new();
        api.Setup(x => x.AtlasApiEndpointsTasksUpdateTaskEndpointAsync(
                taskId,
                It.IsAny<AtlasApiDTOsTasksUpdateTaskRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, AtlasApiDTOsTasksUpdateTaskRequest, CancellationToken>((_, req, _) => persisted.Add(req))
            .Returns(Task.CompletedTask);

        AppCacheService cache = AutosaveTestSupport.CreateCache(api.Object);
        cache.AddTask(initial);
        TaskService service = new(api.Object, cache);

        Task debouncedTitle = service.UpdateAsync(
            taskId,
            t => EntityClone.Task(t, title: "Renamed task"),
            debounce: true);

        await service.UpdateAsync(
            taskId,
            t => EntityClone.Task(t, priority: Priority.High));

        await debouncedTitle;

        AtlasTask cached = cache.TryGetTask(taskId)!;
        cached.Title.Should().Be("Renamed task");
        cached.Priority.Should().Be(Priority.High);
        persisted.Last().Title.Should().Be("Renamed task");
        persisted.Last().Priority.Should().Be(AtlasDomainEnumsPriority.High);
    }
}
