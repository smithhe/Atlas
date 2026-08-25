using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Atlas.Ui.Api.Generated;
using Atlas.Ui.Mapping;
using Atlas.Ui.Models;
using Atlas.Ui.Services;

namespace Atlas.Ui.Shared;

public partial class MarkdownBlock : IAsyncDisposable
{
    [Inject] MarkdownRenderer Renderer { get; set; } = default!;
    [Inject] IJSRuntime Js { get; set; } = default!;

    [Parameter] public string Text { get; set; } = "";

    ElementReference _root;
    string _html = "";
    string? _lastText;

    protected override void OnParametersSet()
    {
        if (Text == _lastText) return;
        _lastText = Text;
        _html = Renderer.ToSafeHtml(Text);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (string.IsNullOrEmpty(_html)) return;
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
