using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Atlas.Ui.Services;
using Atlas.Ui.Contracts;

namespace Atlas.Ui.Shared
{
    public partial class MarkdownBlock : IAsyncDisposable
    {
        [Inject] private MarkdownRenderer _renderer { get; set; } = null!;
        [Inject] private IJSRuntime _js { get; set; } = null!;

        [Parameter] public string Text { get; set; } = "";

        private ElementReference Root { get; set; }
        private string Html { get; set; } = "";
        private string? LastText { get; set; }
        private MarkdownBlockInterop? MarkdownInterop { get; set; }

        protected override void OnParametersSet()
        {
            if (Text == this.LastText)
            {
                return;
            }

            this.LastText = Text;
            this.Html = this._renderer.ToSafeHtml(Text);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (string.IsNullOrEmpty(this.Html))
            {
                return;
            }

            if (firstRender)
            {
                this.MarkdownInterop = new MarkdownBlockInterop(this._js);
            }

            if (this.MarkdownInterop is null)
            {
                return;
            }

            try
            {
                await this.MarkdownInterop.HighlightAsync(this.Root);
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
            if (this.MarkdownInterop is not null)
            {
                await this.MarkdownInterop.DisposeAsync();
                this.MarkdownInterop = null;
            }
        }
    }
}
