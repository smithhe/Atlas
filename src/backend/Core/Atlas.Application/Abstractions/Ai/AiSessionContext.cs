namespace Atlas.Application.Abstractions.Ai;

public sealed record AiSessionContext(
    Guid SessionId,
    AiSessionStartRequest Request);

