using Microsoft.JSInterop;

namespace Atlas.Ui.Services;

/// <summary>Browser localStorage helpers. Only <c>atlas.defaultAiPanelOpen</c> is used (match React).</summary>
public sealed class LocalSettings(IJSRuntime js)
{
    private const string DefaultAiPanelOpenKey = "atlas.defaultAiPanelOpen";

    public async Task<bool> LoadDefaultAiPanelOpenAsync()
    {
        try
        {
            var raw = await js.InvokeAsync<string?>("localStorage.getItem", DefaultAiPanelOpenKey);
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
            await js.InvokeVoidAsync("localStorage.setItem", DefaultAiPanelOpenKey, value ? "true" : "false");
        }
        catch
        {
            // Ignore storage failures in restricted contexts.
        }
    }
}
