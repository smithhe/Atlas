using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.AzureDevOps.Areas;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class ListAzureTeamAreaPathsQueryHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToClient()
    {
        var client = new Mock<IAzureDevOpsClient>();
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(s => s.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Settings?)null);
        var expected = new AzureTeamAreaPaths("default", [new AzureTeamAreaPath("\\Area", true)]);
        client.Setup(c => c.GetTeamAreaPathsAsync("https://dev.azure.com", "org", "pid", "team", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new ListAzureTeamAreaPathsQueryHandler(client.Object, settings.Object);
        (await handler.Handle(new ListAzureTeamAreaPathsQuery("org", "pid", "team"), CancellationToken.None)).Should().BeSameAs(expected);
    }
}
