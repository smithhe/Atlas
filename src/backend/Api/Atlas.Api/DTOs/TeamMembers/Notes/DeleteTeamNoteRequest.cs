namespace Atlas.Api.DTOs.TeamMembers.Notes;

public sealed record DeleteTeamNoteRequest(Guid TeamMemberId, Guid NoteId);

