using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Risks.CreateRisk;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Risks;

public sealed class CreateRiskCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesRiskAndReturnsId()
    {
        var risks = new Mock<IRiskRepository>();
        (Mock<IUnitOfWork> uow, Mock<IUnitOfWorkTransaction> tx) = MockUnitOfWork.Create();
        Risk? captured = null;
        risks.Setup(r => r.AddAsync(It.IsAny<Risk>(), It.IsAny<CancellationToken>()))
            .Callback<Risk, CancellationToken>((risk, _) => captured = risk)
            .Returns(Task.CompletedTask);

        var handler = new CreateRiskCommandHandler(risks.Object, uow.Object);
        Guid id = await handler.Handle(new CreateRiskCommand(
            "Slippage", RiskStatus.Open, SeverityLevel.High, null, "Desc", "Evidence"), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Slippage");
        captured.Severity.Should().Be(SeverityLevel.High);
        tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
