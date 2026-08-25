using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Shared;

public partial class AtlasModal
{
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }

    readonly string _titleId = $"modal-title-{Guid.NewGuid():N}";
    ElementReference _panel;
    bool _wasOpen;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            try { await _panel.FocusAsync(); } catch { /* ignore */ }
            return;
        }

        if (!IsOpen)
            _wasOpen = false;
    }

    async Task HandleClose() => await OnClose.InvokeAsync();

    async Task CloseFromOverlay(MouseEventArgs e)
    {
        // Overlay itself only — panel stops propagation.
        await OnClose.InvokeAsync();
    }

    async Task OnOverlayKey(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
            await OnClose.InvokeAsync();
    }
}
