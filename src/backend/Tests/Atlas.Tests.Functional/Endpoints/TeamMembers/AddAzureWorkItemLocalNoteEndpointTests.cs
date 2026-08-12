using System.Net;
using System.Net.Http;
using Atlas.Api.DTOs.TeamMembers;
using Atlas.Api.DTOs.TeamMembers.AzureWorkItems;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests.Functional.Endpoints.TeamMembers;

public sealed class AddAzureWorkItemLocalNoteEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    [Fact]
    public async Task AddAzureWorkItemLocalNote_WhenLinked_ReturnsCreated()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        var memberId = Guid.NewGuid();
        const int workItemId = 4242;

        using (IServiceScope scope = isolated.Services.CreateScope())
        {
            AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();

            var connection = new AzureConnection
            {
                Id = Guid.NewGuid(),
                Organization = "contoso",
                Project = "Atlas",
                ProjectId = "proj-1",
                AreaPath = "Atlas\\Core",
                IsEnabled = true
            };
            var workItem = new AzureWorkItem
            {
                Id = Guid.NewGuid(),
                AzureConnectionId = connection.Id,
                WorkItemId = workItemId,
                Title = "Linked WI",
                State = "Active",
                WorkItemType = "Bug",
                AreaPath = "Atlas\\Core",
                IterationPath = "Sprint",
                Url = "https://example.com/4242",
                ChangedDateUtc = DateTimeOffset.UtcNow
            };
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = $"NoteProj-{Guid.NewGuid():N}",
                Summary = "Summary",
                LastUpdatedAt = DateTimeOffset.UtcNow
            };
            var member = new TeamMember
            {
                Id = memberId,
                Name = $"NoteMember-{Guid.NewGuid():N}",
                Role = "Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = string.Empty,
                AzureWorkItemLinks =
                [
                    new AzureWorkItemLink
                    {
                        Id = Guid.NewGuid(),
                        AzureWorkItemId = workItem.Id,
                        ProjectId = project.Id,
                        TeamMemberId = memberId,
                        LinkedAtUtc = DateTimeOffset.UtcNow
                    }
                ]
            };

            db.AzureConnections.Add(connection);
            db.AzureWorkItems.Add(workItem);
            db.Projects.Add(project);
            db.TeamMembers.Add(member);
            await db.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.PostJsonAsync(
            $"/team-members/{memberId}/azure-work-items/{workItemId}/notes",
            new AddAzureWorkItemLocalNoteRequest(memberId, workItemId, "Local ADO note"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AddAzureWorkItemLocalNoteResponse? created = await response.ReadJsonAsync<AddAzureWorkItemLocalNoteResponse>();
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);

        TeamMemberDto? detail = await (await client.GetAsync($"/team-members/{memberId}"))
            .ReadJsonAsync<TeamMemberDto>();
        detail.Should().NotBeNull();
        TeamMemberAzureWorkItemDto wi = detail.AzureWorkItems.Should()
            .ContainSingle(x => x.Id == workItemId.ToString()).Subject;
        wi.LocalNotes.Should().ContainSingle(n => n.Id == created.Id && n.Text == "Local ADO note");
    }

    [Fact]
    public async Task AddAzureWorkItemLocalNote_WhenNotLinked_ReturnsNotFound()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        var memberId = Guid.NewGuid();
        using (IServiceScope scope = isolated.Services.CreateScope())
        {
            AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
            db.TeamMembers.Add(new TeamMember
            {
                Id = memberId,
                Name = $"Orphan-{Guid.NewGuid():N}",
                Role = "Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = string.Empty
            });
            await db.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.PostJsonAsync(
            $"/team-members/{memberId}/azure-work-items/9999/notes",
            new AddAzureWorkItemLocalNoteRequest(memberId, 9999, "orphan note"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
