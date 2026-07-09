using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.AzureDevOps.Users;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class ListAzureUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToClientWithDefaultBaseUrl()
    {
        var client = new Mock<IAzureDevOpsClient>();
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(s => s.GetSingletonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.Settings?)null);
        IReadOnlyList<AzureUserSummary> expected = [new AzureUserSummary("Ada", "ada@x", null)];
        client.Setup(c => c.ListUsersAsync("https://dev.azure.com", "org", "pid", "tid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new ListAzureUsersQueryHandler(client.Object, settings.Object);
        (await handler.Handle(new ListAzureUsersQuery("org", "pid", "tid"), CancellationToken.None)).Should().BeSameAs(expected);
    }
}
