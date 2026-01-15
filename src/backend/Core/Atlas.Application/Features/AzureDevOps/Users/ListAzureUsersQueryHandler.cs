using Atlas.Application.Abstractions.AzureDevOps;
using Atlas.Application.Abstractions.Persistence;

namespace Atlas.Application.Features.AzureDevOps.Users;

public sealed class ListAzureUsersQueryHandler : IRequestHandler<ListAzureUsersQuery, IReadOnlyList<AzureUserSummary>>
{
    private readonly IAzureDevOpsClient _client;
    private readonly ISettingsRepository _settings;

    public ListAzureUsersQueryHandler(IAzureDevOpsClient client, ISettingsRepository settings)
    {
        _client = client;
        _settings = settings;
    }

    public async Task<IReadOnlyList<AzureUserSummary>> Handle(ListAzureUsersQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settings.GetSingletonAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(settings?.AzureDevOpsBaseUrl)
            ? "https://dev.azure.com"
            : settings!.AzureDevOpsBaseUrl!.Trim();

        return await _client.ListUsersAsync(baseUrl, request.Organization, cancellationToken);
    }
}
