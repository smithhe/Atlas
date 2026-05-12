namespace Atlas.Application.Abstractions.Ai;

public sealed record AiSessionEvent(
    Guid EventId,
    Guid SessionId,
    int Sequence,
    string Type,
    DateTimeOffset OccurredAtUtc,
    string? Status = null,
    string? Message = null,
    string? Delta = null,
    bool IsTerminal = false);

