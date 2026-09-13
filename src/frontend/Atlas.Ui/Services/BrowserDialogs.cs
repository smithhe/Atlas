using Microsoft.JSInterop;

namespace Atlas.Ui.Services;

/// <summary>
/// Native <c>confirm</c>/<c>prompt</c>/<c>alert</c> via JS interop so Playwright <c>page.on('dialog')</c> works.
/// </summary>
public sealed class BrowserDialogs : IAsyncDisposable
{
    internal const string ModulePath = "./js/modules/dialogs.js";
    internal const string AlertMethod = "alert";
    internal const string ConfirmMethod = "confirm";
    internal const string PromptMethod = "prompt";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public BrowserDialogs(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask AlertAsync(string message)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync(AlertMethod, message);
    }

    public async ValueTask<bool> ConfirmAsync(string message)
    {
        IJSObjectReference module = await GetModuleAsync();
        return await module.InvokeAsync<bool>(ConfirmMethod, message);
    }

    public async ValueTask<string?> PromptAsync(string message, string? defaultValue = null)
    {
        IJSObjectReference module = await GetModuleAsync();
        return await module.InvokeAsync<string?>(PromptMethod, message, defaultValue);
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
