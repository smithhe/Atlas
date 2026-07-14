using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Features.Ai.Context;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Atlas.Tests.Unit.Features.Ai;

public sealed class SettingsPromptContextBuilderTests
{
    [Fact]
    public async Task BuildContextAsync_DescribesSettingsAndAzureWithoutSecrets()
    {
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(s => s.GetSingletonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Entities.Settings
            {
                Id = Guid.NewGuid(),
                StaleDays = 7,
                DefaultAiManualOnly = true,
                Theme = Theme.Dark,
                AzureDevOpsBaseUrl = "https://dev.azure.com/example"
            });

        var azure = new Mock<IAzureConnectionRepository>();
        azure.Setup(a => a.GetSingletonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AzureConnection
            {
                Id = Guid.NewGuid(),
                Organization = "contoso",
                Project = "Atlas",
                AreaPath = "Atlas\\Team",
                TeamName = "Platform",
                IsEnabled = true
            });

        var builder = new SettingsPromptContextBuilder(settings.Object, azure.Object);
        var context = await builder.BuildContextAsync(
            new AiSessionStartRequest(Guid.NewGuid(), 0, "help", AiViewScope.Settings, null, null, null, null, null),
            CancellationToken.None);

        context.Should().StartWith("Settings context:");
        context.Should().Contain("Stale days: 7");
        context.Should().Contain("Organization: contoso");
        context.Should().Contain("Never reveal secrets");
        context.Should().Contain("OpenAI__ApiKey");
    }
}
