using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.Settings;

public sealed record UpdateSettingsRequest(
    int StaleDays,
    bool DefaultAiManualOnly,
    Theme Theme,
    string? AzureDevOpsBaseUrl);

