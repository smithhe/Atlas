using Atlas.Ui.Models;

namespace Atlas.Ui.Contracts
{
    public interface ITeamNoteService
    {
        Task<TeamNote> AddAsync(
            Guid memberId,
            NoteTag tag,
            string text,
            string? title,
            string? adoWorkItemId,
            string? prUrl,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(Guid memberId, TeamNote note, CancellationToken cancellationToken = default);
    }
}
