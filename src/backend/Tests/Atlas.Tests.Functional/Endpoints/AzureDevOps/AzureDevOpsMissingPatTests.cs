using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.AzureDevOps;
using Atlas.Persistence;
using Atlas.Tests.Common.Fakes;
using Atlas.Tests.Common.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Atlas.Tests.Functional.Endpoints.AzureDevOps;

/// <summary>
/// Missing-PAT coverage. Default functional hosts replace <see cref="IAzureDevOpsClient"/> with
/// <see cref="FakeAzureDevOpsClient"/>, which bypasses real <see cref="AzureDevOpsClient"/> construction.
/// These tests exercise the production client throw and the API middleware mapping instead.
/// </summary>
public sealed class AzureDevOpsMissingPatTests
{
    private const string MissingPatMessage =
        "Azure DevOps PAT is not configured. Set AzureDevopsToken in user-secrets, appsettings, or environment.";

    [Fact]
    public async Task AzureDevOpsClient_WhenPatMissing_ThrowsActionableMessageOnFirstCall()
    {
        IConfiguration config = new ConfigurationBuilder().Build();
        var client = new AzureDevOpsClient(new HttpClient(), config);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ListProjectsAsync("https://dev.azure.com", "contoso"));

        Assert.Equal(MissingPatMessage, ex.Message);
        Assert.Contains("AzureDevopsToken", ex.Message);
    }

    [Fact]
    public async Task ListAzureProjects_WhenClientReportsMissingPat_Returns503WithActionableMessage()
    {
        // Note: FakeAzureDevOpsClient normally bypasses real client construction in test hosts.
        using MissingPatAzureWebApplicationFactory factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/azure-devops/projects?organization=contoso");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await using System.IO.Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
        Assert.True(doc.RootElement.TryGetProperty("message", out JsonElement message));
        Assert.Contains("AzureDevopsToken", message.GetString());
        Assert.Contains("PAT is not configured", message.GetString());
    }

    private sealed class MissingPatAzureWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"AtlasMissingPat-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                AtlasTestServiceConfigurator.ConfigureInMemoryAtlas(services, _databaseName);
                services.RemoveAll<IAzureDevOpsClient>();
                services.RemoveAll<FakeAzureDevOpsClient>();
                services.AddSingleton<IAzureDevOpsClient, ThrowingMissingPatAzureDevOpsClient>();
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            IHost host = base.CreateHost(builder);
            using IServiceScope scope = host.Services.CreateScope();
            AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
            db.Database.EnsureCreated();
            return host;
        }
    }

    private sealed class ThrowingMissingPatAzureDevOpsClient : IAzureDevOpsClient
    {
        private static InvalidOperationException MissingPat() => new(MissingPatMessage);

        public Task<IReadOnlyList<AzureProjectSummary>> ListProjectsAsync(
            string baseUrl,
            string organization,
            CancellationToken cancellationToken = default) =>
            throw MissingPat();

        public Task<IReadOnlyList<AzureTeamSummary>> ListTeamsAsync(
            string baseUrl,
            string organization,
            string projectId,
            CancellationToken cancellationToken = default) =>
            throw MissingPat();

        public Task<IReadOnlyList<AzureUserSummary>> ListUsersAsync(
            string baseUrl,
            string organization,
            string projectId,
            string teamId,
            CancellationToken cancellationToken = default) =>
            throw MissingPat();

        public Task<AzureTeamAreaPaths> GetTeamAreaPathsAsync(
            string baseUrl,
            string organization,
            string projectId,
            string teamName,
            CancellationToken cancellationToken = default) =>
            throw MissingPat();

        public Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(
            string baseUrl,
            string organization,
            string project,
            string wiql,
            int? top = null,
            CancellationToken cancellationToken = default) =>
            throw MissingPat();

        public Task<IReadOnlyList<AzureWorkItemDetails>> GetWorkItemsAsync(
            string baseUrl,
            string organization,
            string project,
            IReadOnlyList<int> workItemIds,
            CancellationToken cancellationToken = default) =>
            throw MissingPat();
    }
}
