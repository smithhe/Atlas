using Microsoft.JSInterop;

namespace Atlas.Ui.Services;

/// <summary>Browser localStorage helpers. Only <c>atlas.defaultAiPanelOpen</c> is used (match React).</summary>
public sealed class LocalSettings : IAsyncDisposable
{
    internal const string ModulePath = "./js/modules/local-settings.js";
    internal const string GetItemMethod = "getItem";
    internal const string SetItemMethod = "setItem";

    private const string DefaultAiPanelOpenKey = "atlas.defaultAiPanelOpen";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public LocalSettings(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async Task<bool> LoadDefaultAiPanelOpenAsync()
    {
        try
        {
            IJSObjectReference module = await GetModuleAsync();
            string? raw = await module.InvokeAsync<string?>(GetItemMethod, DefaultAiPanelOpenKey);
            return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task SaveDefaultAiPanelOpenAsync(bool value)
    {
        try
        {
            IJSObjectReference module = await GetModuleAsync();
            await module.InvokeVoidAsync(SetItemMethod, DefaultAiPanelOpenKey, value ? "true" : "false");
        }
        catch
        {
            // Ignore storage failures in restricted contexts.
        }
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
