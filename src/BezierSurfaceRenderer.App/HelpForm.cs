using System.Reflection;

namespace BezierSurfaceRenderer.App;

/// <summary>
/// In-app documentation (spec §7), opened with F1 / Help menu: the embedded Help.md shown as plain text.
/// The same Help.md is the repository's user documentation, so there is one source of truth.
/// </summary>
public sealed class HelpForm : Form
{
    public HelpForm()
    {
        Text = "Dokumentacja — Bézier Surface Renderer";
        Size = new Size(900, 800);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        KeyPreview = true;

        var text = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            Font = new Font("Consolas", 10f),
            Text = LoadMarkdown().Replace("\r\n", "\n").Replace("\n", "\r\n"),
        };
        Controls.Add(text);
        Shown += (_, _) => text.Select(0, 0);
        KeyDown += (_, e) => { if (e.KeyCode is Keys.Escape or Keys.F1) Close(); };
    }

    private static string LoadMarkdown()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Help.md");
        if (stream == null) return "Nie znaleziono zasobu dokumentacji.";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
