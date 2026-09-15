using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Shared
{
public partial class AtlasModal
{
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }

    private readonly string _titleId = $"modal-title-{Guid.NewGuid():N}";
    private ElementReference Panel { get; set; }
    private bool WasOpen { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen && !this.WasOpen)
        {
            this.WasOpen = true;
            try { await this.Panel.FocusAsync(); } catch { /* ignore */ }
            return;
        }

        if (!IsOpen)
        {
            this.WasOpen = false;
        }
    }

    private async Task HandleClose() => await OnClose.InvokeAsync();

    private async Task CloseFromOverlay(MouseEventArgs e)
    {
        // Overlay itself only — panel stops propagation.
        await OnClose.InvokeAsync();
    }

    private async Task OnOverlayKey(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            await OnClose.InvokeAsync();
        }
    }
}
}
