using Atlas.Domain.Abstractions;
using Atlas.Domain.Enums;
using Atlas.Domain.ValueObjects;

namespace Atlas.Domain.Entities;

public sealed class TeamMember : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }

    public StatusDot StatusDot { get; set; }
    public string CurrentFocus { get; set; } = string.Empty;

    public TeamMemberProfile Profile { get; set; } = new();
    public TeamMemberSignals Signals { get; set; } = new();

    public List<Guid> PinnedNoteIds { get; set; } = [];
    public TeamMemberActivitySnapshot ActivitySnapshot { get; set; } = new();

    public List<TeamNote> Notes { get; set; } = [];

    // Azure DevOps/work item shapes intentionally deferred to a later layer.
}

