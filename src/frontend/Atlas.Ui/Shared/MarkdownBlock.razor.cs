using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Atlas.Ui.Services;

namespace Atlas.Ui.Shared;

public partial class MarkdownBlock : IAsyncDisposable
{
    [Inject] private MarkdownRenderer Renderer { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    [Parameter] public string Text { get; set; } = "";

    private ElementReference _root;
    private string _html = "";
    private string? _lastText;
    private MarkdownBlockInterop? _markdownInterop;

    protected override void OnParametersSet()
    {
        if (Text == _lastText)
        {
            return;
        }

        _lastText = Text;
        _html = Renderer.ToSafeHtml(Text);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (string.IsNullOrEmpty(_html))
        {
            return;
        }

        if (firstRender)
        {
            _markdownInterop = new MarkdownBlockInterop(Js);
        }

        if (_markdownInterop is null)
        {
            return;
        }

        try
        {
            await _markdownInterop.HighlightAsync(_root);
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

    public async ValueTask DisposeAsync()
    {
        if (_markdownInterop is not null)
        {
            await _markdownInterop.DisposeAsync();
            _markdownInterop = null;
        }
    }
}
