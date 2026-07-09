using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;
using Atlas.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests.Integration.Persistence;

public sealed class AzureWorkItemRepositoryIntegrationTests : IClassFixture<AtlasIntegrationApplicationFactory>
{
    private readonly AtlasIntegrationApplicationFactory _factory;

    public AzureWorkItemRepositoryIntegrationTests(AtlasIntegrationApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListUnlinkedAsync_ExcludesLinkedWorkItems()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AtlasDbContext db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        IAzureWorkItemRepository workItems = scope.ServiceProvider.GetRequiredService<IAzureWorkItemRepository>();
        IAzureWorkItemLinkRepository links = scope.ServiceProvider.GetRequiredService<IAzureWorkItemLinkRepository>();
        IUnitOfWork uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var connection = new AzureConnection
        {
            Id = Guid.NewGuid(),
            Organization = "contoso",
            Project = "Atlas",
            ProjectId = "proj-1",
            AreaPath = "Atlas\\Core",
            IsEnabled = true
        };
        db.AzureConnections.Add(connection);

        var linkedItem = new AzureWorkItem
        {
            Id = Guid.NewGuid(),
            AzureConnectionId = connection.Id,
            WorkItemId = 201,
            Title = "Linked",
            State = "Active",
            WorkItemType = "Bug",
            AreaPath = "Atlas\\Core",
            IterationPath = "Sprint",
            Url = "https://example.com/201",
            ChangedDateUtc = DateTimeOffset.UtcNow
        };
        var unlinkedItem = new AzureWorkItem
        {
            Id = Guid.NewGuid(),
            AzureConnectionId = connection.Id,
            WorkItemId = 202,
            Title = "Unlinked",
            State = "New",
            WorkItemType = "Task",
            AreaPath = "Atlas\\Core",
            IterationPath = "Sprint",
            Url = "https://example.com/202",
            ChangedDateUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        await workItems.AddAsync(linkedItem, CancellationToken.None);
        await workItems.AddAsync(unlinkedItem, CancellationToken.None);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = $"Repo-{Guid.NewGuid():N}",
            Summary = "Summary",
            LastUpdatedAt = DateTimeOffset.UtcNow
        };
        db.Projects.Add(project);

        await links.AddAsync(new AzureWorkItemLink
        {
            Id = Guid.NewGuid(),
            AzureWorkItemId = linkedItem.Id,
            ProjectId = project.Id,
            LinkedAtUtc = DateTimeOffset.UtcNow
        }, CancellationToken.None);

        await uow.SaveChangesAsync(CancellationToken.None);

        IReadOnlyList<AzureWorkItem> result = await workItems.ListUnlinkedAsync(connection.Id, 50, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(unlinkedItem.Id, result[0].Id);
    }
}
