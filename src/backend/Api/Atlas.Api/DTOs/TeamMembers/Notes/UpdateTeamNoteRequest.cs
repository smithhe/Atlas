using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.TeamMembers.Notes;

public sealed record UpdateTeamNoteRequest(
    Guid TeamMemberId,
    Guid NoteId,
    NoteType Type,
    string? Title,
    string Text,
    int? PinnedOrder);

