namespace Atlas.Application.Features.TeamMembers.Notes.SetPinnedNotes;

/// <summary>
/// Sets pinned notes order for a member. Note ids provided are pinned in the given order; all other notes become unpinned.
/// </summary>
public sealed record SetPinnedNotesCommand(Guid TeamMemberId, IReadOnlyList<Guid> NoteIdsInOrder) : IRequest<bool>;

