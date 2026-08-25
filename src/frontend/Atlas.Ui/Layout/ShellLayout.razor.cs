using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Layout;

public partial class ShellLayout : IAsyncDisposable
{
    [Inject] AppCacheService Cache { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;
    [Inject] AiStateService Ai { get; set; } = default!;
    [Inject] IJSRuntime Js { get; set; } = default!;

    const int MinAiWidth = 320;
    const string DefaultAiWidthCss = "clamp(320px, 26vw, 560px)";

    static readonly NavItem[] NavItems =
    [
        new("/dashboard", "Dashboard"),
        new("/tasks", "Tasks"),
        new("/risks", "Risks & Mitigation"),
        new("/team", "Team"),
        new("/projects", "Projects"),
        new("/settings", "Settings"),
    ];

    bool _quickAddOpen;
    bool _resizing;
    double _resizeStartX;
    int _resizeStartWidth;
    ElementReference _resizerRef;
    DotNetObjectReference<ShellLayout>? _selfRef;

    string BodyGridClass => Ai.IsOpen ? "bodyGrid bodyGridAiOpen" : "bodyGrid bodyGridNoAi";

    string AiWidthStyle
    {
        get
        {
            string width = Ai.PanelWidthPx is { } px
                ? $"{Math.Max(MinAiWidth, px)}px"
                : DefaultAiWidthCss;
            return $"--aiWidth: {width}";
        }
    }

    string ContextTitle
    {
        get
        {
            string path = new Uri(Nav.Uri).AbsolutePath;
            if (path.StartsWith("/dashboard", StringComparison.OrdinalIgnoreCase)) return "Context: Dashboard";
            if (path.StartsWith("/tasks", StringComparison.OrdinalIgnoreCase)) return "Context: Tasks";
            if (path.StartsWith("/team", StringComparison.OrdinalIgnoreCase)) return "Context: Team";
            if (path.StartsWith("/risks", StringComparison.OrdinalIgnoreCase)) return "Context: Risks";
            if (path.StartsWith("/projects", StringComparison.OrdinalIgnoreCase)) return "Context: Projects";
            if (path.StartsWith("/settings", StringComparison.OrdinalIgnoreCase)) return "Context: Settings";
            return "Context: Dashboard";
        }
    }

    protected override void OnInitialized()
    {
        Cache.Changed += OnCacheChanged;
        Ai.Changed += OnAiChanged;
        Ai.EnsureStartupPreference();
        _ = Cache.EnsureHydratedAsync();
        _ = EnsureResizeListenersAsync();
    }

    async Task EnsureResizeListenersAsync()
    {
        try
        {
            _selfRef ??= DotNetObjectReference.Create(this);
            await Js.InvokeVoidAsync("atlasAiEvents.ensureResizeListeners", _selfRef);
        }
        catch
        {
            // Module may not be ready on first frame.
        }
    }

    void OnCacheChanged() => InvokeAsync(StateHasChanged);
    void OnAiChanged() => InvokeAsync(StateHasChanged);

    void OpenQuickAdd() => _quickAddOpen = true;
    void CloseQuickAdd() => _quickAddOpen = false;
    void ToggleAi() => Ai.SetIsOpen(!Ai.IsOpen);

    void ResetAiWidth() => Ai.SetPanelWidthPx(null);

    void OnResizePointerDown(PointerEventArgs e)
    {
        _resizing = true;
        _resizeStartX = e.ClientX;
        _resizeStartWidth = Ai.PanelWidthPx ?? MinAiWidth;
        _ = Js.InvokeVoidAsync("atlasAiEvents.beginResizeCapture", _resizerRef, e.PointerId);
    }

    void OnResizeKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Home")
        {
            Ai.SetPanelWidthPx(null);
            return;
        }

        int current = Ai.PanelWidthPx ?? MinAiWidth;
        if (e.Key == "ArrowLeft") Ai.SetPanelWidthPx(current + 20);
        if (e.Key == "ArrowRight") Ai.SetPanelWidthPx(Math.Max(MinAiWidth, current - 20));
    }

    [JSInvokable]
    public void OnAiResizeMove(double clientX, double innerWidth)
    {
        if (!_resizing) return;
        double dx = _resizeStartX - clientX;
        double next = _resizeStartWidth + dx;
        int max = Math.Max(MinAiWidth, (int)Math.Floor(innerWidth * 0.6));
        Ai.SetPanelWidthPx(Math.Max(MinAiWidth, Math.Min(max, (int)Math.Floor(next))));
    }

    [JSInvokable]
    public void OnAiResizeUp() => _resizing = false;

    public async ValueTask DisposeAsync()
    {
        Cache.Changed -= OnCacheChanged;
        Ai.Changed -= OnAiChanged;
        // Clear JS listeners before disposing DotNetObjectReference so in-flight
        // pointer callbacks cannot invoke a disposed ref.
        try
        {
            await Js.InvokeVoidAsync("atlasAiEvents.clearResizeListeners");
        }
        catch
        {
            // Ignore dispose races during navigation / host teardown.
        }

        _selfRef?.Dispose();
        _selfRef = null;
    }

    sealed record NavItem(string Href, string Label);
}
