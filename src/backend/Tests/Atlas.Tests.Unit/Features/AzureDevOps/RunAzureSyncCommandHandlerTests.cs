using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Features.AzureDevOps.RunAzureSync;
using Atlas.Domain.Entities;
using Atlas.Domain.Enums;
using Atlas.Tests.Unit.Fakes;

namespace Atlas.Tests.Unit.Features.AzureDevOps;

public sealed class RunAzureSyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenConnectionMissing_ReturnsDisabledMessage()
    {
        RunAzureSyncCommandHandler handler = CreateHandler(
            connections: new FakeAzureConnectionRepository(),
            client: new StubAzureDevOpsClient());

        RunAzureSyncResult result = await handler.Handle(new RunAzureSyncCommand(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ItemsUpserted);
        Assert.Contains("not configured", result.Error);
    }

    [Fact]
    public async Task Handle_WhenConnectionDisabled_ReturnsDisabledMessage()
    {
        var connections = new FakeAzureConnectionRepository
        {
            Singleton = NewConnection(isEnabled: false)
        };

        RunAzureSyncCommandHandler handler = CreateHandler(connections, new StubAzureDevOpsClient());
        RunAzureSyncResult result = await handler.Handle(new RunAzureSyncCommand(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ItemsFetched);
        Assert.Contains("disabled", result.Error);
    }

    [Fact]
    public async Task Handle_FirstRun_UpsertsWorkItemsAndAdvancesWatermark()
    {
        AzureConnection connection = NewConnection();
        var connections = new FakeAzureConnectionRepository { Singleton = connection };
        var workItems = new FakeAzureWorkItemRepository();
        var syncStates = new FakeAzureSyncStateRepository();
        var clock = new FakeDateTimeProvider();
        DateTimeOffset changed = clock.UtcNow.AddDays(-1);

        var client = new StubAzureDevOpsClient
        {
            WorkItemIds = [101, 102],
            WorkItems =
            [
                NewDetails(101, 3, changed, "Fix login"),
                NewDetails(102, 1, changed.AddHours(1), "Add tests")
            ]
        };

        RunAzureSyncCommandHandler handler = CreateHandler(connections, client, workItems, syncStates, clock);
        RunAzureSyncResult result = await handler.Handle(new RunAzureSyncCommand(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.ItemsFetched);
        Assert.Equal(2, result.ItemsUpserted);
        Assert.Equal(2, workItems.Items.Count);
        Assert.Equal(102, result.LastWorkItemId);
        Assert.Equal(changed.AddHours(1), result.LastChangedUtc);

        AzureSyncState? state = await syncStates.GetByConnectionIdAsync(connection.Id, CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(SyncRunStatus.Succeeded, state.LastRunStatus);
        Assert.Null(state.LastError);
        Assert.Contains("UNDER", client.CapturedWiql[0]);
    }

    [Fact]
    public async Task Handle_IncrementalRun_BuildsWiqlWithChangedDateAndId()
    {
        AzureConnection connection = NewConnection();
        var connections = new FakeAzureConnectionRepository { Singleton = connection };
        var syncStates = new FakeAzureSyncStateRepository();
        DateTimeOffset watermark = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        syncStates.Seed(new AzureSyncState
        {
            Id = Guid.NewGuid(),
            AzureConnectionId = connection.Id,
            LastSuccessfulChangedUtc = watermark,
            LastSuccessfulWorkItemId = 50,
            LastRunStatus = SyncRunStatus.Succeeded
        });

        var client = new StubAzureDevOpsClient { WorkItemIds = [] };
        RunAzureSyncCommandHandler handler = CreateHandler(connections, client, syncStates: syncStates);

        await handler.Handle(new RunAzureSyncCommand(), CancellationToken.None);

        Assert.Single(client.CapturedWiql);
        Assert.Contains(
            "(([System.ChangedDate] > '2026-01-10') OR ([System.ChangedDate] = '2026-01-10' AND [System.Id] > 50))",
            client.CapturedWiql[0]);
    }

    [Fact]
    public async Task Handle_WhenWorkItemExists_UpdatesInsteadOfDuplicating()
    {
        AzureConnection connection = NewConnection();
        var connections = new FakeAzureConnectionRepository { Singleton = connection };
        var workItems = new FakeAzureWorkItemRepository();
        var existingId = Guid.NewGuid();
        workItems.Items.Add(new AzureWorkItem
        {
            Id = existingId,
            AzureConnectionId = connection.Id,
            WorkItemId = 101,
            Rev = 1,
            Title = "Old title",
            State = "New",
            WorkItemType = "Bug",
            AreaPath = "Atlas\\Core",
            IterationPath = "Sprint 1",
            Url = "https://dev.azure.com/wi/101",
            ChangedDateUtc = DateTimeOffset.UtcNow.AddDays(-2)
        });

        DateTimeOffset changed = DateTimeOffset.UtcNow;
        var client = new StubAzureDevOpsClient
        {
            WorkItemIds = [101],
            WorkItems = [NewDetails(101, 5, changed, "Updated title")]
        };

        RunAzureSyncCommandHandler handler = CreateHandler(connections, client, workItems);
        RunAzureSyncResult result = await handler.Handle(new RunAzureSyncCommand(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(workItems.Items);
        Assert.Equal(existingId, workItems.Items[0].Id);
        Assert.Equal(5, workItems.Items[0].Rev);
        Assert.Equal("Updated title", workItems.Items[0].Title);
    }

    [Fact]
    public async Task Handle_WhenClientThrows_MarksFailedAndReturnsError()
    {
        AzureConnection connection = NewConnection();
        var connections = new FakeAzureConnectionRepository { Singleton = connection };
        var syncStates = new FakeAzureSyncStateRepository();
        var client = new StubAzureDevOpsClient
        {
            QueryException = new InvalidOperationException("ADO unavailable")
        };

        RunAzureSyncCommandHandler handler = CreateHandler(connections, client, syncStates: syncStates);
        RunAzureSyncResult result = await handler.Handle(new RunAzureSyncCommand(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("ADO unavailable", result.Error);

        AzureSyncState? state = await syncStates.GetByConnectionIdAsync(connection.Id, CancellationToken.None);
        Assert.NotNull(state);
        Assert.Equal(SyncRunStatus.Failed, state.LastRunStatus);
        Assert.Equal("ADO unavailable", state.LastError);
    }

    private static RunAzureSyncCommandHandler CreateHandler(
        FakeAzureConnectionRepository connections,
        StubAzureDevOpsClient client,
        FakeAzureWorkItemRepository? workItems = null,
        FakeAzureSyncStateRepository? syncStates = null,
        FakeDateTimeProvider? clock = null) =>
        new(
            client,
            connections,
            syncStates ?? new FakeAzureSyncStateRepository(),
            workItems ?? new FakeAzureWorkItemRepository(),
            new FakeSettingsRepository(),
            new FakeUnitOfWork(),
            clock ?? new FakeDateTimeProvider());

    private static AzureConnection NewConnection(bool isEnabled = true) => new()
    {
        Id = Guid.NewGuid(),
        Organization = "contoso",
        Project = "Atlas",
        ProjectId = "proj-1",
        AreaPath = "Atlas\\Core",
        TeamName = "Core",
        TeamId = "team-1",
        IsEnabled = isEnabled
    };

    private static AzureWorkItemDetails NewDetails(int id, int rev, DateTimeOffset changed, string title) =>
        new(id, rev, changed, title, "Active", "Bug", "Atlas\\Core", "Sprint 1", "ada@example.com", $"https://dev.azure.com/wi/{id}");
}
