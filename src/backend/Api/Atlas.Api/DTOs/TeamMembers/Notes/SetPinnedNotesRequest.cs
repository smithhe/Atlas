namespace Atlas.Api.DTOs.TeamMembers.Notes;

public sealed record SetPinnedNotesRequest(Guid TeamMemberId, IReadOnlyList<Guid> NoteIdsInOrder);

