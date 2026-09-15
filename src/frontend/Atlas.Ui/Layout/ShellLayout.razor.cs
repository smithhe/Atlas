using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Layout
{
public partial class ShellLayout
{
    [Inject] private IAppCacheService _cache { get; set; } = null!;
    [Inject] private NavigationManager _nav { get; set; } = null!;
    [Inject] private IAiStateService _ai { get; set; } = null!;
    [Inject] private IJSRuntime _js { get; set; } = null!;
    [Inject] private IGrowthService _growthService { get; set; } = null!;
    [Inject] private BrowserDialogs _dialogs { get; set; } = null!;

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

    private bool QuickAddOpen { get; set; }
    private bool Resizing { get; set; }
    private double ResizeStartX { get; set; }
    private int ResizeStartWidth { get; set; }
    private ElementReference ResizerRef { get; set; }
    private DotNetObjectReference<ShellLayout>? SelfRef { get; set; }
    private ShellLayoutInterop? ResizeInterop { get; set; }
    private ErrorBoundary? ErrorBoundaryRef { get; set; }

    private string BodyGridClass => this._ai.IsOpen ? "bodyGrid bodyGridAiOpen" : "bodyGrid bodyGridNoAi";

    private string AiWidthStyle
    {
        get
        {
            string width = this._ai.PanelWidthPx is { } px
                ? $"{Math.Max(MinAiWidth, px)}px"
                : DefaultAiWidthCss;
            return $"--aiWidth: {width}";
        }
    }

    private string ContextTitle
    {
        get
        {
            string path = new Uri(this._nav.Uri).AbsolutePath;
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
        this._cache.Changed += OnCacheChangedAsync;
        this._ai.Changed += OnAiChangedAsync;
        this._nav.LocationChanged += OnLocationChanged;
        this._ai.EnsureStartupPreference();
        this._growthService.PersistFailed += OnPersistFailed;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            this.ResizeInterop ??= new ShellLayoutInterop(this._js);
            this.SelfRef ??= DotNetObjectReference.Create(this);
            await this.ResizeInterop.EnsureResizeListenersAsync(this.SelfRef);
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
        this.ErrorBoundaryRef?.Recover();

    private void RecoverFromError() => this.ErrorBoundaryRef?.Recover();

    private void OpenQuickAdd() => this.QuickAddOpen = true;
    private void CloseQuickAdd() => this.QuickAddOpen = false;
    private void ToggleAi() => this._ai.SetIsOpen(!this._ai.IsOpen);

    private void ResetAiWidth() => this._ai.SetPanelWidthPx(null);

    private async Task OnResizePointerDown(PointerEventArgs e)
    {
        this.Resizing = true;
        this.ResizeStartX = e.ClientX;
        this.ResizeStartWidth = this._ai.PanelWidthPx ?? MinAiWidth;

        try
        {
            this.ResizeInterop ??= new ShellLayoutInterop(this._js);
            await this.ResizeInterop.BeginResizeCaptureAsync(this.ResizerRef, e.PointerId);
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
            this._ai.SetPanelWidthPx(null);
            return;
        }

        int current = this._ai.PanelWidthPx ?? MinAiWidth;
        if (e.Key == "ArrowLeft")
        {
            this._ai.SetPanelWidthPx(current + 20);
        }

        if (e.Key == "ArrowRight")
        {
            this._ai.SetPanelWidthPx(Math.Max(MinAiWidth, current - 20));
        }
    }

    [JSInvokable]
    public void OnAiResizeMove(double clientX, double innerWidth)
    {
        if (!this.Resizing)
        {
            return;
        }

        double dx = this.ResizeStartX - clientX;
        double next = this.ResizeStartWidth + dx;
        int max = Math.Max(MinAiWidth, (int)Math.Floor(innerWidth * 0.6));
        this._ai.SetPanelWidthPx(Math.Max(MinAiWidth, Math.Min(max, (int)Math.Floor(next))));
    }

    [JSInvokable]
    public void OnAiResizeUp() => this.Resizing = false;

    private async void OnPersistFailed(string message)
    {
        try
        {
            await InvokeAsync(async () => await this._dialogs.AlertAsync(message));
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        this._cache.Changed -= OnCacheChangedAsync;
        this._ai.Changed -= OnAiChangedAsync;
        this._nav.LocationChanged -= OnLocationChanged;
        this._growthService.PersistFailed -= OnPersistFailed;

        if (this.ResizeInterop is not null)
        {
            await this.ResizeInterop.DisposeAsync();
            this.ResizeInterop = null;
        }

        this.SelfRef?.Dispose();
        this.SelfRef = null;
    }

    private sealed record NavItem(string Href, string Label);
}
}
