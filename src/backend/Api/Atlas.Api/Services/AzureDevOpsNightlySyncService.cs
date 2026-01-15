using Atlas.Application.Abstractions.Persistence;
using Atlas.Application.Abstractions.Time;
using Atlas.Application.Features.AzureDevOps.RunAzureSync;

namespace Atlas.Api.Services;

public sealed class AzureDevOpsNightlySyncService : BackgroundService
{
    private static readonly SemaphoreSlim SyncGate = new(1, 1);
    private static readonly TimeSpan TargetLocalTime = new(2, 0, 0);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeProvider _clock;

    public AzureDevOpsNightlySyncService(IServiceScopeFactory scopeFactory, IDateTimeProvider clock)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunUtc = GetNextRunUtc(_clock.UtcNow, TargetLocalTime);
            var delay = nextRunUtc - _clock.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            await RunSyncOnce(stoppingToken);
        }
    }

    private async Task RunSyncOnce(CancellationToken stoppingToken)
    {
        if (!await SyncGate.WaitAsync(0, stoppingToken)) return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var connections = scope.ServiceProvider.GetRequiredService<IAzureConnectionRepository>();
            var syncStates = scope.ServiceProvider.GetRequiredService<IAzureSyncStateRepository>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var connection = await connections.GetSingletonAsync(stoppingToken);
            if (connection is null || !connection.IsEnabled) return;

            var state = await syncStates.GetByConnectionIdAsync(connection.Id, stoppingToken);
            if (state?.LastRunStatus == Atlas.Domain.Enums.SyncRunStatus.Running &&
                state.LastAttemptedAtUtc.HasValue &&
                state.LastAttemptedAtUtc.Value > _clock.UtcNow.AddHours(-2))
            {
                return;
            }

            await mediator.Send(new RunAzureSyncCommand(), stoppingToken);
        }
        finally
        {
            SyncGate.Release();
        }
    }

    private static DateTimeOffset GetNextRunUtc(DateTimeOffset nowUtc, TimeSpan targetLocalTime)
    {
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, TimeZoneInfo.Local);
        var localTarget = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            targetLocalTime.Hours,
            targetLocalTime.Minutes,
            targetLocalTime.Seconds,
            localNow.Offset);

        if (localNow >= localTarget)
        {
            localTarget = localTarget.AddDays(1);
        }

        return localTarget.ToUniversalTime();
    }
}
