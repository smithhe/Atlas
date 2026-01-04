namespace Atlas.Domain.ValueObjects;

public sealed record TeamMemberProfile
{
    public string? TimeZone { get; init; }
    public string? TypicalHours { get; init; }
}

