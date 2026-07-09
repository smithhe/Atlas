using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.Risks;
using Atlas.Api.DTOs.Risks.History;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.Risks;

public sealed class RiskHistoryEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RiskHistoryEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RiskHistory_AddUpdateDeleteAndSetTeamMembers_Works()
    {
        CreateRiskResponse? risk = await CreateRiskAsync();
        Assert.NotNull(risk);

        HttpResponseMessage historyResponse = await _client.PostJsonAsync(
            $"/risks/{risk.Id}/history",
            new AddRiskHistoryEntryRequest(risk.Id, "Initial assessment"));
        AddRiskHistoryEntryResponse? entry = await historyResponse.ReadJsonAsync<AddRiskHistoryEntryResponse>();
        Assert.NotNull(entry);

        HttpResponseMessage updateHistory = await _client.PutJsonAsync(
            $"/risks/{risk.Id}/history/{entry.Id}",
            new UpdateRiskHistoryEntryRequest(risk.Id, entry.Id, "Updated assessment"));
        Assert.Equal(HttpStatusCode.NoContent, updateHistory.StatusCode);

        CreateTeamMemberResponse? member = await (await _client.PostJsonAsync(
            "/team-members",
            new CreateTeamMemberRequest($"RiskMember-{Guid.NewGuid():N}", "Engineer", StatusDot.Green)))
            .ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(member);

        HttpResponseMessage setMembers = await _client.PutJsonAsync(
            $"/risks/{risk.Id}/team-members",
            new SetRiskTeamMembersRequest([member.Id]));
        Assert.Equal(HttpStatusCode.NoContent, setMembers.StatusCode);

        RiskDto? loaded = await (await _client.GetAsync($"/risks/{risk.Id}")).ReadJsonAsync<RiskDto>();
        Assert.NotNull(loaded);
        Assert.Single(loaded.History);
        Assert.Equal("Updated assessment", loaded.History[0].Text);
        Assert.Contains(member.Id, loaded.LinkedTeamMemberIds);

        HttpResponseMessage deleteHistory = await _client.DeleteAsync($"/risks/{risk.Id}/history/{entry.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteHistory.StatusCode);
    }

    [Fact]
    public async Task SetRiskTeamMembers_WhenMemberMissing_ReturnsBadRequest()
    {
        CreateRiskResponse? risk = await CreateRiskAsync();
        Assert.NotNull(risk);

        HttpResponseMessage response = await _client.PutJsonAsync(
            $"/risks/{risk.Id}/team-members",
            new SetRiskTeamMembersRequest([Guid.NewGuid()]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetRiskTeamMembers_ClearsMembersWhenEmpty()
    {
        CreateRiskResponse? risk = await CreateRiskAsync();
        Assert.NotNull(risk);

        CreateTeamMemberResponse? member = await (await _client.PostJsonAsync(
            "/team-members",
            new CreateTeamMemberRequest($"Clear-{Guid.NewGuid():N}", "Engineer", StatusDot.Green)))
            .ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(member);

        HttpResponseMessage setMembers = await _client.PutJsonAsync(
            $"/risks/{risk.Id}/team-members",
            new SetRiskTeamMembersRequest([member.Id]));
        Assert.Equal(HttpStatusCode.NoContent, setMembers.StatusCode);

        RiskDto? afterSet = await (await _client.GetAsync($"/risks/{risk.Id}")).ReadJsonAsync<RiskDto>();
        Assert.NotNull(afterSet);
        Assert.Contains(member.Id, afterSet.LinkedTeamMemberIds);

        HttpResponseMessage clear = await _client.PutJsonAsync(
            $"/risks/{risk.Id}/team-members",
            new SetRiskTeamMembersRequest([]));
        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);

        RiskDto? loaded = await (await _client.GetAsync($"/risks/{risk.Id}")).ReadJsonAsync<RiskDto>();
        Assert.NotNull(loaded);
        Assert.Empty(loaded.LinkedTeamMemberIds);
    }

    private async Task<CreateRiskResponse?> CreateRiskAsync()
    {
        HttpResponseMessage response = await _client.PostJsonAsync("/risks", new CreateRiskRequest(
            Title: $"Risk-{Guid.NewGuid():N}",
            Status: RiskStatus.Open,
            Severity: SeverityLevel.Medium,
            ProjectId: null,
            Description: "desc",
            Evidence: "evidence"));

        return await response.ReadJsonAsync<CreateRiskResponse>();
    }
}
