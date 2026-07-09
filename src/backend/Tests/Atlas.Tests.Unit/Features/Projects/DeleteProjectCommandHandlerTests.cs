using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Projects.DeleteProject;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Projects;

public sealed class DeleteProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteProjectCommandHandler(_projects.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenProjectExists_RemovesAndReturnsTrue()
    {
        var project = new Project { Id = Guid.NewGuid(), Name = "Atlas", Summary = "S" };
        _projects.Setup(p => p.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        bool ok = await _handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        ok.Should().BeTrue();
        _projects.Verify(p => p.Remove(project), Times.Once);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectMissing_ReturnsFalse()
    {
        _projects.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        bool ok = await _handler.Handle(new DeleteProjectCommand(Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _projects.Verify(p => p.Remove(It.IsAny<Project>()), Times.Never);
    }
}
