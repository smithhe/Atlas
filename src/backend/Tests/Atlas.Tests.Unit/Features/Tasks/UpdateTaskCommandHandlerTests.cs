using Atlas.Application.Features.Tasks.UpdateTask;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.Tasks;

public sealed class UpdateTaskCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTaskMissing_ReturnsFalse()
    {
        var handler = new UpdateTaskCommandHandler(new FakeTaskRepository(), new FakeUnitOfWork());
        bool ok = await handler.Handle(ValidUpdate(Guid.NewGuid()), CancellationToken.None);
        Assert.False(ok);
    }

    [Fact]
    public async Task Handle_WhenDependencyCycleDetected_Throws()
    {
        var tasks = new FakeTaskRepository();
        Guid taskA = Guid.NewGuid();
        Guid taskB = Guid.NewGuid();

        var a = NewTask(taskA, "A");
        var b = NewTask(taskB, "B");
        a.BlockedBy.Add(new TaskDependency { Id = Guid.NewGuid(), DependentTaskId = taskA, BlockerTaskId = taskB });
        tasks.Seed(a);
        tasks.Seed(b);

        var handler = new UpdateTaskCommandHandler(tasks, new FakeUnitOfWork());
        UpdateTaskCommand command = ValidUpdate(taskB) with { BlockedByTaskIds = [taskA] };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesTask()
    {
        var tasks = new FakeTaskRepository();
        Guid id = Guid.NewGuid();
        tasks.Seed(NewTask(id, "Before"));

        var handler = new UpdateTaskCommandHandler(tasks, new FakeUnitOfWork());
        bool ok = await handler.Handle(ValidUpdate(id) with { Title = "After" }, CancellationToken.None);

        Assert.True(ok);
        TaskItem? updated = await tasks.GetByIdAsync(id, CancellationToken.None);
        Assert.Equal("After", updated?.Title);
    }

    [Fact]
    public async Task Handle_WhenRemovingBlockers_ClearsDependencies()
    {
        var tasks = new FakeTaskRepository();
        Guid taskId = Guid.NewGuid();
        Guid blockerId = Guid.NewGuid();
        var task = NewTask(taskId, "Dependent");
        task.BlockedBy.Add(new TaskDependency
        {
            Id = Guid.NewGuid(),
            DependentTaskId = taskId,
            BlockerTaskId = blockerId
        });
        tasks.Seed(task);
        tasks.Seed(NewTask(blockerId, "Blocker"));

        var handler = new UpdateTaskCommandHandler(tasks, new FakeUnitOfWork());
        bool ok = await handler.Handle(ValidUpdate(taskId) with { BlockedByTaskIds = [] }, CancellationToken.None);

        Assert.True(ok);
        TaskItem? updated = await tasks.GetByIdAsync(taskId, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Empty(updated.BlockedBy);
    }

    [Fact]
    public async Task Handle_WhenBlockerMissingOnUpdate_Throws()
    {
        var tasks = new FakeTaskRepository();
        Guid id = Guid.NewGuid();
        tasks.Seed(NewTask(id, "Task"));

        var handler = new UpdateTaskCommandHandler(tasks, new FakeUnitOfWork());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(ValidUpdate(id) with { BlockedByTaskIds = [Guid.NewGuid()] }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_IgnoresSelfDependency()
    {
        var tasks = new FakeTaskRepository();
        Guid id = Guid.NewGuid();
        tasks.Seed(NewTask(id, "Task"));

        var handler = new UpdateTaskCommandHandler(tasks, new FakeUnitOfWork());
        bool ok = await handler.Handle(ValidUpdate(id) with { BlockedByTaskIds = [id] }, CancellationToken.None);

        Assert.True(ok);
        TaskItem? updated = await tasks.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Empty(updated.BlockedBy);
    }

    private static TaskItem NewTask(Guid id, string title) => new()
    {
        Id = id,
        Title = title,
        Priority = Priority.Medium,
        Status = Domain.Enums.TaskStatus.NotStarted,
        EstimatedDurationText = "1h",
        EstimateConfidence = Confidence.Medium,
        Notes = string.Empty,
        LastTouchedAt = DateTimeOffset.UtcNow
    };

    private static UpdateTaskCommand ValidUpdate(Guid id) => new(
        Id: id,
        Title: "Updated",
        Priority: Priority.High,
        Status: Domain.Enums.TaskStatus.InProgress,
        AssigneeId: null,
        ProjectId: null,
        RiskId: null,
        DueDate: null,
        EstimatedDurationText: "2h",
        EstimateConfidence: Confidence.High,
        ActualDurationText: null,
        Notes: "notes",
        BlockedByTaskIds: null);
}
