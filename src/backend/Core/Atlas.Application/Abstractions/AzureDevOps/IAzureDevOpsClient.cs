namespace Atlas.Application.Abstractions.AzureDevOps;

public interface IAzureDevOpsClient
{
    Task<IReadOnlyList<AzureProjectSummary>> ListProjectsAsync(string baseUrl, string organization, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AzureUserSummary>> ListUsersAsync(string baseUrl, string organization, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(string baseUrl, string organization, string wiql, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AzureWorkItemDetails>> GetWorkItemsAsync(string baseUrl, string organization, IReadOnlyList<int> workItemIds, CancellationToken cancellationToken = default);
}
