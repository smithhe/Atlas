using Atlas.Ui.Models;
using Atlas.Ui.Services;
using FluentAssertions;

namespace Atlas.Ui.Tests.Services;

public sealed class EntityAutosaveCoordinatorTests
{
    [Fact]
    public async Task SaveAsync_WhenOlderSaveFailsAfterNewerEdit_DoesNotRollbackNewerState()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(value: "initial");
        SemaphoreSlim gate = new(0, 1);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task firstPersist = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "A"),
            async _ =>
            {
                releaseFirst.SetResult();
                await gate.WaitAsync();
                throw new InvalidOperationException("save A failed");
            },
            debounce: false);

        await releaseFirst.Task;
        gate.Release();
        await Assert.ThrowsAsync<InvalidOperationException>(() => firstPersist);

        await SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "B"),
            _ => Task.CompletedTask,
            debounce: false);

        cache.Value.Value.Should().Be("B");
        coordinator.State.Should().Be(EntitySaveState.Saved);
    }

    [Fact]
    public async Task SaveAsync_WhenLatestSaveFails_RollsBackFailedVersion()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(value: "initial");

        await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "only"),
            _ => throw new InvalidOperationException("save failed"),
            debounce: false));

        cache.Value.Value.Should().Be("initial");
        coordinator.State.Should().Be(EntitySaveState.Failed);
    }

    [Fact]
    public async Task SaveAsync_WhenRefetchProvidedOnFailure_RefetchesInsteadOfSnapshotRollback()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(value: "initial");
        bool refetchCalled = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "failed"),
            _ => throw new InvalidOperationException("save failed"),
            debounce: false,
            refetchAsync: _ =>
            {
                refetchCalled = true;
                cache.Value = new SampleEntity { Id = cache.Value.Id, Value = "from-server" };
                return Task.CompletedTask;
            }));

        refetchCalled.Should().BeTrue();
        cache.Value.Value.Should().Be("from-server");
        coordinator.State.Should().Be(EntitySaveState.Failed);
    }

    [Fact]
    public async Task SaveAsync_WhenRefetchFailsWithNewerPendingEdit_ReplaysNewerEditOnRefetchedEntity()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new(TimeSpan.FromSeconds(30));
        CacheHolder cache = CreateCache(value: "initial", notes: "initial");
        TaskCompletionSource releaseFailedPersist = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int persistCount = 0;

        Task newerEdit = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, notes: "typed notes"),
            _ =>
            {
                persistCount++;
                return Task.CompletedTask;
            },
            debounce: true);

        Task failedSave = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "rejected link"),
            async _ =>
            {
                await releaseFailedPersist.Task;
                throw new InvalidOperationException("link failed");
            },
            debounce: false,
            refetchAsync: _ =>
            {
                cache.Value = new SampleEntity { Id = cache.Value.Id, Value = "from-server", Notes = "initial" };
                return Task.CompletedTask;
            });

        releaseFailedPersist.TrySetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => failedSave);
        await newerEdit;

        cache.Value.Value.Should().Be("from-server");
        cache.Value.Notes.Should().Be("typed notes");
        persistCount.Should().Be(0);
        coordinator.State.Should().Be(EntitySaveState.Failed);
        coordinator.PendingEditCount.Should().BeGreaterThan(0);

        await SaveAsync(
            coordinator,
            cache,
            entity => entity,
            _ => Task.CompletedTask,
            debounce: false);

        coordinator.State.Should().Be(EntitySaveState.Saved);
        coordinator.PendingEditCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveAsync_WhenFiveRapidDebouncedEdits_PersistsOnceAndClearsAllPending()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new(TimeSpan.FromMilliseconds(20));
        CacheHolder cache = CreateCache(value: "initial");
        TaskCompletionSource releasePersist = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int persistCount = 0;

        Task PersistAndCount(CancellationToken _)
        {
            persistCount++;
            return releasePersist.Task;
        }

        List<Task> saves =
        [
            SaveAsync(coordinator, cache, entity => Edit(entity, value: "1"), PersistAndCount, debounce: true),
            SaveAsync(coordinator, cache, entity => Edit(entity, value: "2"), PersistAndCount, debounce: true),
            SaveAsync(coordinator, cache, entity => Edit(entity, value: "3"), PersistAndCount, debounce: true),
            SaveAsync(coordinator, cache, entity => Edit(entity, value: "4"), PersistAndCount, debounce: true),
            SaveAsync(coordinator, cache, entity => Edit(entity, value: "5"), PersistAndCount, debounce: true),
        ];

        releasePersist.TrySetResult();
        await Task.WhenAll(saves);

        persistCount.Should().Be(1);
        cache.Value.Value.Should().Be("5");
        coordinator.PendingEditCount.Should().Be(0);
        coordinator.State.Should().Be(EntitySaveState.Saved);
    }

    [Fact]
    public async Task SaveAsync_WhenDebouncedThenImmediate_ImmediatePersistsOnceAndDebouncedReturnsWithoutPersisting()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new(TimeSpan.FromSeconds(30));
        CacheHolder cache = CreateCache(notes: "initial", status: "Todo");
        int persistCount = 0;

        Task debounced = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, notes: "typed notes"),
            _ =>
            {
                persistCount++;
                return Task.CompletedTask;
            },
            debounce: true);

        await SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, status: "InProgress"),
            _ =>
            {
                persistCount++;
                return Task.CompletedTask;
            },
            debounce: false);

        await debounced;

        persistCount.Should().Be(1);
        cache.Value.Notes.Should().Be("typed notes");
        cache.Value.Status.Should().Be("InProgress");
        coordinator.PendingEditCount.Should().Be(0);
        coordinator.State.Should().Be(EntitySaveState.Saved);
    }

    [Fact]
    public async Task SaveAsync_WhenPersistSucceeds_ClearsPendingEditsSoLaterFailureDoesNotReplayThem()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(value: "initial", notes: "initial");

        await SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "saved-on-server"),
            _ => Task.CompletedTask,
            debounce: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, notes: "rejected"),
            _ => throw new InvalidOperationException("save failed"),
            debounce: false,
            refetchAsync: _ =>
            {
                cache.Value = new SampleEntity { Id = cache.Value.Id, Value = "saved-on-server", Notes = "initial" };
                return Task.CompletedTask;
            }));

        cache.Value.Value.Should().Be("saved-on-server");
        cache.Value.Notes.Should().Be("initial");
        coordinator.State.Should().Be(EntitySaveState.Failed);
    }

    [Fact]
    public async Task SaveAsync_WhenRefetchThrows_FallsBackToSnapshotRollback()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(value: "initial");

        await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "failed"),
            _ => throw new InvalidOperationException("save failed"),
            debounce: false,
            refetchAsync: _ => throw new InvalidOperationException("refetch failed")));

        cache.Value.Value.Should().Be("initial");
        coordinator.State.Should().Be(EntitySaveState.Failed);
    }

    [Fact]
    public async Task SaveAsync_WhenCallerCancelledWaitingForWriteGate_ReturnsToIdleAndClearsPendingEdit()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(value: "initial");
        TaskCompletionSource releaseGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseBlockedPersist = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource cancellation = new();

        Task blockedSave = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "blocked"),
            async _ =>
            {
                releaseGate.TrySetResult();
                await releaseBlockedPersist.Task;
            },
            debounce: false);

        await releaseGate.Task;
        Task cancelledWait = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "cancelled"),
            _ => Task.CompletedTask,
            debounce: false,
            cancellationToken: cancellation.Token);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWait);

        coordinator.State.Should().Be(EntitySaveState.Idle);
        coordinator.PendingEditCount.Should().Be(1);
        cache.Value.Value.Should().Be("cancelled");

        releaseBlockedPersist.TrySetResult();
        await blockedSave;
        coordinator.State.Should().Be(EntitySaveState.Saved);
        coordinator.PendingEditCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveAsync_WhenCallerCancelledDuringDebounce_ReturnsToIdle()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new(TimeSpan.FromMilliseconds(100));
        CacheHolder cache = CreateCache(value: "initial");
        using CancellationTokenSource cancellation = new();

        Task save = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "draft"),
            _ => Task.CompletedTask,
            debounce: true,
            cancellationToken: cancellation.Token);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => save);
        coordinator.State.Should().Be(EntitySaveState.Idle);
    }

    [Fact]
    public async Task SaveAsync_SerializesWrites()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(value: "initial");
        List<int> order = [];
        TaskCompletionSource firstPersistEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task first = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "first"),
            async _ =>
            {
                order.Add(1);
                firstPersistEntered.TrySetResult();
                await releaseFirst.Task;
            },
            debounce: false);

        await firstPersistEntered.Task;
        Task second = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, value: "second"),
            _ =>
            {
                order.Add(2);
                return Task.CompletedTask;
            },
            debounce: false);

        releaseFirst.TrySetResult();
        await Task.WhenAll(first, second);
        order.Should().ContainInOrder(1, 2);
        order[^1].Should().Be(2);
        cache.Value.Value.Should().Be("second");
    }

    [Fact]
    public async Task SaveAsync_WhenDebouncedSaveCancelledByImmediateSave_DoesNotLeaveSavingForever()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(notes: "initial", status: "Todo");
        List<string> persisted = [];

        Task debounced = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, notes: "draft notes"),
            _ =>
            {
                persisted.Add($"{cache.Value.Notes}|{cache.Value.Status}");
                return Task.CompletedTask;
            },
            debounce: true);

        await SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, status: "InProgress"),
            _ =>
            {
                persisted.Add($"{cache.Value.Notes}|{cache.Value.Status}");
                return Task.CompletedTask;
            },
            debounce: false);

        await debounced;
        coordinator.State.Should().Be(EntitySaveState.Saved);
        persisted.Should().OnlyContain(x => x == "draft notes|InProgress");
    }

    [Fact]
    public async Task SaveAsync_WhenDebouncedThenImmediateInterleave_PersistsCombinedLatestState()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new(TimeSpan.FromMilliseconds(1));
        CacheHolder cache = CreateCache(notes: "initial", status: "Todo");
        List<string> persisted = [];

        Task debouncedNotes = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, notes: "typed notes"),
            _ =>
            {
                persisted.Add($"{cache.Value.Notes}|{cache.Value.Status}");
                return Task.CompletedTask;
            },
            debounce: true);

        await SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, status: "InProgress"),
            _ =>
            {
                persisted.Add($"{cache.Value.Notes}|{cache.Value.Status}");
                return Task.CompletedTask;
            },
            debounce: false);

        await debouncedNotes;
        cache.Value.Notes.Should().Be("typed notes");
        cache.Value.Status.Should().Be("InProgress");
        persisted.Should().OnlyContain(x => x == "typed notes|InProgress");
        coordinator.State.Should().Be(EntitySaveState.Saved);
    }

    [Fact]
    public async Task SaveAsync_WhenSeparateEntities_DoNotShareCoordinatorState()
    {
        EntityAutosaveCoordinator<SampleEntity> firstCoordinator = new();
        EntityAutosaveCoordinator<SampleEntity> secondCoordinator = new();
        CacheHolder firstCache = CreateCache(value: "first");
        CacheHolder secondCache = CreateCache(value: "second");

        await SaveAsync(
            firstCoordinator,
            firstCache,
            entity => Edit(entity, value: "first-save"),
            _ => Task.CompletedTask,
            debounce: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => SaveAsync(
            secondCoordinator,
            secondCache,
            entity => Edit(entity, value: "second-save"),
            _ => Task.FromException(new InvalidOperationException("second failed")),
            debounce: false));

        firstCache.Value.Value.Should().Be("first-save");
        secondCache.Value.Value.Should().Be("second");
        firstCoordinator.State.Should().Be(EntitySaveState.Saved);
        secondCoordinator.State.Should().Be(EntitySaveState.Failed);
    }

    [Fact]
    public async Task SaveAsync_WhenStaleDebouncedPersistWouldRevertStatus_PersistsLatestCacheState()
    {
        EntityAutosaveCoordinator<SampleEntity> coordinator = new();
        CacheHolder cache = CreateCache(notes: "initial", status: "Todo");
        List<string> persisted = [];
        TaskCompletionSource releaseDebounced = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseDebouncedPersist = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task debouncedNotes = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, notes: "stale snapshot notes"),
            async _ =>
            {
                releaseDebounced.TrySetResult();
                await releaseDebouncedPersist.Task;
                persisted.Add($"{cache.Value.Notes}|{cache.Value.Status}");
            },
            debounce: true);

        await releaseDebounced.Task;
        Task immediate = SaveAsync(
            coordinator,
            cache,
            entity => Edit(entity, status: "Done"),
            _ =>
            {
                persisted.Add($"{cache.Value.Notes}|{cache.Value.Status}");
                return Task.CompletedTask;
            },
            debounce: false);

        releaseDebouncedPersist.SetResult();
        await Task.WhenAll(debouncedNotes, immediate);
        cache.Value.Notes.Should().Be("stale snapshot notes");
        cache.Value.Status.Should().Be("Done");
        persisted.Should().OnlyContain(x => x == "stale snapshot notes|Done");
        coordinator.State.Should().Be(EntitySaveState.Saved);
    }

    private static CacheHolder CreateCache(
        string value = "",
        string notes = "",
        string status = "") =>
        new()
        {
            Value = new SampleEntity
            {
                Id = Guid.NewGuid(),
                Value = value,
                Notes = notes,
                Status = status
            }
        };

    private sealed class CacheHolder
    {
        public SampleEntity Value { get; set; } = null!;
    }

    private static Task SaveAsync(
        EntityAutosaveCoordinator<SampleEntity> coordinator,
        CacheHolder cache,
        Func<SampleEntity, SampleEntity> edit,
        Func<CancellationToken, Task> persistLatestAsync,
        bool debounce,
        Func<CancellationToken, Task>? refetchAsync = null,
        CancellationToken cancellationToken = default) =>
        coordinator.SaveAsync(
            edit,
            () => cache.Value,
            updated => cache.Value = Clone(updated),
            persistLatestAsync,
            debounce,
            refetchAsync,
            cancellationToken);

    private static SampleEntity Clone(SampleEntity entity) =>
        new()
        {
            Id = entity.Id,
            Value = entity.Value,
            Notes = entity.Notes,
            Status = entity.Status
        };

    private static SampleEntity Edit(
        SampleEntity current,
        string? value = null,
        string? notes = null,
        string? status = null) =>
        new()
        {
            Id = current.Id,
            Value = value ?? current.Value,
            Notes = notes ?? current.Notes,
            Status = status ?? current.Status
        };

    private sealed class SampleEntity
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
