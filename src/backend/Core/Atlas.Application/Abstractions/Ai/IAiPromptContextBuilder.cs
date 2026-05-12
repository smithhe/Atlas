namespace Atlas.Application.Abstractions.Ai;

public interface IAiPromptContextBuilder
{
    AiViewScope Scope { get; }
    Task<string> BuildContextAsync(AiSessionStartRequest request, CancellationToken cancellationToken);
}

