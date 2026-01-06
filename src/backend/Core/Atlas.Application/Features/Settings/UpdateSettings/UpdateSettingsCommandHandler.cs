using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Settings.UpdateSettings;

public sealed class UpdateSettingsCommandHandler : IRequestHandler<UpdateSettingsCommand, bool>
{
    private readonly ISettingsRepository _settings;
    private readonly IUnitOfWork _uow;

    public UpdateSettingsCommandHandler(ISettingsRepository settings, IUnitOfWork uow)
    {
        _settings = settings;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

        var existing = await _settings.GetSingletonAsync(cancellationToken);
        if (existing is null)
        {
            existing = new Atlas.Domain.Entities.Settings
            {
                Id = Guid.NewGuid(),
                StaleDays = 10,
                DefaultAiManualOnly = true,
                Theme = Theme.Dark,
                AzureDevOpsBaseUrl = null
            };
            await _settings.AddAsync(existing, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        existing.StaleDays = request.StaleDays;
        existing.DefaultAiManualOnly = request.DefaultAiManualOnly;
        existing.Theme = request.Theme;
        existing.AzureDevOpsBaseUrl = string.IsNullOrWhiteSpace(request.AzureDevOpsBaseUrl)
            ? null
            : request.AzureDevOpsBaseUrl.Trim();

        await _uow.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}

