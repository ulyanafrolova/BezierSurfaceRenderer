using System.Reflection;
using T = BezierSurfaceRenderer.App.Theme.Theme;

namespace BezierSurfaceRenderer.App;

/// <summary>
/// In-app documentation (spec §7), opened with F1 / Help menu. Renders the embedded Help.md
/// (headings, paragraphs, bullet lists, tables, code/formula blocks, **bold** and `code` spans)
/// into a read-only RichTextBox. The same Help.md is the repository's user documentation,
/// so there is one source of truth.
/// </summary>
public sealed class HelpForm : Form
{
    private static readonly Font H1 = new("Segoe UI Semibold", 17f);
    private static readonly Font H2 = new("Segoe UI Semibold", 12.5f);
    private static readonly Font H3 = new("Segoe UI Semibold", 10.5f);
    private static readonly Font Body = new("Segoe UI", 10f);
    private static readonly Font Bold = new("Segoe UI Semibold", 10f);

    private readonly RichTextBox _box;

    public HelpForm()
    {
        Text = "Documentation — Bézier Surface Renderer";
        Size = new Size(900, 820);
        MinimumSize = new Size(560, 400);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        KeyPreview = true;
        T.Apply(this);

        _box = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = T.Background,
            ForeColor = T.Text,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Cursor = Cursors.Default,
        };
        var frame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 20, 12, 12), BackColor = T.Background };
        frame.Controls.Add(_box);
        Controls.Add(frame);

        Render(LoadMarkdown());
        _box.SelectionStart = 0;
        KeyDown += (_, e) => { if (e.KeyCode is Keys.Escape or Keys.F1) Close(); };
    }

    private static string LoadMarkdown()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Help.md");
        if (stream == null) return "# Help\n\nDocumentation resource not found.";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ---------------------------------------------------------------- tiny Markdown subset renderer

    private void Render(string markdown)
    {
        bool inCode = false;
        var table = new List<string[]>();
        foreach (string rawLine in markdown.Replace("\r", "").Split('\n'))
        {
            string line = rawLine.TrimEnd();
            if (!line.StartsWith('|') && table.Count > 0) FlushTable(table);

            if (line.StartsWith("```")) { inCode = !inCode; continue; }

            if (inCode) Append("    " + line + "\n", T.Mono, T.AccentAlt);
            else if (line.StartsWith("# ")) Append(line[2..] + "\n", H1, T.Accent, spaceBefore: 0, spaceAfter: 6);
            else if (line.StartsWith("## ")) Append(line[3..] + "\n", H2, T.Text, spaceBefore: 14, spaceAfter: 4);
            else if (line.StartsWith("### ")) Append(line[4..] + "\n", H3, T.Accent, spaceBefore: 8, spaceAfter: 2);
            else if (line.StartsWith('|'))
            {
                bool separator = line.Replace("|", "").Replace("-", "").Replace(":", "").Trim().Length == 0;
                if (!separator) table.Add(line.Trim('|').Split('|').Select(c => c.Trim()).ToArray());
            }
            else if (line.StartsWith("- ")) AppendInline("  •  " + line[2..] + "\n", indent: 14);
            else if (line.Length == 0) Append("\n", Body, T.Text);
            else AppendInline(line + "\n", indent: 0);
        }
        if (table.Count > 0) FlushTable(table);
    }

    /// <summary>Monospace table: first row as header, first column padded to the widest cell.</summary>
    private void FlushTable(List<string[]> rows)
    {
        int width = rows.Max(r => r[0].Length) + 3;
        for (int k = 0; k < rows.Count; k++)
        {
            string text = "  " + rows[k][0].PadRight(width) + string.Join("   ", rows[k].Skip(1)) + "\n";
            if (k == 0) Append(text, T.Mono, T.TextMuted);
            else Append(text, T.Mono, T.Text);
        }
        rows.Clear();
    }

    /// <summary>Plain text with **bold** and `code` spans.</summary>
    private void AppendInline(string text, int indent)
    {
        _box.SelectionIndent = indent;
        int k = 0;
        while (k < text.Length)
        {
            int bold = text.IndexOf("**", k, StringComparison.Ordinal);
            int code = text.IndexOf('`', k);
            int next = bold < 0 ? code : code < 0 ? bold : Math.Min(bold, code);
            if (next < 0) { Append(text[k..], Body, T.Text, indent: indent); break; }

            if (next > k) Append(text[k..next], Body, T.Text, indent: indent);
            bool isBold = next == bold;
            int open = next + (isBold ? 2 : 1);
            int close = isBold ? text.IndexOf("**", open, StringComparison.Ordinal) : text.IndexOf('`', open);
            if (close < 0) { Append(text[next..], Body, T.Text, indent: indent); break; }

            Append(text[open..close], isBold ? Bold : T.Mono, isBold ? T.Text : T.Accent, indent: indent);
            k = close + (isBold ? 2 : 1);
        }
    }

    private void Append(string text, Font font, Color color, int spaceBefore = 0, int spaceAfter = 0, int indent = 0)
    {
        if (spaceBefore > 0) AppendSpacer(spaceBefore);
        _box.SelectionStart = _box.TextLength;
        _box.SelectionLength = 0;
        _box.SelectionFont = font;
        _box.SelectionColor = color;
        _box.SelectionIndent = indent;
        _box.AppendText(text);
        if (spaceAfter > 0) AppendSpacer(spaceAfter);
    }

    private static readonly Dictionary<int, Font> SpacerFonts = [];

    private void AppendSpacer(int points)
    {
        if (!SpacerFonts.TryGetValue(points, out var font))
            SpacerFonts[points] = font = new Font("Segoe UI", points);
        _box.SelectionStart = _box.TextLength;
        _box.SelectionFont = font;
        _box.AppendText("\n");
    }
}
