using Atlas.Application.Abstractions.AzureDevOps;

namespace Atlas.Tests.Common.Fakes;

public sealed class FakeAzureDevOpsClient : IAzureDevOpsClient
{
    public IReadOnlyList<AzureProjectSummary> Projects { get; set; } =
    [
        new("proj-1", "Atlas"),
        new("proj-2", "Platform")
    ];

    public IReadOnlyList<AzureTeamSummary> Teams { get; set; } =
    [
        new("team-1", "Core"),
        new("team-2", "Growth")
    ];

    public IReadOnlyList<AzureUserSummary> Users { get; set; } =
    [
        new("Ada Lovelace", "ada@example.com", "user-1"),
        new("Grace Hopper", "grace@example.com", "user-2")
    ];

    public AzureTeamAreaPaths TeamAreaPaths { get; set; } = new(
        "Atlas\\Core",
        [new AzureTeamAreaPath("Atlas\\Core", true), new AzureTeamAreaPath("Atlas\\Platform", false)]);

    public IReadOnlyList<int> WorkItemIds { get; set; } = [101, 102, 103];

    public IReadOnlyList<AzureWorkItemDetails> WorkItems { get; set; } =
    [
        new(
            101, 1, DateTimeOffset.UtcNow, "Fix login", "Active", "Bug",
            "Atlas\\Core", "Atlas\\Sprint 1", "ada@example.com", "https://dev.azure.com/wi/101"),
        new(
            102, 1, DateTimeOffset.UtcNow, "Add tests", "New", "Task",
            "Atlas\\Platform", "Atlas\\Sprint 1", "grace@example.com", "https://dev.azure.com/wi/102")
    ];

    public Task<IReadOnlyList<AzureProjectSummary>> ListProjectsAsync(
        string baseUrl,
        string organization,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Projects);

    public Task<IReadOnlyList<AzureTeamSummary>> ListTeamsAsync(
        string baseUrl,
        string organization,
        string projectId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Teams);

    public Task<IReadOnlyList<AzureUserSummary>> ListUsersAsync(
        string baseUrl,
        string organization,
        string projectId,
        string teamId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users);

    public Task<AzureTeamAreaPaths> GetTeamAreaPathsAsync(
        string baseUrl,
        string organization,
        string projectId,
        string teamName,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeamAreaPaths);

    public Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(
        string baseUrl,
        string organization,
        string project,
        string wiql,
        int? top = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(top is { } limit ? WorkItemIds.Take(limit).ToList() : WorkItemIds);

    public Task<IReadOnlyList<AzureWorkItemDetails>> GetWorkItemsAsync(
        string baseUrl,
        string organization,
        string project,
        IReadOnlyList<int> workItemIds,
        CancellationToken cancellationToken = default)
    {
        var lookup = WorkItems.ToDictionary(w => w.Id);
        IReadOnlyList<AzureWorkItemDetails> result = workItemIds
            .Where(lookup.ContainsKey)
            .Select(id => lookup[id])
            .ToList();
        return Task.FromResult(result);
    }
}
