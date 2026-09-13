using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Atlas.Ui.Components.Ai;

public sealed class AiPanelInterop : IAsyncDisposable
{
    internal const string ModulePath = "./Components/Ai/AiPanel.razor.js";
    internal const string ScrollToBottomMethod = "scrollToBottom";
    internal const string CopyTextMethod = "copyText";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public AiPanelInterop(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask ScrollToBottomAsync(ElementReference scrollContainer)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync(ScrollToBottomMethod, scrollContainer);
    }

    public async ValueTask<bool> CopyTextAsync(string text)
    {
        IJSObjectReference module = await GetModuleAsync();
        return await module.InvokeAsync<bool>(CopyTextMethod, text);
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
