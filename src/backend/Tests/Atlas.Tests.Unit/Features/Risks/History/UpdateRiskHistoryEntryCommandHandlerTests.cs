using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Risks.History.UpdateRiskHistoryEntry;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Risks.History;

public sealed class UpdateRiskHistoryEntryCommandHandlerTests
{
    private readonly Mock<IRiskRepository> _risks = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly UpdateRiskHistoryEntryCommandHandler _handler;

    public UpdateRiskHistoryEntryCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new UpdateRiskHistoryEntryCommandHandler(_risks.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenEntryExists_UpdatesText()
    {
        Guid riskId = Guid.NewGuid();
        Guid entryId = Guid.NewGuid();
        var entry = new RiskHistoryEntry { Id = entryId, RiskId = riskId, Text = "Old", CreatedAt = DateTimeOffset.UtcNow };
        var risk = new Risk
        {
            Id = riskId,
            Title = "R",
            Status = RiskStatus.Open,
            Severity = SeverityLevel.Low,
            Description = "",
            Evidence = "",
            LastUpdatedAt = DateTimeOffset.UtcNow,
            History = [entry]
        };
        _risks.Setup(r => r.GetByIdWithDetailsAsync(riskId, It.IsAny<CancellationToken>())).ReturnsAsync(risk);

        bool ok = await _handler.Handle(new UpdateRiskHistoryEntryCommand(riskId, entryId, "New"), CancellationToken.None);

        ok.Should().BeTrue();
        entry.Text.Should().Be("New");
    }

    [Fact]
    public async Task Handle_WhenEntryMissing_ReturnsFalse()
    {
        Guid riskId = Guid.NewGuid();
        var risk = new Risk { Id = riskId, Title = "R", Status = RiskStatus.Open, Severity = SeverityLevel.Low, Description = "", Evidence = "", LastUpdatedAt = DateTimeOffset.UtcNow };
        _risks.Setup(r => r.GetByIdWithDetailsAsync(riskId, It.IsAny<CancellationToken>())).ReturnsAsync(risk);

        bool ok = await _handler.Handle(new UpdateRiskHistoryEntryCommand(riskId, Guid.NewGuid(), "x"), CancellationToken.None);

        ok.Should().BeFalse();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
