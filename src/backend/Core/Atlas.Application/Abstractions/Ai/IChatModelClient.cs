namespace Atlas.Application.Abstractions.Ai;

public interface IChatModelClient
{
    IAsyncEnumerable<string> GenerateStreamingAsync(AiModelRequest request, CancellationToken cancellationToken);
}

