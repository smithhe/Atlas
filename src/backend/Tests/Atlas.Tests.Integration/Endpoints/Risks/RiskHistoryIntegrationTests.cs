using System.Net.Http;
using Atlas.Api.DTOs.Risks;
using Atlas.Api.DTOs.Risks.History;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Integration.Endpoints.Risks;

public sealed class RiskHistoryIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly HttpClient _client;

    public RiskHistoryIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RiskHistory_PersistsAcrossGetRequests()
    {
        CreateRiskResponse? risk = await (await _client.PostJsonAsync("/risks", new CreateRiskRequest(
            Title: $"History-{Guid.NewGuid():N}",
            Status: RiskStatus.Open,
            Severity: SeverityLevel.High,
            ProjectId: null,
            Description: "desc",
            Evidence: "evidence"))).ReadJsonAsync<CreateRiskResponse>();
        Assert.NotNull(risk);

        AddRiskHistoryEntryResponse? entry = await (await _client.PostJsonAsync(
            $"/risks/{risk.Id}/history",
            new AddRiskHistoryEntryRequest(risk.Id, "First note"))).ReadJsonAsync<AddRiskHistoryEntryResponse>();
        Assert.NotNull(entry);

        RiskDto? firstLoad = await (await _client.GetAsync($"/risks/{risk.Id}")).ReadJsonAsync<RiskDto>();
        RiskDto? secondLoad = await (await _client.GetAsync($"/risks/{risk.Id}")).ReadJsonAsync<RiskDto>();

        Assert.NotNull(firstLoad);
        Assert.NotNull(secondLoad);
        Assert.Single(firstLoad.History);
        Assert.Equal(entry.Id, firstLoad.History[0].Id);
        Assert.Equal("First note", secondLoad.History[0].Text);
    }
}
