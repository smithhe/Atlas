using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Risks;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.Risks;

public sealed class RisksEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RisksEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListRisks_ReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/risks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListRisks_ReturnsFullRiskDtos()
    {
        string title = $"List-{Guid.NewGuid():N}";
        CreateRiskResponse? created = await (await _client.PostJsonAsync(
            "/risks",
            new CreateRiskRequest(
                Title: title,
                Status: RiskStatus.Open,
                Severity: SeverityLevel.High,
                ProjectId: null,
                Description: "Schedule pressure",
                Evidence: "Slipping milestones")))
            .ReadJsonAsync<CreateRiskResponse>();
        Assert.NotNull(created);

        IReadOnlyList<RiskDto>? risks = await (await _client.GetAsync("/risks"))
            .ReadJsonAsync<IReadOnlyList<RiskDto>>();
        Assert.NotNull(risks);

        RiskDto listed = Assert.Single(risks, r => r.Id == created.Id);
        Assert.Equal(title, listed.Title);
        Assert.Equal("Schedule pressure", listed.Description);
        Assert.Equal("Slipping milestones", listed.Evidence);
        Assert.NotNull(listed.LinkedTaskIds);
        Assert.NotNull(listed.LinkedTeamMemberIds);
        Assert.NotNull(listed.History);
    }

    [Fact]
    public async Task CreateGetUpdateDeleteRisk_CompletesCrudFlow()
    {
        string title = $"Risk-{Guid.NewGuid():N}";

        CreateRiskRequest createRequest = new(
            Title: title,
            Status: RiskStatus.Open,
            Severity: SeverityLevel.High,
            ProjectId: null,
            Description: "Schedule pressure",
            Evidence: "Slipping milestones");

        HttpResponseMessage createResponse = await _client.PostJsonAsync("/risks", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateRiskResponse? created = await createResponse.ReadJsonAsync<CreateRiskResponse>();
        Assert.NotNull(created);

        HttpResponseMessage getResponse = await _client.GetAsync($"/risks/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        RiskDto? risk = await getResponse.ReadJsonAsync<RiskDto>();
        Assert.NotNull(risk);
        Assert.Equal(title, risk.Title);

        UpdateRiskRequest updateRequest = new(
            Title: $"{title}-updated",
            Status: RiskStatus.Watching,
            Severity: SeverityLevel.Medium,
            ProjectId: null,
            Description: "Mitigation underway",
            Evidence: "Added buffer");

        HttpResponseMessage updateResponse = await _client.PutJsonAsync($"/risks/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/risks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage missingResponse = await _client.GetAsync($"/risks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }
}
