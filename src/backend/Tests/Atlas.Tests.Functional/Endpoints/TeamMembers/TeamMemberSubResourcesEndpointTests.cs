using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Api.DTOs.TeamMembers.Notes;
using Atlas.Api.DTOs.TeamMembers.Profile;
using Atlas.Api.DTOs.TeamMembers.Risks;
using Atlas.Api.DTOs.TeamMembers.Signals;
using Atlas.Domain.Enums;

namespace Atlas.Tests.Functional.Endpoints.TeamMembers;

public sealed class TeamMemberSubResourcesEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TeamMemberSubResourcesEndpointTests(AtlasWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TeamMemberNotesRisksProfileAndSignals_FullFlow()
    {
        CreateTeamMemberResponse? member = await (await _client.PostJsonAsync(
            "/team-members",
            new CreateTeamMemberRequest($"Sub-{Guid.NewGuid():N}", "Engineer", StatusDot.Green)))
            .ReadJsonAsync<CreateTeamMemberResponse>();
        Assert.NotNull(member);

        AddTeamNoteResponse? note = await (await _client.PostJsonAsync(
            $"/team-members/{member.Id}/notes",
            new AddTeamNoteRequest(
                member.Id,
                NoteType.Standup,
                "Standup",
                "Shipped tests",
                "12345",
                "https://dev.azure.com/org/project/_git/repo/pullrequest/1")))
            .ReadJsonAsync<AddTeamNoteResponse>();
        Assert.NotNull(note);

        HttpResponseMessage updateNote = await _client.PutJsonAsync(
            $"/team-members/{member.Id}/notes/{note.Id}",
            new UpdateTeamNoteRequest(
                member.Id,
                note.Id,
                NoteType.Standup,
                "Standup",
                "Shipped more tests",
                null,
                "67890",
                "https://dev.azure.com/org/project/_git/repo/pullrequest/2"));
        Assert.Equal(HttpStatusCode.NoContent, updateNote.StatusCode);

        HttpResponseMessage pinNotes = await _client.PutJsonAsync(
            $"/team-members/{member.Id}/notes/pins",
            new SetPinnedNotesRequest(member.Id, [note.Id]));
        Assert.Equal(HttpStatusCode.NoContent, pinNotes.StatusCode);

        AddTeamMemberRiskResponse? risk = await (await _client.PostJsonAsync(
            $"/team-members/{member.Id}/risks",
            new AddTeamMemberRiskRequest(
                member.Id,
                "Context switching",
                TeamMemberRiskSeverity.Medium,
                "Focus",
                TeamMemberRiskStatus.Open,
                TeamMemberRiskTrend.Worsening,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Delivery",
                "Too many threads",
                "Reduce WIP",
                null))).ReadJsonAsync<AddTeamMemberRiskResponse>();
        Assert.NotNull(risk);

        HttpResponseMessage updateRisk = await _client.PutJsonAsync(
            $"/team-members/{member.Id}/risks/{risk.Id}",
            new UpdateTeamMemberRiskRequest(
                member.Id,
                risk.Id,
                "Context switching",
                TeamMemberRiskSeverity.Low,
                "Focus",
                TeamMemberRiskStatus.Mitigating,
                TeamMemberRiskTrend.Stable,
                DateOnly.FromDateTime(DateTime.UtcNow),
                "Delivery",
                "Improved",
                "Keep WIP low",
                null,
                DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.NoContent, updateRisk.StatusCode);

        HttpResponseMessage profile = await _client.PutJsonAsync(
            $"/team-members/{member.Id}/profile",
            new UpdateTeamMemberProfileRequest(member.Id, "America/Chicago", "9-5 CST"));
        Assert.Equal(HttpStatusCode.NoContent, profile.StatusCode);

        HttpResponseMessage signals = await _client.PutJsonAsync(
            $"/team-members/{member.Id}/signals",
            new UpdateTeamMemberSignalsRequest(member.Id, LoadSignal.Heavy, DeliverySignal.OnTrack, SupportNeededSignal.Medium));
        Assert.Equal(HttpStatusCode.NoContent, signals.StatusCode);

        TeamMemberDto? loaded = await (await _client.GetAsync($"/team-members/{member.Id}")).ReadJsonAsync<TeamMemberDto>();
        Assert.NotNull(loaded);
        Assert.Single(loaded.Notes);
        Assert.Equal("Shipped more tests", loaded.Notes[0].Text);
        Assert.Equal(0, loaded.Notes[0].PinnedOrder);
        Assert.Equal("67890", loaded.Notes[0].AdoWorkItemId);
        Assert.Equal("https://dev.azure.com/org/project/_git/repo/pullrequest/2", loaded.Notes[0].PrUrl);
        Assert.Single(loaded.Risks);
        Assert.Equal(TeamMemberRiskSeverity.Low, loaded.Risks[0].Severity);
        Assert.Equal("America/Chicago", loaded.Profile.TimeZone);
        Assert.Equal(LoadSignal.Heavy, loaded.Signals.Load);

        HttpResponseMessage deleteRisk = await _client.DeleteAsync($"/team-members/{member.Id}/risks/{risk.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRisk.StatusCode);

        HttpResponseMessage deleteNote = await _client.DeleteAsync($"/team-members/{member.Id}/notes/{note.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteNote.StatusCode);
    }
}
