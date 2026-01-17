namespace Atlas.AzureDevOps;

public sealed class AzureDevOpsClient : IAzureDevOpsClient
{
    private static readonly IReadOnlyList<string> WorkItemFields =
    [
        "System.Id",
        "System.Title",
        "System.State",
        "System.WorkItemType",
        "System.AreaPath",
        "System.IterationPath",
        "System.ChangedDate",
        "System.AssignedTo"
    ];

    private readonly HttpClient _httpClient;
    private readonly string _patToken;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AzureDevOpsClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var pat = configuration["AzureDevopsToken"];
        if (string.IsNullOrWhiteSpace(pat))
        {
            throw new InvalidOperationException("Azure DevOps PAT is not configured. Set AzureDevOps:Pat in user-secrets.");
        }

        _patToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
    }

    public async Task<IReadOnlyList<AzureProjectSummary>> ListProjectsAsync(string baseUrl, string organization, CancellationToken cancellationToken = default)
    {
        var url = $"{NormalizeBaseUrl(baseUrl)}/{organization}/_apis/projects?api-version=7.1";
        using var req = CreateRequest(HttpMethod.Get, url);
        using var res = await _httpClient.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<ProjectListResponse>(_jsonOptions, cancellationToken);
        return payload?.Value?
            .Select(p => new AzureProjectSummary(p.Id, p.Name))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<AzureTeamSummary>> ListTeamsAsync(string baseUrl, string organization, string projectId, CancellationToken cancellationToken = default)
    {
        var url = $"{NormalizeBaseUrl(baseUrl)}/{organization}/_apis/projects/{projectId}/teams?api-version=7.1";
        using var req = CreateRequest(HttpMethod.Get, url);
        using var res = await _httpClient.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<TeamListResponse>(_jsonOptions, cancellationToken);
        return payload?.Value?
            .Select(t => new AzureTeamSummary(t.Id, t.Name))
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<AzureUserSummary>> ListUsersAsync(string baseUrl, string organization, string projectId, string teamId, CancellationToken cancellationToken = default)
    {
        var url = $"{NormalizeBaseUrl(baseUrl)}/{organization}/_apis/projects/{projectId}/teams/{teamId}/members?api-version=7.1";
        using var req = CreateRequest(HttpMethod.Get, url);
        using var res = await _httpClient.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<TeamMembersResponse>(_jsonOptions, cancellationToken);
        return payload?.Value?
            .Select(m => m.Identity)
            .Where(i => i is not null)
            .Select(i => new AzureUserSummary(i!.DisplayName ?? i.UniqueName ?? string.Empty, i.UniqueName ?? string.Empty, i.Descriptor))
            .Where(u => !string.IsNullOrWhiteSpace(u.UniqueName))
            .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(string baseUrl, string organization, string wiql, CancellationToken cancellationToken = default)
    {
        var url = $"{NormalizeBaseUrl(baseUrl)}/{organization}/_apis/wit/wiql?api-version=7.1";
        using var req = CreateRequest(HttpMethod.Post, url);
        req.Content = new StringContent(JsonSerializer.Serialize(new { query = wiql }), Encoding.UTF8, "application/json");

        using var res = await _httpClient.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<WiqlResponse>(_jsonOptions, cancellationToken);
        return payload?.WorkItems?.Select(x => x.Id).ToList() ?? [];
    }

    public async Task<IReadOnlyList<AzureWorkItemDetails>> GetWorkItemsAsync(string baseUrl, string organization, IReadOnlyList<int> workItemIds, CancellationToken cancellationToken = default)
    {
        if (workItemIds.Count == 0) return [];

        var url = $"{NormalizeBaseUrl(baseUrl)}/{organization}/_apis/wit/workitemsbatch?api-version=7.1";
        using var req = CreateRequest(HttpMethod.Post, url);
        req.Content = new StringContent(
            JsonSerializer.Serialize(new { ids = workItemIds, fields = WorkItemFields }),
            Encoding.UTF8,
            "application/json");

        using var res = await _httpClient.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var payload = await res.Content.ReadFromJsonAsync<WorkItemBatchResponse>(_jsonOptions, cancellationToken);
        if (payload?.Value is null) return [];

        var results = new List<AzureWorkItemDetails>(payload.Value.Count);
        foreach (var item in payload.Value)
        {
            if (!item.Fields.TryGetValue("System.ChangedDate", out var changed)) continue;

            var changedDate = DateTimeOffset.Parse(changed.GetString() ?? string.Empty, CultureInfo.InvariantCulture);
            var assigned = TryGetAssignedToUniqueName(item.Fields);

            results.Add(new AzureWorkItemDetails(
                item.Id,
                item.Rev,
                changedDate,
                GetString(item.Fields, "System.Title"),
                GetString(item.Fields, "System.State"),
                GetString(item.Fields, "System.WorkItemType"),
                GetString(item.Fields, "System.AreaPath"),
                GetString(item.Fields, "System.IterationPath"),
                assigned,
                item.Url ?? string.Empty));
        }

        return results;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", _patToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return req;
    }

    private static string NormalizeBaseUrl(string baseUrl) => baseUrl.TrimEnd('/');

    private static string GetString(Dictionary<string, JsonElement> fields, string key)
    {
        return fields.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string? TryGetAssignedToUniqueName(Dictionary<string, JsonElement> fields)
    {
        if (!fields.TryGetValue("System.AssignedTo", out var assigned) || assigned.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (assigned.TryGetProperty("uniqueName", out var uniqueName) && uniqueName.ValueKind == JsonValueKind.String)
        {
            return uniqueName.GetString();
        }

        if (assigned.TryGetProperty("principalName", out var principalName) && principalName.ValueKind == JsonValueKind.String)
        {
            return principalName.GetString();
        }

        return null;
    }

    private sealed record ProjectListResponse([property: JsonPropertyName("value")] List<ProjectItem> Value);
    private sealed record ProjectItem([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("name")] string Name);

    private sealed record TeamListResponse([property: JsonPropertyName("value")] List<TeamListItem> Value);
    private sealed record TeamListItem([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("name")] string Name);

    private sealed record TeamMembersResponse([property: JsonPropertyName("value")] List<TeamMemberItem> Value);
    private sealed record TeamMemberItem([property: JsonPropertyName("identity")] TeamMemberIdentity? Identity);
    private sealed record TeamMemberIdentity(
        [property: JsonPropertyName("displayName")] string? DisplayName,
        [property: JsonPropertyName("uniqueName")] string? UniqueName,
        [property: JsonPropertyName("descriptor")] string? Descriptor);

    private sealed record WiqlResponse([property: JsonPropertyName("workItems")] List<WiqlWorkItem> WorkItems);
    private sealed record WiqlWorkItem([property: JsonPropertyName("id")] int Id);

    private sealed record WorkItemBatchResponse([property: JsonPropertyName("value")] List<WorkItemBatchItem> Value);
    private sealed record WorkItemBatchItem(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("rev")] int Rev,
        [property: JsonPropertyName("fields")] Dictionary<string, JsonElement> Fields,
        [property: JsonPropertyName("url")] string? Url);
}
