using Atlas.Application.Features.Tasks.CreateTask;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.Tasks;

public sealed class CreateTaskCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_CreatesTaskWithDependencies()
    {
        var tasks = new FakeTaskRepository();
        var blocker = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Blocker",
            Priority = Priority.Medium,
            Status = Domain.Enums.TaskStatus.NotStarted,
            EstimatedDurationText = "1h",
            EstimateConfidence = Confidence.Medium,
            Notes = string.Empty,
            LastTouchedAt = DateTimeOffset.UtcNow
        };
        tasks.Seed(blocker);

        var handler = new CreateTaskCommandHandler(tasks, new FakeUnitOfWork());
        Guid id = await handler.Handle(ValidCommand([blocker.Id]), CancellationToken.None);

        TaskItem? created = await tasks.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(created);
        Assert.Equal("Ship feature", created.Title);
        Assert.Single(created.BlockedBy);
        Assert.Equal(blocker.Id, created.BlockedBy[0].BlockerTaskId);
    }

    [Fact]
    public async Task Handle_WhenBlockerMissing_Throws()
    {
        var handler = new CreateTaskCommandHandler(new FakeTaskRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(ValidCommand([Guid.NewGuid()]), CancellationToken.None));
    }

    private static CreateTaskCommand ValidCommand(IReadOnlyList<Guid>? blockers) => new(
        Title: "Ship feature",
        Priority: Priority.Medium,
        Status: Domain.Enums.TaskStatus.NotStarted,
        AssigneeId: null,
        ProjectId: null,
        RiskId: null,
        DueDate: null,
        EstimatedDurationText: "1h",
        EstimateConfidence: Confidence.Medium,
        ActualDurationText: null,
        Notes: string.Empty,
        BlockedByTaskIds: blockers);
}
