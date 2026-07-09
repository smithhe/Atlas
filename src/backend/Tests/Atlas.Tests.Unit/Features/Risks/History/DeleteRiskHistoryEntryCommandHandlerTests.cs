using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Risks.History.DeleteRiskHistoryEntry;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Risks.History;

public sealed class DeleteRiskHistoryEntryCommandHandlerTests
{
    private readonly Mock<IRiskRepository> _risks = new();
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUnitOfWorkTransaction> _tx;
    private readonly DeleteRiskHistoryEntryCommandHandler _handler;

    public DeleteRiskHistoryEntryCommandHandlerTests()
    {
        (_uow, _tx) = MockUnitOfWork.Create();
        _handler = new DeleteRiskHistoryEntryCommandHandler(_risks.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_WhenEntryExists_Removes()
    {
        Guid riskId = Guid.NewGuid();
        Guid entryId = Guid.NewGuid();
        var risk = new Risk
        {
            Id = riskId,
            Title = "R",
            Status = RiskStatus.Open,
            Severity = SeverityLevel.Low,
            Description = "",
            Evidence = "",
            LastUpdatedAt = DateTimeOffset.UtcNow,
            History = [new RiskHistoryEntry { Id = entryId, RiskId = riskId, Text = "x", CreatedAt = DateTimeOffset.UtcNow }]
        };
        _risks.Setup(r => r.GetByIdWithDetailsAsync(riskId, It.IsAny<CancellationToken>())).ReturnsAsync(risk);

        bool ok = await _handler.Handle(new DeleteRiskHistoryEntryCommand(riskId, entryId), CancellationToken.None);

        ok.Should().BeTrue();
        risk.History.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRiskMissing_ReturnsFalse()
    {
        _risks.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Risk?)null);

        bool ok = await _handler.Handle(new DeleteRiskHistoryEntryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ok.Should().BeFalse();
    }
}
