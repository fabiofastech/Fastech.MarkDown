using Markdig;

namespace Fastech.MarkDown.App.Services;

public class MarkdownRenderService
{
    private readonly MarkdownPipeline _pipeline;

    private const string HtmlTemplate = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8"/>
            {{BASE}}
            <link rel="stylesheet" href="https://assets.local/highlight-github.min.css"/>
            <script src="https://assets.local/highlight.min.js"></script>
            <script src="https://assets.local/mermaid.min.js"></script>
            <style>
                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;
                    font-size: 16px;
                    line-height: 1.6;
                    color: #24292e;
                    background: #ffffff;
                    padding: 24px 48px;
                    max-width: 960px;
                }
                h1, h2, h3, h4, h5, h6 {
                    border-bottom: 1px solid #eaecef;
                    padding-bottom: .3em;
                    margin-top: 24px;
                    font-weight: 600;
                }
                h1 { font-size: 2em; }
                h2 { font-size: 1.5em; }
                h3 { font-size: 1.25em; }
                pre {
                    background: #f6f8fa;
                    border-radius: 6px;
                    padding: 16px;
                    overflow: auto;
                    font-size: 14px;
                }
                code {
                    background: #f0f0f0;
                    padding: 2px 5px;
                    border-radius: 3px;
                    font-family: 'Consolas', 'Courier New', monospace;
                    font-size: .9em;
                }
                pre code {
                    background: none;
                    padding: 0;
                    font-size: inherit;
                }
                table {
                    border-collapse: collapse;
                    width: 100%;
                    margin: 16px 0;
                }
                th, td {
                    border: 1px solid #dfe2e5;
                    padding: 8px 13px;
                }
                th { background: #f6f8fa; font-weight: 600; }
                tr:nth-child(even) { background: #f6f8fa; }
                blockquote {
                    border-left: 4px solid #dfe2e5;
                    margin: 0 0 16px 0;
                    padding: 0 16px;
                    color: #6a737d;
                }
                img { max-width: 100%; height: auto; }
                a { color: #0366d6; text-decoration: none; }
                a:hover { text-decoration: underline; }
                .mermaid { text-align: center; margin: 24px 0; }
                hr { border: none; border-top: 1px solid #eaecef; margin: 24px 0; }
                ul, ol { padding-left: 2em; }
                li { margin: 4px 0; }
                .task-list-item { list-style-type: none; margin-left: -1.6em; }
                .task-list-item input { margin-right: .5em; }
            </style>
        </head>
        <body>
            {{CONTENT}}
            <script>
                // Converte i blocchi mermaid da code block a div mermaid
                document.querySelectorAll('code.language-mermaid').forEach(function(el) {
                    var div = document.createElement('div');
                    div.className = 'mermaid';
                    div.textContent = el.textContent;
                    el.closest('pre').replaceWith(div);
                });
                mermaid.initialize({ startOnLoad: true, theme: 'default', securityLevel: 'loose' });
                // Syntax highlighting per tutti i blocchi non-mermaid
                document.querySelectorAll('pre code').forEach(function(el) {
                    if (!el.classList.contains('language-mermaid')) {
                        hljs.highlightElement(el);
                    }
                });
            </script>
        </body>
        </html>
        """;

    public MarkdownRenderService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public string RenderToHtml(string markdownContent, string? baseHref = null)
    {
        var html = Markdown.ToHtml(markdownContent, _pipeline);
        var baseTag = baseHref is null ? "" : $"""<base href="{baseHref}"/>""";
        return HtmlTemplate
            .Replace("{{BASE}}", baseTag)
            .Replace("{{CONTENT}}", html);
    }
}
