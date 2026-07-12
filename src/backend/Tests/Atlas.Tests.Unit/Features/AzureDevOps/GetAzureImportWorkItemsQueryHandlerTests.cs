using Atlas.Application.Features.AzureDevOps.Import;
using Atlas.Domain.Entities;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class GetAzureImportWorkItemsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoConnection_ReturnsEmpty()
    {
        var handler = new GetAzureImportWorkItemsQueryHandler(
            new FakeAzureConnectionRepository(),
            new FakeAzureWorkItemRepository(),
            new FakeAzureUserMappingRepository());

        IReadOnlyList<AzureImportWorkItem> items = await handler.Handle(
            new GetAzureImportWorkItemsQuery(),
            CancellationToken.None);

        Assert.Empty(items);
    }

    [Fact]
    public async Task Handle_EnrichesSuggestedTeamMemberId_FromMapping()
    {
        AzureConnection connection = new()
        {
            Id = Guid.NewGuid(),
            Organization = "contoso",
            Project = "Atlas",
            ProjectId = "proj-1",
            AreaPath = "Atlas\\Core",
            IsEnabled = true
        };

        var memberId = Guid.NewGuid();
        var workItems = new FakeAzureWorkItemRepository();
        workItems.Items.Add(new AzureWorkItem
        {
            Id = Guid.NewGuid(),
            AzureConnectionId = connection.Id,
            WorkItemId = 101,
            Title = "Fix login",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "Atlas\\Core",
            IterationPath = "Sprint 1",
            AssignedToUniqueName = "ada@example.com",
            Url = "https://example.com/101",
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

        var handler = new GetAzureImportWorkItemsQueryHandler(
            new FakeAzureConnectionRepository { Singleton = connection },
            workItems,
            mappings);

        IReadOnlyList<AzureImportWorkItem> items = await handler.Handle(
            new GetAzureImportWorkItemsQuery(),
            CancellationToken.None);

        Assert.Single(items);
        Assert.Equal(memberId, items[0].SuggestedTeamMemberId);
        Assert.Equal(101, items[0].WorkItemId);
    }

    [Fact]
    public async Task Handle_ExcludesWorkItemsThatAreAlreadyLinked()
    {
        AzureConnection connection = new()
        {
            Id = Guid.NewGuid(),
            Organization = "contoso",
            Project = "Atlas",
            ProjectId = "proj-1",
            AreaPath = "Atlas\\Core",
            IsEnabled = true
        };

        var linkedId = Guid.NewGuid();
        var unlinkedId = Guid.NewGuid();
        var sharedLinks = new List<AzureWorkItemLink>();
        var links = new FakeAzureWorkItemLinkRepository(sharedLinks);
        var workItems = new FakeAzureWorkItemRepository(sharedLinks);

        workItems.Items.Add(new AzureWorkItem
        {
            Id = linkedId,
            AzureConnectionId = connection.Id,
            WorkItemId = 201,
            Title = "Linked",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "Atlas\\Core",
            IterationPath = "Sprint 1",
            Url = "https://example.com/201",
            ChangedDateUtc = DateTimeOffset.UtcNow
        });
        workItems.Items.Add(new AzureWorkItem
        {
            Id = unlinkedId,
            AzureConnectionId = connection.Id,
            WorkItemId = 202,
            Title = "Unlinked",
            State = "New",
            WorkItemType = "Task",
            AreaPath = "Atlas\\Core",
            IterationPath = "Sprint 1",
            Url = "https://example.com/202",
            ChangedDateUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        });

        links.Links.Add(new AzureWorkItemLink
        {
            Id = Guid.NewGuid(),
            AzureWorkItemId = linkedId,
            ProjectId = Guid.NewGuid(),
            LinkedAtUtc = DateTimeOffset.UtcNow
        });

        var handler = new GetAzureImportWorkItemsQueryHandler(
            new FakeAzureConnectionRepository { Singleton = connection },
            workItems,
            new FakeAzureUserMappingRepository());

        IReadOnlyList<AzureImportWorkItem> items = await handler.Handle(
            new GetAzureImportWorkItemsQuery(),
            CancellationToken.None);

        Assert.Single(items);
        Assert.Equal(unlinkedId, items[0].Id);
        Assert.Equal(202, items[0].WorkItemId);
    }
}
