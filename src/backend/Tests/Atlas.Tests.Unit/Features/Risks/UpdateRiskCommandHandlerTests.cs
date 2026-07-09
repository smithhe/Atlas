using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Risks.UpdateRisk;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Risks;

public sealed class UpdateRiskCommandHandlerTests
{
    private readonly Mock<IRiskRepository> _risks = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateRiskCommandHandler _handler;

    public UpdateRiskCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateRiskCommandHandler(_risks.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenExists_UpdatesFields()
    {
        var risk = new Risk { Id = Guid.NewGuid(), Title = "Old", Status = RiskStatus.Open, Severity = SeverityLevel.Low, Description = "d", Evidence = "e", LastUpdatedAt = DateTimeOffset.UtcNow };
        _risks.Setup(r => r.GetByIdAsync(risk.Id, It.IsAny<CancellationToken>())).ReturnsAsync(risk);

        bool ok = await _handler.Handle(new UpdateRiskCommand(risk.Id, "New", RiskStatus.Watching, SeverityLevel.High, null, "D2", "E2"), CancellationToken.None);

        ok.Should().BeTrue();
        risk.Title.Should().Be("New");
        risk.Status.Should().Be(RiskStatus.Watching);
        risk.Severity.Should().Be(SeverityLevel.High);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsFalse()
    {
        _risks.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Risk?)null);

        bool ok = await _handler.Handle(new UpdateRiskCommand(Guid.NewGuid(), "T", RiskStatus.Open, SeverityLevel.Low, null, "d", "e"), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
