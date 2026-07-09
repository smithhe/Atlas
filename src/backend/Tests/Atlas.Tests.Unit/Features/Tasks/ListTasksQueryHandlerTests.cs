using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Tasks.ListTasks;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Tasks;

public sealed class ListTasksQueryHandlerTests
{
    [Fact]
    public async Task Handle_PassesIdsFilterToRepository()
    {
        var tasks = new Mock<ITaskRepository>();
        Guid[] ids = [Guid.NewGuid()];
        IReadOnlyList<TaskItem> expected = [];
        tasks.Setup(t => t.ListAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new ListTasksQueryHandler(tasks.Object);
        IReadOnlyList<TaskItem> result = await handler.Handle(new ListTasksQuery(ids), CancellationToken.None);

        result.Should().BeSameAs(expected);
        tasks.Verify(t => t.ListAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
