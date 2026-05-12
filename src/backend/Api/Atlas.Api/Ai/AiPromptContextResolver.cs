using Atlas.Application.Abstractions.Ai;

namespace Atlas.Api.Ai;

public sealed class AiPromptContextResolver
{
    private readonly IReadOnlyDictionary<AiViewScope, IAiPromptContextBuilder> _builders;

    public AiPromptContextResolver(IEnumerable<IAiPromptContextBuilder> builders)
    {
        _builders = builders.ToDictionary(b => b.Scope, b => b);
    }

    public Task<string> BuildContextAsync(AiSessionStartRequest request, CancellationToken cancellationToken)
    {
        if (_builders.TryGetValue(request.View, out IAiPromptContextBuilder? builder))
        {
            return builder.BuildContextAsync(request, cancellationToken);
        }

        return Task.FromResult("No context builder available for this view.");
    }
}

