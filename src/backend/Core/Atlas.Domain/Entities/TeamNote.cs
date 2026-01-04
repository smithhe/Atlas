using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;

namespace Atlas.Domain.Entities;

public sealed class TeamNote : Entity
{
    public Guid TeamMemberId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }

    public NoteType Type { get; set; }
    public string? Title { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional pinned ordering (lower comes first). When null, the note is not pinned.
    /// </summary>
    public int? PinnedOrder { get; set; }
}

