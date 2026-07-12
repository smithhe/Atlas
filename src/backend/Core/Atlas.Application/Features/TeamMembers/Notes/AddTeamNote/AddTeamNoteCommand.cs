using Atlas.Domain.Enums;

namespace Atlas.Application.Features.TeamMembers.Notes.AddTeamNote;

public sealed record AddTeamNoteCommand(
    Guid TeamMemberId,
    NoteType Type,
    string? Title,
    string Text,
    string? AdoWorkItemId = null,
    string? PrUrl = null) : IRequest<Guid>;

