using Atlas.Api.DTOs.Settings;
using Atlas.Domain.Entities;

namespace Atlas.Api.Mappers;

internal static class SettingsMapper
{
    public static SettingsDto ToDto(Atlas.Domain.Entities.Settings s)
    {
        return new SettingsDto(s.StaleDays, s.DefaultAiManualOnly, s.Theme, s.AzureDevOpsBaseUrl);
    }
}

