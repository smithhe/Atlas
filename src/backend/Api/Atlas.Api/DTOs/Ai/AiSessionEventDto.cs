namespace Atlas.Api.DTOs.Ai;

public sealed record AiSessionEventDto(
    Guid EventId,
    Guid SessionId,
    int Sequence,
    string Type,
    string? Status,
    string? Message,
    string? Delta,
    DateTimeOffset OccurredAtUtc,
    bool IsTerminal);

