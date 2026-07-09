using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Tasks.CreateTask;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Tasks;

/// <summary>
/// Canonical Moq + FluentAssertions pattern for new MediatR handler unit tests.
/// </summary>
public sealed class CreateTaskCommandHandlerTests
{
    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUnitOfWorkTransaction> _tx = new();
    private readonly CreateTaskCommandHandler _handler;

    public CreateTaskCommandHandlerTests()
    {
        _tx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tx.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tx.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tx.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CreateTaskCommandHandler(_tasks.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesTaskWithDependencies()
    {
        Guid blockerId = Guid.NewGuid();
        _tasks.Setup(t => t.ExistsAsync(blockerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        TaskItem? captured = null;
        _tasks.Setup(t => t.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((task, _) => captured = task)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(ValidCommand([blockerId]), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Ship feature");
        captured.Id.Should().Be(id);
        captured.BlockedBy.Should().ContainSingle()
            .Which.BlockerTaskId.Should().Be(blockerId);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBlockerMissing_Throws()
    {
        Guid missingBlocker = Guid.NewGuid();
        _tasks.Setup(t => t.ExistsAsync(missingBlocker, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Func<Task> act = () => _handler.Handle(ValidCommand([missingBlocker]), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'{missingBlocker}'*");
        _tasks.Verify(t => t.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
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
