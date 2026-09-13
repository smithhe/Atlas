using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Services;

namespace Atlas.Ui.Layout;

public partial class ShellLayout
{
    [Inject] private AppCacheService Cache { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private AiStateService Ai { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private const int MinAiWidth = 320;
    private const string DefaultAiWidthCss = "clamp(320px, 26vw, 560px)";

    private static readonly NavItem[] NavItems =
    [
        new("/dashboard", "Dashboard"),
        new("/tasks", "Tasks"),
        new("/risks", "Risks & Mitigation"),
        new("/team", "Team"),
        new("/projects", "Projects"),
        new("/settings", "Settings"),
    ];

    private bool _quickAddOpen;
    private bool _resizing;
    private double _resizeStartX;
    private int _resizeStartWidth;
    private ElementReference _resizerRef;
    private DotNetObjectReference<ShellLayout>? _selfRef;
    private ShellLayoutInterop? _resizeInterop;
    private ErrorBoundary? _errorBoundary;

    private string BodyGridClass => Ai.IsOpen ? "bodyGrid bodyGridAiOpen" : "bodyGrid bodyGridNoAi";

    private string AiWidthStyle
    {
        get
        {
            string width = Ai.PanelWidthPx is { } px
                ? $"{Math.Max(MinAiWidth, px)}px"
                : DefaultAiWidthCss;
            return $"--aiWidth: {width}";
        }
    }

    private string ContextTitle
    {
        get
        {
            string path = new Uri(Nav.Uri).AbsolutePath;
            if (path.StartsWith("/dashboard", StringComparison.OrdinalIgnoreCase))
            {
                return "Context: Dashboard";
            }

            if (path.StartsWith("/tasks", StringComparison.OrdinalIgnoreCase))
            {
                return "Context: Tasks";
            }

            if (path.StartsWith("/team", StringComparison.OrdinalIgnoreCase))
            {
                return "Context: Team";
            }

            if (path.StartsWith("/risks", StringComparison.OrdinalIgnoreCase))
            {
                return "Context: Risks";
            }

            if (path.StartsWith("/projects", StringComparison.OrdinalIgnoreCase))
            {
                return "Context: Projects";
            }

            if (path.StartsWith("/settings", StringComparison.OrdinalIgnoreCase))
            {
                return "Context: Settings";
            }

            return "Context: Dashboard";
        }
    }

    protected override void OnInitialized()
    {
        Cache.Changed += OnCacheChangedAsync;
        Ai.Changed += OnAiChangedAsync;
        Nav.LocationChanged += OnLocationChanged;
        Ai.EnsureStartupPreference();
        HydrateInBackgroundFireAndForget();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _resizeInterop ??= new ShellLayoutInterop(Js);
            _selfRef ??= DotNetObjectReference.Create(this);
            await _resizeInterop.EnsureResizeListenersAsync(_selfRef);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private async void HydrateInBackgroundFireAndForget()
    {
        try
        {
            await Cache.EnsureHydratedAsync();
        }
        catch (Exception ex)
        {
            Cache.RecordHydrationFailure(ex);
        }
    }

    private async void OnCacheChangedAsync()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private async void OnAiChangedAsync()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        _errorBoundary?.Recover();

    private void RecoverFromError() => _errorBoundary?.Recover();

    private void OpenQuickAdd() => _quickAddOpen = true;
    private void CloseQuickAdd() => _quickAddOpen = false;
    private void ToggleAi() => Ai.SetIsOpen(!Ai.IsOpen);

    private void ResetAiWidth() => Ai.SetPanelWidthPx(null);

    private async Task OnResizePointerDown(PointerEventArgs e)
    {
        _resizing = true;
        _resizeStartX = e.ClientX;
        _resizeStartWidth = Ai.PanelWidthPx ?? MinAiWidth;

        try
        {
            _resizeInterop ??= new ShellLayoutInterop(Js);
            await _resizeInterop.BeginResizeCaptureAsync(_resizerRef, e.PointerId);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void OnResizeKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Home")
        {
            Ai.SetPanelWidthPx(null);
            return;
        }

        int current = Ai.PanelWidthPx ?? MinAiWidth;
        if (e.Key == "ArrowLeft")
        {
            Ai.SetPanelWidthPx(current + 20);
        }

        if (e.Key == "ArrowRight")
        {
            Ai.SetPanelWidthPx(Math.Max(MinAiWidth, current - 20));
        }
    }

    [JSInvokable]
    public void OnAiResizeMove(double clientX, double innerWidth)
    {
        if (!_resizing)
        {
            return;
        }

        double dx = _resizeStartX - clientX;
        double next = _resizeStartWidth + dx;
        int max = Math.Max(MinAiWidth, (int)Math.Floor(innerWidth * 0.6));
        Ai.SetPanelWidthPx(Math.Max(MinAiWidth, Math.Min(max, (int)Math.Floor(next))));
    }

    [JSInvokable]
    public void OnAiResizeUp() => _resizing = false;

    public async ValueTask DisposeAsync()
    {
        Cache.Changed -= OnCacheChangedAsync;
        Ai.Changed -= OnAiChangedAsync;
        Nav.LocationChanged -= OnLocationChanged;

        if (_resizeInterop is not null)
        {
            await _resizeInterop.DisposeAsync();
            _resizeInterop = null;
        }

        _selfRef?.Dispose();
        _selfRef = null;
    }

    private sealed record NavItem(string Href, string Label);
}
