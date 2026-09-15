using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface IAzureDevOpsService
    {
        Task<AzureConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

        Task<AzureConnection?> TryGetConnectionAsync(CancellationToken cancellationToken = default);

        Task UpdateConnectionAsync(AzureUpdateConnection connection, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AzureProject>> ListProjectsAsync(
            string organization,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AzureTeam>> ListTeamsAsync(
            string organization,
            string projectId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AzureUser>> ListUsersAsync(
            string organization,
            string projectId,
            string teamId,
            CancellationToken cancellationToken = default);

        Task<AzureTeamAreaPaths> ListTeamAreaPathsAsync(
            string organization,
            string projectId,
            string teamName,
            CancellationToken cancellationToken = default);

        Task ImportTeamAsync(IReadOnlyList<AzureUser> users, CancellationToken cancellationToken = default);

        Task<ImportProductOwnersResult> ImportProductOwnersAsync(
            IReadOnlyList<AzureUser> users,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AzureUser>> ListImportedUsersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AzureImportWorkItem>> ListImportWorkItemsAsync(
            CancellationToken cancellationToken = default);

        Task LinkWorkItemsAsync(LinkAzureWorkItemsRequest request, CancellationToken cancellationToken = default);

        Task<AzureSyncState> GetSyncStateAsync(CancellationToken cancellationToken = default);

        Task<AzureSyncResult> RunSyncAsync(CancellationToken cancellationToken = default);
    }
}
