using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.AzureDevOps.Areas;

public sealed class ListAzureTeamAreaPathsQueryHandler : IRequestHandler<ListAzureTeamAreaPathsQuery, AzureTeamAreaPaths>
{
    private readonly IAzureDevOpsClient _client;
    private readonly ISettingsRepository _settings;

    public ListAzureTeamAreaPathsQueryHandler(IAzureDevOpsClient client, ISettingsRepository settings)
    {
        _client = client;
        _settings = settings;
    }

    public async Task<AzureTeamAreaPaths> Handle(ListAzureTeamAreaPathsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetSingletonAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(settings?.AzureDevOpsBaseUrl)
            ? "https://dev.azure.com"
            : settings!.AzureDevOpsBaseUrl!.Trim();

        return await _client.GetTeamAreaPathsAsync(baseUrl, request.Organization, request.ProjectId, request.TeamName, cancellationToken);
    }
}

