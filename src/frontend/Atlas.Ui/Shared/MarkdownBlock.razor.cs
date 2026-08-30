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

        try
        {
            await Js.InvokeVoidAsync("atlasMarkdown.highlight", _root);
        }
        catch
        {
            // highlight.js may not be ready during first paint.
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
