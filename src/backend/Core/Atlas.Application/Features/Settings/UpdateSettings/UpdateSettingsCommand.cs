using Atlas.Domain.Enums;

namespace Atlas.Application.Features.Settings.UpdateSettings;

public sealed record UpdateSettingsCommand(
    int StaleDays,
    bool DefaultAiManualOnly,
    Theme Theme,
    string? AzureDevOpsBaseUrl) : IRequest<bool>;

