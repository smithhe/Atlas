using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Tasks.DeleteTask;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Tasks;

public sealed class DeleteTaskCommandHandlerTests
{
    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteTaskCommandHandler _handler;

    public DeleteTaskCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteTaskCommandHandler(_tasks.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenTaskExists_RemovesAndReturnsTrue()
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "T",
            Priority = Priority.Medium,
            Status = Domain.Enums.TaskStatus.NotStarted,
            EstimatedDurationText = "1h",
            EstimateConfidence = Confidence.Medium,
            Notes = string.Empty,
            LastTouchedAt = DateTimeOffset.UtcNow
        };
        _tasks.Setup(t => t.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        bool ok = await _handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None);

        ok.Should().BeTrue();
        _tasks.Verify(t => t.Remove(task), Times.Once);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTaskMissing_ReturnsFalse()
    {
        _tasks.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        bool ok = await _handler.Handle(new DeleteTaskCommand(Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
