namespace Atlas.Api.Ai;

public sealed class AiExecutionGate
{
    private readonly SemaphoreSlim _semaphore;

    public AiExecutionGate(int maxConcurrency)
    {
        _semaphore = new SemaphoreSlim(Math.Max(1, maxConcurrency), Math.Max(1, maxConcurrency));
    }

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Release(_semaphore);
    }

    private sealed class Release : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _released;

        public Release(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (_released) return;
            _released = true;
            _semaphore.Release();
        }
    }
}

