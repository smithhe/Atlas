using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.AzureDevOps.Teams;

public sealed class ListAzureTeamsQueryHandler : IRequestHandler<ListAzureTeamsQuery, IReadOnlyList<AzureTeamSummary>>
{
    private readonly IAzureDevOpsClient _client;
    private readonly ISettingsRepository _settings;

    public ListAzureTeamsQueryHandler(IAzureDevOpsClient client, ISettingsRepository settings)
    {
        _client = client;
        _settings = settings;
    }

    public async Task<IReadOnlyList<AzureTeamSummary>> Handle(ListAzureTeamsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetSingletonAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(settings?.AzureDevOpsBaseUrl)
            ? "https://dev.azure.com"
            : settings!.AzureDevOpsBaseUrl!.Trim();

        return await _client.ListTeamsAsync(baseUrl, request.Organization, request.ProjectId, cancellationToken);
    }
}
