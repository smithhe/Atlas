using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Risks.History.AddRiskHistoryEntry;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Risks.History;

public sealed class AddRiskHistoryEntryCommandHandlerTests
{
    private readonly Mock<IRiskRepository> _risks = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly AddRiskHistoryEntryCommandHandler _handler;

    public AddRiskHistoryEntryCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new AddRiskHistoryEntryCommandHandler(_risks.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenRiskExists_AddsEntry()
    {
        var risk = new Risk { Id = Guid.NewGuid(), Title = "R", Status = RiskStatus.Open, Severity = SeverityLevel.Low, Description = "", Evidence = "", LastUpdatedAt = DateTimeOffset.UtcNow };
        _risks.Setup(r => r.GetByIdAsync(risk.Id, It.IsAny<CancellationToken>())).ReturnsAsync(risk);
        RiskHistoryEntry? captured = null;
        _risks.Setup(r => r.AddHistoryEntryAsync(It.IsAny<RiskHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Callback<RiskHistoryEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        Guid id = await _handler.Handle(new AddRiskHistoryEntryCommand(risk.Id, "Noted"), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured.Should().NotBeNull();
        captured!.Text.Should().Be("Noted");
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRiskMissing_ReturnsEmptyGuid()
    {
        _risks.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Risk?)null);

        Guid id = await _handler.Handle(new AddRiskHistoryEntryCommand(Guid.NewGuid(), "x"), CancellationToken.None);

        id.Should().Be(Guid.Empty);
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
