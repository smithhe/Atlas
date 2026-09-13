using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Atlas.Ui.Shared;

public sealed class MarkdownBlockInterop : IAsyncDisposable
{
    internal const string ModulePath = "./Shared/MarkdownBlock.razor.js";
    internal const string HighlightMethod = "highlight";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public MarkdownBlockInterop(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask HighlightAsync(ElementReference root)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync(HighlightMethod, root);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
            _module = null;
        }
    }
}
