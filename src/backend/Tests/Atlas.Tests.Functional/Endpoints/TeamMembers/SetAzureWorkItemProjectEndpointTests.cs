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

public sealed class SetAzureWorkItemProjectEndpointTests : IClassFixture<AtlasWebApplicationFactory>
{
    [Fact]
    public async Task SetAzureWorkItemProject_WhenLinked_PersistsProjectId()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        var memberId = Guid.NewGuid();
        var initialProjectId = Guid.NewGuid();
        var nextProjectId = Guid.NewGuid();
        const int workItemId = 5150;

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
                Url = "https://example.com/5150",
                ChangedDateUtc = DateTimeOffset.UtcNow
            };
            var initialProject = new Project
            {
                Id = initialProjectId,
                Name = $"Initial-{Guid.NewGuid():N}",
                Summary = "Summary",
                LastUpdatedAt = DateTimeOffset.UtcNow
            };
            var nextProject = new Project
            {
                Id = nextProjectId,
                Name = $"Next-{Guid.NewGuid():N}",
                Summary = "Summary",
                LastUpdatedAt = DateTimeOffset.UtcNow
            };
            var member = new TeamMember
            {
                Id = memberId,
                Name = $"Member-{Guid.NewGuid():N}",
                Role = "Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = string.Empty,
                AzureWorkItemLinks =
                [
                    new AzureWorkItemLink
                    {
                        Id = Guid.NewGuid(),
                        AzureWorkItemId = workItem.Id,
                        ProjectId = initialProjectId,
                        TeamMemberId = memberId,
                        LinkedAtUtc = DateTimeOffset.UtcNow
                    }
                ]
            };

            db.AzureConnections.Add(connection);
            db.AzureWorkItems.Add(workItem);
            db.Projects.AddRange(initialProject, nextProject);
            db.TeamMembers.Add(member);
            await db.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.PutJsonAsync(
            $"/team-members/{memberId}/azure-work-items/{workItemId}/project",
            new SetAzureWorkItemProjectRequest(memberId, workItemId, nextProjectId));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        TeamMemberDto? detail = await (await client.GetAsync($"/team-members/{memberId}"))
            .ReadJsonAsync<TeamMemberDto>();
        detail.Should().NotBeNull();
        detail.AzureWorkItems.Should().ContainSingle(x => x.Id == workItemId.ToString() && x.ProjectId == nextProjectId);
    }

    [Fact]
    public async Task SetAzureWorkItemProject_WhenProjectMissing_ReturnsNotFound()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        var memberId = Guid.NewGuid();
        var initialProjectId = Guid.NewGuid();
        var missingProjectId = Guid.NewGuid();
        const int workItemId = 5151;

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
                Url = "https://example.com/5151",
                ChangedDateUtc = DateTimeOffset.UtcNow
            };
            var initialProject = new Project
            {
                Id = initialProjectId,
                Name = $"Initial-{Guid.NewGuid():N}",
                Summary = "Summary",
                LastUpdatedAt = DateTimeOffset.UtcNow
            };
            var member = new TeamMember
            {
                Id = memberId,
                Name = $"Member-{Guid.NewGuid():N}",
                Role = "Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = string.Empty,
                AzureWorkItemLinks =
                [
                    new AzureWorkItemLink
                    {
                        Id = Guid.NewGuid(),
                        AzureWorkItemId = workItem.Id,
                        ProjectId = initialProjectId,
                        TeamMemberId = memberId,
                        LinkedAtUtc = DateTimeOffset.UtcNow
                    }
                ]
            };

            db.AzureConnections.Add(connection);
            db.AzureWorkItems.Add(workItem);
            db.Projects.Add(initialProject);
            db.TeamMembers.Add(member);
            await db.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.PutJsonAsync(
            $"/team-members/{memberId}/azure-work-items/{workItemId}/project",
            new SetAzureWorkItemProjectRequest(memberId, workItemId, missingProjectId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetAzureWorkItemProject_WhenClearingProject_ClearsLinkProjectId()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        var memberId = Guid.NewGuid();
        var initialProjectId = Guid.NewGuid();
        const int workItemId = 5152;

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
                Url = "https://example.com/5152",
                ChangedDateUtc = DateTimeOffset.UtcNow
            };
            var initialProject = new Project
            {
                Id = initialProjectId,
                Name = $"Initial-{Guid.NewGuid():N}",
                Summary = "Summary",
                LastUpdatedAt = DateTimeOffset.UtcNow
            };
            var member = new TeamMember
            {
                Id = memberId,
                Name = $"Member-{Guid.NewGuid():N}",
                Role = "Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = string.Empty,
                AzureWorkItemLinks =
                [
                    new AzureWorkItemLink
                    {
                        Id = Guid.NewGuid(),
                        AzureWorkItemId = workItem.Id,
                        ProjectId = initialProjectId,
                        TeamMemberId = memberId,
                        LinkedAtUtc = DateTimeOffset.UtcNow
                    }
                ]
            };

            db.AzureConnections.Add(connection);
            db.AzureWorkItems.Add(workItem);
            db.Projects.Add(initialProject);
            db.TeamMembers.Add(member);
            await db.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.PutJsonAsync(
            $"/team-members/{memberId}/azure-work-items/{workItemId}/project",
            new SetAzureWorkItemProjectRequest(memberId, workItemId, null));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        TeamMemberDto? detail = await (await client.GetAsync($"/team-members/{memberId}"))
            .ReadJsonAsync<TeamMemberDto>();
        detail.Should().NotBeNull();
        detail.AzureWorkItems.Should().ContainSingle(x => x.Id == workItemId.ToString() && x.ProjectId == Guid.Empty);
    }

    [Fact]
    public async Task SetAzureWorkItemProject_WhenMemberMissing_ReturnsNotFound()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        var memberId = Guid.NewGuid();

        HttpResponseMessage response = await client.PutJsonAsync(
            $"/team-members/{memberId}/azure-work-items/5153/project",
            new SetAzureWorkItemProjectRequest(memberId, 5153, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetAzureWorkItemProject_WhenWorkItemNotLinked_ReturnsNotFound()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        var memberId = Guid.NewGuid();
        const int workItemId = 5154;

        using (IServiceScope scope = isolated.Services.CreateScope())
        {
            AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
            db.TeamMembers.Add(new TeamMember
            {
                Id = memberId,
                Name = $"Member-{Guid.NewGuid():N}",
                Role = "Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = string.Empty
            });
            await db.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.PutJsonAsync(
            $"/team-members/{memberId}/azure-work-items/{workItemId}/project",
            new SetAzureWorkItemProjectRequest(memberId, workItemId, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetAzureWorkItemProject_WhenInvalidBody_ReturnsBadRequest()
    {
        using AtlasWebApplicationFactory isolated = new();
        HttpClient client = isolated.CreateClient();

        var memberId = Guid.NewGuid();
        const int workItemId = 5155;

        using (IServiceScope scope = isolated.Services.CreateScope())
        {
            AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
            db.TeamMembers.Add(new TeamMember
            {
                Id = memberId,
                Name = $"Member-{Guid.NewGuid():N}",
                Role = "Engineer",
                StatusDot = StatusDot.Green,
                CurrentFocus = string.Empty
            });
            await db.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.PutJsonAsync(
            $"/team-members/{memberId}/azure-work-items/{workItemId}/project",
            new SetAzureWorkItemProjectRequest(memberId, workItemId, Guid.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
