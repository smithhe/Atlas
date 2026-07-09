using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Tasks.GetTask;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Tasks;

public sealed class GetTaskByIdQueryHandlerTests
{
    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly GetTaskByIdQueryHandler _handler;

    public GetTaskByIdQueryHandlerTests() => _handler = new GetTaskByIdQueryHandler(_tasks.Object);

    [Fact]
    public async Task Handle_WhenIncludeDetails_UsesDetailsQuery()
    {
        Guid id = Guid.NewGuid();
        var task = new TaskItem
        {
            Id = id,
            Title = "T",
            Priority = Priority.Low,
            Status = Domain.Enums.TaskStatus.NotStarted,
            EstimatedDurationText = "1h",
            EstimateConfidence = Confidence.Low,
            Notes = string.Empty,
            LastTouchedAt = DateTimeOffset.UtcNow
        };
        _tasks.Setup(t => t.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        TaskItem? result = await _handler.Handle(new GetTaskByIdQuery(id, true), CancellationToken.None);

        result.Should().BeSameAs(task);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsNull()
    {
        Guid id = Guid.NewGuid();
        _tasks.Setup(t => t.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        TaskItem? result = await _handler.Handle(new GetTaskByIdQuery(id, false), CancellationToken.None);

        result.Should().BeNull();
    }
}
