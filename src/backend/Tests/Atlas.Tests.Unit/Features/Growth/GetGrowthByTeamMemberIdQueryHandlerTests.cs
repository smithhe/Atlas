using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Growth.GetGrowth;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Growth;

public sealed class GetGrowthByTeamMemberIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDetailsByTeamMember()
    {
        var growth = new Mock<IGrowthRepository>();
        Guid memberId = Guid.NewGuid();
        var plan = new Domain.Entities.Growth { Id = Guid.NewGuid(), TeamMemberId = memberId };
        growth.Setup(g => g.GetByTeamMemberIdWithDetailsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var handler = new GetGrowthByTeamMemberIdQueryHandler(growth.Object);
        (await handler.Handle(new GetGrowthByTeamMemberIdQuery(memberId), CancellationToken.None)).Should().BeSameAs(plan);
    }

    [Fact]
    public async Task Handle_WhenMissing_ReturnsNull()
    {
        var growth = new Mock<IGrowthRepository>();
        growth.Setup(g => g.GetByTeamMemberIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Growth?)null);

        var handler = new GetGrowthByTeamMemberIdQueryHandler(growth.Object);
        (await handler.Handle(new GetGrowthByTeamMemberIdQuery(Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
    }
}
