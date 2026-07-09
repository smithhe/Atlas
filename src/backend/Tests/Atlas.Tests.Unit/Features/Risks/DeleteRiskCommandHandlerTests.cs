using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Risks.DeleteRisk;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Risks;

public sealed class DeleteRiskCommandHandlerTests
{
    private readonly Mock<IRiskRepository> _risks = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteRiskCommandHandler _handler;

    public DeleteRiskCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteRiskCommandHandler(_risks.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExists_Removes()
    {
        var risk = new Risk { Id = Guid.NewGuid(), Title = "R", Status = RiskStatus.Open, Severity = SeverityLevel.Low, Description = "", Evidence = "", LastUpdatedAt = DateTimeOffset.UtcNow };
        _risks.Setup(r => r.GetByIdAsync(risk.Id, It.IsAny<CancellationToken>())).ReturnsAsync(risk);

        bool ok = await _handler.Handle(new DeleteRiskCommand(risk.Id), CancellationToken.None);

        ok.Should().BeTrue();
        _risks.Verify(r => r.Remove(risk), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _risks.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Risk?)null);

        bool ok = await _handler.Handle(new DeleteRiskCommand(Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
