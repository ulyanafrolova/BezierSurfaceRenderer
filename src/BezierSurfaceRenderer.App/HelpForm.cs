using System.Reflection;
using Markdig;

namespace BezierSurfaceRenderer.App;

/// <summary>
/// In-app documentation (spec §7), opened with F1 / Help menu: the embedded Help.md rendered to HTML.
/// The same Help.md is the repository's user documentation, so there is one source of truth.
/// </summary>
public sealed class HelpForm : Form
{
    private const string Style = """
        body { font-family: 'Segoe UI', sans-serif; font-size: 10.5pt; line-height: 1.5;
               color: #222; margin: 16px 28px; }
        h1 { font-size: 20pt; border-bottom: 1px solid #ddd; padding-bottom: 6px; }
        h2 { font-size: 14pt; margin-top: 28px; border-bottom: 1px solid #eee; padding-bottom: 4px; }
        h3 { font-size: 11.5pt; margin-top: 18px; }
        code { font-family: Consolas, monospace; background: #f3f3f3; padding: 1px 4px; }
        pre { background: #f6f6f6; border: 1px solid #e3e3e3; padding: 10px 12px; }
        pre code { background: none; padding: 0; }
        table { border-collapse: collapse; margin: 8px 0; }
        th, td { border: 1px solid #ccc; padding: 5px 10px; text-align: left; vertical-align: top; }
        th { background: #f0f0f0; }
        """;

    public HelpForm()
    {
        Text = "Dokumentacja — Bézier Surface Renderer";
        Size = new Size(900, 800);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        var pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();
        var browser = new WebBrowser
        {
            Dock = DockStyle.Fill,
            AllowWebBrowserDrop = false,
            IsWebBrowserContextMenuEnabled = false,
            // X-UA-Compatible: the WinForms WebBrowser otherwise renders in IE7 mode.
            DocumentText = "<!DOCTYPE html><html><head><meta charset=\"utf-8\">"
                + "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">"
                + $"<style>{Style}</style></head><body>"
                + Markdown.ToHtml(LoadMarkdown(), pipeline)
                + "</body></html>",
        };
        // The browser has keyboard focus, so close on its key events rather than the form's.
        browser.PreviewKeyDown += (_, e) => { if (e.KeyCode is Keys.Escape or Keys.F1) Close(); };
        Controls.Add(browser);
    }

    private static string LoadMarkdown()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Help.md");
        if (stream == null) return "Nie znaleziono zasobu dokumentacji.";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
