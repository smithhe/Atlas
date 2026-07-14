using System.Text;
using Atlas.Application.Abstractions.Ai;
using Atlas.Application.Abstractions.Persistence;
using Atlas.Domain.Entities;

namespace Atlas.Application.Features.Ai.Context;

public sealed class SettingsPromptContextBuilder : IAiPromptContextBuilder
{
    private readonly ISettingsRepository _settings;
    private readonly IAzureConnectionRepository _azureConnections;

    public SettingsPromptContextBuilder(ISettingsRepository settings, IAzureConnectionRepository azureConnections)
    {
        _settings = settings;
        _azureConnections = azureConnections;
    }

    public AiViewScope Scope => AiViewScope.Settings;

    public async Task<string> BuildContextAsync(AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        Domain.Entities.Settings? settings = await _settings.GetSingletonAsync(cancellationToken);
        AzureConnection? connection = await _azureConnections.GetSingletonAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Settings context:");
        sb.AppendLine("- Help the user with Atlas configuration questions (OpenAI, Azure DevOps, theme, stale thresholds).");
        sb.AppendLine("- Never reveal secrets, PATs, or API keys.");

        if (settings is not null)
        {
            sb.AppendLine("- Current settings:");
            sb.AppendLine($"  - Stale days: {settings.StaleDays}");
            sb.AppendLine($"  - Default AI manual only: {settings.DefaultAiManualOnly}");
            sb.AppendLine($"  - Theme: {settings.Theme}");
            sb.AppendLine($"  - Azure DevOps base URL configured: {!string.IsNullOrWhiteSpace(settings.AzureDevOpsBaseUrl)}");
        }
        else
        {
            sb.AppendLine("- Current settings: not initialized yet.");
        }

        if (connection is not null)
        {
            sb.AppendLine("- Azure DevOps connection:");
            sb.AppendLine($"  - Enabled: {connection.IsEnabled}");
            sb.AppendLine($"  - Organization set: {!string.IsNullOrWhiteSpace(connection.Organization)}");
            sb.AppendLine($"  - Project set: {!string.IsNullOrWhiteSpace(connection.Project)}");
            sb.AppendLine($"  - Area path set: {!string.IsNullOrWhiteSpace(connection.AreaPath)}");
            sb.AppendLine($"  - Team set: {!string.IsNullOrWhiteSpace(connection.TeamName) || !string.IsNullOrWhiteSpace(connection.TeamId)}");
            if (!string.IsNullOrWhiteSpace(connection.Organization))
            {
                sb.AppendLine($"  - Organization: {connection.Organization}");
            }

            if (!string.IsNullOrWhiteSpace(connection.Project))
            {
                sb.AppendLine($"  - Project: {connection.Project}");
            }

            if (!string.IsNullOrWhiteSpace(connection.AreaPath))
            {
                sb.AppendLine($"  - Area path: {connection.AreaPath}");
            }

            if (!string.IsNullOrWhiteSpace(connection.TeamName))
            {
                sb.AppendLine($"  - Team name: {connection.TeamName}");
            }
        }
        else
        {
            sb.AppendLine("- Azure DevOps connection: not configured.");
        }

        sb.AppendLine("- OpenAI: required for AI panel; configure via OpenAI__ApiKey env var, user-secrets, or Compose .env.");

        return sb.ToString().TrimEnd();
    }
}
