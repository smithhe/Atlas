namespace Atlas.Application.Abstractions.AzureDevOps;

public interface IAzureDevOpsClient
{
    Task<IReadOnlyList<AzureProjectSummary>> ListProjectsAsync(string baseUrl, string organization, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AzureTeamSummary>> ListTeamsAsync(string baseUrl, string organization, string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AzureUserSummary>> ListUsersAsync(string baseUrl, string organization, string projectId, string teamId, CancellationToken cancellationToken = default);
    Task<AzureTeamAreaPaths> GetTeamAreaPathsAsync(string baseUrl, string organization, string projectId, string teamName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(
        string baseUrl,
        string organization,
        string project,
        string wiql,
        int? top = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AzureWorkItemDetails>> GetWorkItemsAsync(
        string baseUrl,
        string organization,
        string project,
        IReadOnlyList<int> workItemIds,
        CancellationToken cancellationToken = default);
}
