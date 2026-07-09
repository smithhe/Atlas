using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Risks.GetRisk;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Risks;

public sealed class GetRiskByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenIncludeDetails_UsesDetails()
    {
        var risks = new Mock<IRiskRepository>();
        Guid id = Guid.NewGuid();
        var risk = new Risk { Id = id, Title = "R", Status = RiskStatus.Open, Severity = SeverityLevel.Medium, Description = "", Evidence = "", LastUpdatedAt = DateTimeOffset.UtcNow };
        risks.Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(risk);

        var handler = new GetRiskByIdQueryHandler(risks.Object);
        Risk? result = await handler.Handle(new GetRiskByIdQuery(id, true), CancellationToken.None);

        result.Should().BeSameAs(risk);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsNull()
    {
        var risks = new Mock<IRiskRepository>();
        risks.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Risk?)null);

        var handler = new GetRiskByIdQueryHandler(risks.Object);
        Risk? result = await handler.Handle(new GetRiskByIdQuery(Guid.NewGuid(), false), CancellationToken.None);

        result.Should().BeNull();
    }
}
