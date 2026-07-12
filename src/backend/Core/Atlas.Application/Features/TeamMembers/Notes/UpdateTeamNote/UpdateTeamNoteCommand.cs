using Atlas.Domain.Enums;

namespace Atlas.Application.Features.TeamMembers.Notes.UpdateTeamNote;

public sealed record UpdateTeamNoteCommand(
    Guid TeamMemberId,
    Guid NoteId,
    NoteType Type,
    string? Title,
    string Text,
    int? PinnedOrder,
    string? AdoWorkItemId = null,
    string? PrUrl = null) : IRequest<bool>;

