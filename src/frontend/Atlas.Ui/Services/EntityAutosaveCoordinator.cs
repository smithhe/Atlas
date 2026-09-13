namespace Atlas.Ui.Services;

/// <summary>
/// Per-entity autosave: debounced text edits, serialized HTTP writes, and failure recovery.
/// Each save records a generation-scoped edit transform; successful persists clear pending edits
/// through that generation. On refetch failure, pending edits other than the failed generation are
/// replayed on top of the refetched entity so newer debounced text survives a rejected link.
/// Without refetch, a generation-gated snapshot rollback applies when this save is still latest.
/// </summary>
public sealed class EntityAutosaveCoordinator<T> where T : class
{
    private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromMilliseconds(400);

    private readonly TimeSpan _debounceDelay;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly List<(long Generation, Func<T, T> Edit)> _pendingEdits = [];
    private readonly object _pendingEditsLock = new();
    private long _editGeneration;
    private CancellationTokenSource? _debounceCts;

    public EntityAutosaveCoordinator(TimeSpan? debounceDelay = null)
    {
        _debounceDelay = debounceDelay ?? DefaultDebounceDelay;
    }

    public EntitySaveState State { get; private set; } = EntitySaveState.Idle;

    public event Action? StateChanged;

    internal int PendingEditCount
    {
        get
        {
            lock (_pendingEditsLock)
            {
                return _pendingEdits.Count;
            }
        }
    }

    /// <summary>
    /// Applies an optimistic edit and persists it. <paramref name="persistLatestAsync"/> must write the
    /// complete latest cached entity; a successful persist clears all pending edits at or before the
    /// generation observed before persisting. On failure with <paramref name="refetchAsync"/>, newer and
    /// older pending edits that were not persisted remain in the cache unsaved and <see cref="State"/>
    /// is <see cref="EntitySaveState.Failed"/> until a later edit persists them.
    /// </summary>
    public async Task SaveAsync(
        Func<T, T> edit,
        Func<T?> readCurrent,
        Action<T> applyToCache,
        Func<CancellationToken, Task> persistLatestAsync,
        bool debounce,
        Func<CancellationToken, Task>? refetchAsync = null,
        CancellationToken cancellationToken = default)
    {
        long generation = Interlocked.Increment(ref _editGeneration);
        T current = readCurrent()
            ?? throw new InvalidOperationException("Entity is not in the cache.");
        T rollbackSnapshot = current;
        T optimistic = edit(current);
        applyToCache(optimistic);
        AddPendingEdit(generation, edit);
        SetState(EntitySaveState.Saving);
        CancelDebounce();

        if (debounce)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _debounceCts = cts;
            try
            {
                await Task.Delay(_debounceDelay, cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // A newer edit cancelled this debounce; the newer edit owns State and will persist.
                return;
            }
            catch (OperationCanceledException)
            {
                RemovePendingEdit(generation);
                if (generation == Volatile.Read(ref _editGeneration))
                {
                    SetState(EntitySaveState.Idle);
                }

                throw;
            }
        }

        try
        {
            await _writeGate.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            RemovePendingEdit(generation);
            if (generation == Volatile.Read(ref _editGeneration))
            {
                SetState(EntitySaveState.Idle);
            }

            throw;
        }

        try
        {
            while (true)
            {
                long generationBeforePersist = Volatile.Read(ref _editGeneration);
                await persistLatestAsync(cancellationToken);

                if (Volatile.Read(ref _editGeneration) == generationBeforePersist)
                {
                    ClearPendingEditsThrough(generationBeforePersist);
                    SetState(EntitySaveState.Saved);
                    break;
                }
            }
        }
        catch (Exception)
        {
            bool recovered = false;
            if (refetchAsync is not null)
            {
                try
                {
                    await refetchAsync(cancellationToken);
                    ReplayPendingEditsAfterFailure(generation, readCurrent, applyToCache);
                    recovered = true;
                }
                catch
                {
                    // Fall back to snapshot rollback below.
                }
            }

            RemovePendingEdit(generation);

            if (!recovered && generation >= Volatile.Read(ref _editGeneration))
            {
                applyToCache(rollbackSnapshot);
            }

            SetState(EntitySaveState.Failed);
            throw;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private void AddPendingEdit(long generation, Func<T, T> edit)
    {
        lock (_pendingEditsLock)
        {
            _pendingEdits.Add((generation, edit));
        }
    }

    private void RemovePendingEdit(long generation)
    {
        lock (_pendingEditsLock)
        {
            _pendingEdits.RemoveAll(pending => pending.Generation == generation);
        }
    }

    private void ClearPendingEditsThrough(long generationBeforePersist)
    {
        lock (_pendingEditsLock)
        {
            _pendingEdits.RemoveAll(pending => pending.Generation <= generationBeforePersist);
        }
    }

    private void ReplayPendingEditsAfterFailure(
        long failedGeneration,
        Func<T?> readCurrent,
        Action<T> applyToCache)
    {
        List<(long Generation, Func<T, T> Edit)> toReplay;
        lock (_pendingEditsLock)
        {
            toReplay = _pendingEdits
                .Where(pending => pending.Generation != failedGeneration)
                .OrderBy(pending => pending.Generation)
                .ToList();
        }

        foreach ((long _, Func<T, T> pendingEdit) in toReplay)
        {
            T? replayBase = readCurrent();
            if (replayBase is null)
            {
                break;
            }

            applyToCache(pendingEdit(replayBase));
        }
    }

    private void CancelDebounce()
    {
        if (_debounceCts is null)
        {
            return;
        }

        try
        {
            _debounceCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        _debounceCts.Dispose();
        _debounceCts = null;
    }

    private void SetState(EntitySaveState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke();
    }
}
