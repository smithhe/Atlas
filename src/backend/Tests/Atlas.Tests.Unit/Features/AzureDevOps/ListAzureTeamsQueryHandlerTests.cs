using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.Teams;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class ListAzureTeamsQueryHandlerTests
{
    [Fact]
    public async Task Handle_UsesConfiguredBaseUrlFromSettings()
    {
        var settings = new FakeSettingsRepository
        {
            Singleton = new Domain.Entities.Settings
            {
                Id = Guid.NewGuid(),
                StaleDays = 14,
                DefaultAiManualOnly = true,
                Theme = Domain.Enums.Theme.Dark,
                AzureDevOpsBaseUrl = "https://custom.azure.com"
            }
        };

        var client = new RecordingAzureDevOpsClient();
        var handler = new ListAzureTeamsQueryHandler(client, settings);

        IReadOnlyList<AzureTeamSummary> teams = await handler.Handle(
            new ListAzureTeamsQuery("org", "proj"),
            CancellationToken.None);

        Assert.Equal("https://custom.azure.com", client.LastBaseUrl);
        Assert.Single(teams);
        Assert.Equal("team-1", teams[0].Id);
    }

    [Fact]
    public async Task Handle_WhenSettingsMissing_UsesDefaultBaseUrl()
    {
        var client = new RecordingAzureDevOpsClient();
        var handler = new ListAzureTeamsQueryHandler(client, new FakeSettingsRepository());

        await handler.Handle(new ListAzureTeamsQuery("org", "proj"), CancellationToken.None);

        Assert.Equal("https://dev.azure.com", client.LastBaseUrl);
    }

    private sealed class RecordingAzureDevOpsClient : IAzureDevOpsClient
    {
        public string? LastBaseUrl { get; private set; }

        public Task<IReadOnlyList<AzureProjectSummary>> ListProjectsAsync(string baseUrl, string organization, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AzureProjectSummary>>([]);

        public Task<IReadOnlyList<AzureTeamSummary>> ListTeamsAsync(string baseUrl, string organization, string projectId, CancellationToken cancellationToken = default)
        {
            LastBaseUrl = baseUrl;
            return Task.FromResult<IReadOnlyList<AzureTeamSummary>>([new AzureTeamSummary("team-1", "Core")]);
        }

        public Task<IReadOnlyList<AzureUserSummary>> ListUsersAsync(string baseUrl, string organization, string projectId, string teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AzureUserSummary>>([]);

        public Task<AzureTeamAreaPaths> GetTeamAreaPathsAsync(string baseUrl, string organization, string projectId, string teamName, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AzureTeamAreaPaths(null, []));

        public Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(string baseUrl, string organization, string project, string wiql, int? top = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<int>>([]);

        public Task<IReadOnlyList<AzureWorkItemDetails>> GetWorkItemsAsync(string baseUrl, string organization, string project, IReadOnlyList<int> workItemIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AzureWorkItemDetails>>([]);
    }
}
