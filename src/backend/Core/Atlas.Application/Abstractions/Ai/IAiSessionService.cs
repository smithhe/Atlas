namespace Atlas.Application.Abstractions.Ai;

public interface IAiSessionService
{
    Task<Guid> StartSessionAsync(AiSessionStartRequest request, CancellationToken cancellationToken);
}

