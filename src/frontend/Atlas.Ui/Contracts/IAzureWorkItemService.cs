using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface IAzureWorkItemService
    {
        Task<WorkItemNote> AddLocalNoteAsync(
            Guid teamMemberId,
            int workItemId,
            string text,
            CancellationToken cancellationToken = default);

        Task SetProjectAsync(
            Guid teamMemberId,
            string workItemId,
            Guid? projectId,
            CancellationToken cancellationToken = default);
    }
}
