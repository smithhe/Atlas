using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Projects.GetProject;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Projects;

public sealed class GetProjectByIdQueryHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly GetProjectByIdQueryHandler _handler;

    public GetProjectByIdQueryHandlerTests()
    {
        _handler = new GetProjectByIdQueryHandler(_projects.Object);
    }

    [Fact]
    public async Task Handle_WhenIncludeDetails_UsesDetailsQuery()
    {
        var id = Guid.NewGuid();
        var project = new Project { Id = id, Name = "Atlas", Summary = "S" };
        _projects.Setup(p => p.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        Project? result = await _handler.Handle(new GetProjectByIdQuery(id, IncludeDetails: true), CancellationToken.None);

        result.Should().BeSameAs(project);
        _projects.Verify(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotIncludeDetails_UsesBasicQuery()
    {
        var id = Guid.NewGuid();
        _projects.Setup(p => p.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        Project? result = await _handler.Handle(new GetProjectByIdQuery(id, IncludeDetails: false), CancellationToken.None);

        result.Should().BeNull();
        _projects.Verify(p => p.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
