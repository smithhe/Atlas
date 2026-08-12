using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.AzureDevOps.Projects;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class ListAzureProjectsQueryHandlerTests
{
    [Fact]
    public async Task Handle_UsesSettingsBaseUrlOrDefault()
    {
        var client = new Mock<IAzureDevOpsClient>();
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(s => s.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Settings?)null);
        IReadOnlyList<AzureProjectSummary> expected = [new("1", "Atlas")];
        client.Setup(c => c.ListProjectsAsync("https://dev.azure.com", "contoso", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new ListAzureProjectsQueryHandler(client.Object, settings.Object);
        IReadOnlyList<AzureProjectSummary> result = await handler.Handle(new ListAzureProjectsQuery("contoso"), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_UsesConfiguredBaseUrl()
    {
        var client = new Mock<IAzureDevOpsClient>();
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(s => s.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Domain.Entities.Settings
        {
            Id = Guid.NewGuid(),
            StaleDays = 10,
            DefaultAiManualOnly = true,
            Theme = Domain.Enums.Theme.Dark,
            AzureDevOpsBaseUrl = " https://ado.example "
        });
        client.Setup(c => c.ListProjectsAsync("https://ado.example", "org", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AzureProjectSummary>());

        var handler = new ListAzureProjectsQueryHandler(client.Object, settings.Object);
        await handler.Handle(new ListAzureProjectsQuery("org"), CancellationToken.None);

        client.Verify(c => c.ListProjectsAsync("https://ado.example", "org", It.IsAny<CancellationToken>()), Times.Once);
    }
}
