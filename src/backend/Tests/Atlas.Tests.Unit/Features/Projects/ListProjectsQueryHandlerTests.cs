using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Projects.ListProjects;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Projects;

public sealed class ListProjectsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRepositoryList()
    {
        var projects = new Mock<IProjectRepository>();
        IReadOnlyList<Project> expected = [new() { Id = Guid.NewGuid(), Name = "A", Summary = "S" }];
        projects.Setup(p => p.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new ListProjectsQueryHandler(projects.Object);
        IReadOnlyList<Project> result = await handler.Handle(new ListProjectsQuery(), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }
}
