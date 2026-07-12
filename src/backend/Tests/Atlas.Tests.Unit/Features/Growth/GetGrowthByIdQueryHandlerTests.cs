using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.GetGrowth;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth;

public sealed class GetGrowthByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDetails()
    {
        var growth = new Mock<IGrowthRepository>();
        var id = Guid.NewGuid();
        var plan = new Domain.Entities.Growth { Id = id, TeamMemberId = Guid.NewGuid() };
        growth.Setup(g => g.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var handler = new GetGrowthByIdQueryHandler(growth.Object);
        (await handler.Handle(new GetGrowthByIdQuery(id), CancellationToken.None)).Should().BeSameAs(plan);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsNull()
    {
        var growth = new Mock<IGrowthRepository>();
        growth.Setup(g => g.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        var handler = new GetGrowthByIdQueryHandler(growth.Object);
        (await handler.Handle(new GetGrowthByIdQuery(Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
    }
}
