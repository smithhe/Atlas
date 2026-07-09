using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Risks.ListRisks;
using Atlas.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Risks;

public sealed class ListRisksQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRepositoryList()
    {
        var risks = new Mock<IRiskRepository>();
        IReadOnlyList<Risk> expected = [];
        risks.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new ListRisksQueryHandler(risks.Object);
        (await handler.Handle(new ListRisksQuery(), CancellationToken.None)).Should().BeSameAs(expected);
    }
}
