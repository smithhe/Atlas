using Ganss.Xss;
using Markdig;

namespace Atlas.Ui.Services;

/// <summary>
/// Shared Markdig → HTML → HtmlSanitizer pipeline for <c>MarkdownBlock</c>
/// (AI transcript + Team notes). Secure by default: no raw HTML in, sanitize after render.
/// </summary>
public sealed class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml() // Match react-markdown: raw HTML in source is not rendered
        .Build();

    private readonly HtmlSanitizer _sanitizer;

    public MarkdownRenderer()
    {
        _sanitizer = new HtmlSanitizer();
        // Disallow inline styles (XSS / layout injection vector).
        _sanitizer.AllowedAttributes.Remove("style");
        // Allow highlight.js class tokens on code/pre (language-* and hljs*).
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("rel");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedTags.Add("pre");
        _sanitizer.AllowedTags.Add("code");
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("h4");
        _sanitizer.AllowedTags.Add("h5");
        _sanitizer.AllowedTags.Add("h6");
        _sanitizer.AllowedTags.Add("table");
        _sanitizer.AllowedTags.Add("thead");
        _sanitizer.AllowedTags.Add("tbody");
        _sanitizer.AllowedTags.Add("tr");
        _sanitizer.AllowedTags.Add("th");
        _sanitizer.AllowedTags.Add("td");
        _sanitizer.AllowedTags.Add("hr");
        _sanitizer.AllowedTags.Add("blockquote");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("a");
        _sanitizer.AllowedTags.Add("img");
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("alt");
        _sanitizer.AllowedAttributes.Add("title");
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
    }

    public string ToSafeHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return "";
        }

        var raw = Markdown.ToHtml(markdown, Pipeline);
        var sanitized = _sanitizer.Sanitize(raw);

        // Match React: open links in a new tab.
        sanitized = sanitized.Replace("<a href=", "<a target=\"_blank\" rel=\"noreferrer\" href=", StringComparison.Ordinal);
        return sanitized;
    }
}
