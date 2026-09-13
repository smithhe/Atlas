using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Atlas.Ui.Pages;

public sealed class SetupInterop : IAsyncDisposable
{
    internal const string ModulePath = "./Pages/Setup.razor.js";
    internal const string ScrollToCardMethod = "scrollToCard";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public SetupInterop(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask ScrollToCardAsync(ElementReference header, ElementReference target)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync(ScrollToCardMethod, header, target);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.DisposeAsync();
                _module = null;
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
