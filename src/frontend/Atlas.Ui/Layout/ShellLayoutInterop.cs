using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Atlas.Ui.Layout;

public sealed class ShellLayoutInterop : IAsyncDisposable
{
    internal const string ModulePath = "./Layout/ShellLayout.razor.js";
    internal const string EnsureResizeListenersMethod = "ensureResizeListeners";
    internal const string ClearResizeListenersMethod = "clearResizeListeners";
    internal const string BeginResizeCaptureMethod = "beginResizeCapture";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public ShellLayoutInterop(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask EnsureResizeListenersAsync(DotNetObjectReference<ShellLayout> dotNetRef)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync(EnsureResizeListenersMethod, dotNetRef);
    }

    public async ValueTask ClearResizeListenersAsync()
    {
        if (_module is null)
        {
            return;
        }

        await _module.InvokeVoidAsync(ClearResizeListenersMethod);
    }

    public async ValueTask BeginResizeCaptureAsync(ElementReference element, double pointerId)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync(BeginResizeCaptureMethod, element, pointerId);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await ClearResizeListenersAsync();
                await _module.DisposeAsync();
                _module = null;
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
