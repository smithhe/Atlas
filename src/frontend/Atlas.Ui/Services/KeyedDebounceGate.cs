namespace Atlas.Ui.Services;

/// <summary>Keyed debounce with version gate and per-key semaphore for deferred HTTP work.</summary>
internal sealed class KeyedDebounceGate : IDisposable
{
    public readonly record struct Key(string Kind, Guid EntityId);

    private const int DebounceMs = 400;

    private readonly object _lock = new();
    private readonly Dictionary<Key, CancellationTokenSource> _debounceCts = new();
    private readonly Dictionary<Key, long> _versions = new();
    private readonly Dictionary<Key, SemaphoreSlim> _gates = new();
    private readonly CancellationToken _lifetime;

    public KeyedDebounceGate(CancellationToken lifetime) => _lifetime = lifetime;

    public long BumpVersion(Key key)
    {
        lock (_lock)
        {
            long version = _versions.GetValueOrDefault(key) + 1;
            _versions[key] = version;
            return version;
        }
    }

    public void DebounceKeyed(
        Key key,
        long version,
        long routeGeneration,
        Func<long> getRouteGeneration,
        Func<bool> isActive,
        Func<Func<Task>, Task> dispatch,
        Func<Task> persist)
    {
        if (_lifetime.IsCancellationRequested)
        {
            return;
        }

        CancelDebounce(key);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime);
        lock (_lock)
        {
            _debounceCts[key] = cts;
        }

        var capturedRouteGen = routeGeneration;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceMs, cts.Token);
                if (cts.Token.IsCancellationRequested
                    || !isActive()
                    || capturedRouteGen != getRouteGeneration())
                {
                    return;
                }

                await dispatch(() => RunPersistLoop(key, version, isActive, persist));
            }
            catch (TaskCanceledException)
            {
            }
        });
    }

    public void InvalidateKey(Key key)
    {
        BumpVersion(key);
        CancelDebounce(key);
    }

    public void InvalidateAll()
    {
        lock (_lock)
        {
            foreach (Key key in _versions.Keys.ToList())
            {
                _versions[key]++;
            }

            foreach (Key key in _debounceCts.Keys.ToList())
            {
                CancelDebounceUnlocked(key);
            }
        }
    }

    public void Dispose() => InvalidateAll();

    private async Task RunPersistLoop(Key key, long version, Func<bool> isActive, Func<Task> persist)
    {
        SemaphoreSlim gate = GetGate(key);
        try
        {
            await gate.WaitAsync(_lifetime);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            while (isActive())
            {
                if (!MatchesVersion(key, version))
                {
                    return;
                }

                await persist();

                if (!isActive())
                {
                    return;
                }

                if (MatchesVersion(key, version))
                {
                    return;
                }

                version = GetVersion(key);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private void CancelDebounce(Key key)
    {
        lock (_lock)
        {
            CancelDebounceUnlocked(key);
        }
    }

    private void CancelDebounceUnlocked(Key key)
    {
        if (_debounceCts.Remove(key, out CancellationTokenSource? cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private bool MatchesVersion(Key key, long version)
    {
        lock (_lock)
        {
            return _versions.TryGetValue(key, out long current) && current == version;
        }
    }

    private long GetVersion(Key key)
    {
        lock (_lock)
        {
            return _versions.GetValueOrDefault(key);
        }
    }

    private SemaphoreSlim GetGate(Key key)
    {
        lock (_lock)
        {
            if (!_gates.TryGetValue(key, out SemaphoreSlim? gate))
            {
                gate = new SemaphoreSlim(1, 1);
                _gates[key] = gate;
            }

            return gate;
        }
    }
}
