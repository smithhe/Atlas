using Atlas.Application.Features.AzureDevOps.Import;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class LinkAzureWorkItemsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmptyIds_ReturnsZero()
    {
        var handler = new LinkAzureWorkItemsCommandHandler(
            new FakeAzureWorkItemLinkRepository(),
            new FakeAzureWorkItemRepository(),
            new FakeAzureUserMappingRepository(),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider());

        int updated = await handler.Handle(
            new LinkAzureWorkItemsCommand([], Guid.NewGuid(), null),
            CancellationToken.None);

        Assert.Equal(0, updated);
    }

    [Fact]
    public async Task Handle_WithExplicitTeamMemberId_OverridesMapping()
    {
        var projectId = Guid.NewGuid();
        var mappedMemberId = Guid.NewGuid();
        var explicitMemberId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();

        var workItems = new FakeAzureWorkItemRepository();
        workItems.Items.Add(new AzureWorkItem
        {
            Id = workItemId,
            AzureConnectionId = Guid.NewGuid(),
            WorkItemId = 101,
            AssignedToUniqueName = "ada@example.com",
            Title = "Fix",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "Atlas",
            IterationPath = "Sprint",
            Url = "https://example.com",
            ChangedDateUtc = DateTimeOffset.UtcNow
        });

        var mappings = new FakeAzureUserMappingRepository();
        mappings.Mappings.Add(new AzureUserMapping
        {
            Id = Guid.NewGuid(),
            AzureUniqueName = "ada@example.com",
            TeamMemberId = mappedMemberId,
            LinkedAtUtc = DateTimeOffset.UtcNow
        });

        var links = new FakeAzureWorkItemLinkRepository();
        var handler = new LinkAzureWorkItemsCommandHandler(
            links,
            workItems,
            mappings,
            new FakeUnitOfWork(),
            new FakeDateTimeProvider());

        int updated = await handler.Handle(
            new LinkAzureWorkItemsCommand([workItemId], projectId, explicitMemberId),
            CancellationToken.None);

        Assert.Equal(1, updated);
        Assert.Single(links.Links);
        Assert.Equal(projectId, links.Links[0].ProjectId);
        Assert.Equal(explicitMemberId, links.Links[0].TeamMemberId);
        Assert.NotEqual(mappedMemberId, links.Links[0].TeamMemberId);
        Assert.Equal(workItemId, links.Links[0].AzureWorkItemId);
    }

    [Fact]
    public async Task Handle_WithoutTeamMemberId_MapsViaAzureUserMapping()
    {
        var workItemId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var workItems = new FakeAzureWorkItemRepository();
        workItems.Items.Add(new AzureWorkItem
        {
            Id = workItemId,
            AzureConnectionId = Guid.NewGuid(),
            WorkItemId = 101,
            AssignedToUniqueName = "ada@example.com",
            Title = "Fix",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "Atlas",
            IterationPath = "Sprint",
            Url = "https://example.com",
            ChangedDateUtc = DateTimeOffset.UtcNow
        });

        var mappings = new FakeAzureUserMappingRepository();
        mappings.Mappings.Add(new AzureUserMapping
        {
            Id = Guid.NewGuid(),
            AzureUniqueName = "ada@example.com",
            TeamMemberId = memberId,
            LinkedAtUtc = DateTimeOffset.UtcNow
        });

        var links = new FakeAzureWorkItemLinkRepository();
        var handler = new LinkAzureWorkItemsCommandHandler(
            links, workItems, mappings, new FakeUnitOfWork(), new FakeDateTimeProvider());

        int updated = await handler.Handle(
            new LinkAzureWorkItemsCommand([workItemId], projectId, null),
            CancellationToken.None);

        Assert.Equal(1, updated);
        Assert.Equal(memberId, links.Links[0].TeamMemberId);
    }

    [Fact]
    public async Task Handle_WhenWorkItemAlreadyLinked_UpdatesProjectAndTimestamp()
    {
        var workItemId = Guid.NewGuid();
        var oldProject = Guid.NewGuid();
        var newProject = Guid.NewGuid();
        var clock = new FakeDateTimeProvider(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        var links = new FakeAzureWorkItemLinkRepository();
        links.Links.Add(new AzureWorkItemLink
        {
            Id = Guid.NewGuid(),
            AzureWorkItemId = workItemId,
            ProjectId = oldProject,
            TeamMemberId = null,
            LinkedAtUtc = clock.UtcNow.AddDays(-7)
        });

        var handler = new LinkAzureWorkItemsCommandHandler(
            links,
            new FakeAzureWorkItemRepository(),
            new FakeAzureUserMappingRepository(),
            new FakeUnitOfWork(),
            clock);

        int updated = await handler.Handle(
            new LinkAzureWorkItemsCommand([workItemId], newProject, null),
            CancellationToken.None);

        Assert.Equal(1, updated);
        Assert.Single(links.Links);
        Assert.Equal(newProject, links.Links[0].ProjectId);
        Assert.Equal(clock.UtcNow, links.Links[0].LinkedAtUtc);
    }
}
