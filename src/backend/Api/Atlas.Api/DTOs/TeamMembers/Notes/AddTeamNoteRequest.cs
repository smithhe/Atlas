using Atlas.Domain.Enums;

namespace Atlas.Api.DTOs.TeamMembers.Notes;

public sealed record AddTeamNoteRequest(
    Guid TeamMemberId,
    NoteType Type,
    string? Title,
    string Text);

