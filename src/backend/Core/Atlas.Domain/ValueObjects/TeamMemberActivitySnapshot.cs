namespace Atlas.Domain.ValueObjects;

public sealed record TeamMemberActivitySnapshot
{
    public List<string> Bullets { get; init; } = [];
    public DateTimeOffset? LastUpdatedAt { get; init; }
    public List<string>? QuickTags { get; init; }
}

