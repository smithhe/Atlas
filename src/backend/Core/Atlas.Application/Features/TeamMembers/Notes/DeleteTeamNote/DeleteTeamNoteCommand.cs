namespace Atlas.Application.Features.TeamMembers.Notes.DeleteTeamNote;

public sealed record DeleteTeamNoteCommand(Guid TeamMemberId, Guid NoteId) : IRequest<bool>;

