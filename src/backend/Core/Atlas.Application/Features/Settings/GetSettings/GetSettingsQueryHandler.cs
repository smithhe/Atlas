using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Enums;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Settings.GetSettings;

public sealed class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, Atlas.Domain.Entities.Settings>
{
    private readonly ISettingsRepository _settings;
    private readonly IUnitOfWork _uow;

    public GetSettingsQueryHandler(ISettingsRepository settings, IUnitOfWork uow)
    {
        _settings = settings;
        _uow = uow;
    }

    public async Task<Atlas.Domain.Entities.Settings> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        var existing = await _settings.GetSingletonAsync(cancellationToken);
        if (existing is not null) return existing;

        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        existing = await _settings.GetSingletonAsync(cancellationToken);
        if (existing is not null)
        {
            await tx.CommitAsync(cancellationToken);
            return existing;
        }

        var created = new Atlas.Domain.Entities.Settings
        {
            Id = Guid.NewGuid(),
            StaleDays = 10,
            DefaultAiManualOnly = true,
            Theme = Theme.Dark,
            AzureDevOpsBaseUrl = null
        };

        await _settings.AddAsync(created, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return created;
    }
}

