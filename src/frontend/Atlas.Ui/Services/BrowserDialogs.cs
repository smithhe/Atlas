using Microsoft.JSInterop;

namespace Atlas.Ui.Services;

/// <summary>
/// Native <c>confirm</c>/<c>prompt</c>/<c>alert</c> via JS interop so Playwright <c>page.on('dialog')</c> works.
/// </summary>
public sealed class BrowserDialogs(IJSRuntime js)
{
    public ValueTask AlertAsync(string message) =>
        js.InvokeVoidAsync("alert", message);

    public ValueTask<bool> ConfirmAsync(string message) =>
        js.InvokeAsync<bool>("confirm", message);

    public ValueTask<string?> PromptAsync(string message, string? defaultValue = null) =>
        js.InvokeAsync<string?>("prompt", message, defaultValue);
}
