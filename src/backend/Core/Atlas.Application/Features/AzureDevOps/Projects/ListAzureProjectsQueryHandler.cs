using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.AzureDevOps.Projects;

public sealed class ListAzureProjectsQueryHandler : IRequestHandler<ListAzureProjectsQuery, IReadOnlyList<AzureProjectSummary>>
{
    private readonly IAzureDevOpsClient _client;
    private readonly ISettingsRepository _settings;

    public ListAzureProjectsQueryHandler(IAzureDevOpsClient client, ISettingsRepository settings)
    {
        _client = client;
        _settings = settings;
    }

    public async Task<IReadOnlyList<AzureProjectSummary>> Handle(ListAzureProjectsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetSingletonAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(settings?.AzureDevOpsBaseUrl)
            ? "https://dev.azure.com"
            : settings!.AzureDevOpsBaseUrl!.Trim();

        return await _client.ListProjectsAsync(baseUrl, request.Organization, cancellationToken);
    }
}
