using System.Runtime.InteropServices;

namespace BezierSurfaceRenderer.App.Theme;

/// <summary>Single source of truth for colours and fonts; every control reads from here.</summary>
internal static class Theme
{
    public static readonly Color Background   = ColorTranslator.FromHtml("#0B0E14");
    public static readonly Color Panel        = ColorTranslator.FromHtml("#11151D");
    public static readonly Color PanelRaised  = ColorTranslator.FromHtml("#171C26");
    public static readonly Color Border       = ColorTranslator.FromHtml("#1E2633");
    public static readonly Color Text         = ColorTranslator.FromHtml("#E6EDF3");
    public static readonly Color TextMuted    = ColorTranslator.FromHtml("#8B98A9");
    public static readonly Color Accent       = ColorTranslator.FromHtml("#22D3EE"); // cyan: wireframe, slider fill
    public static readonly Color AccentAlt    = ColorTranslator.FromHtml("#A78BFA"); // violet: light path
    public static readonly Color ControlNet   = ColorTranslator.FromHtml("#F5B942"); // amber: Bézier polygon
    public static readonly Color LightMarker  = ColorTranslator.FromHtml("#FFF4C2");

    public static readonly Font Ui      = new("Segoe UI", 9f);
    public static readonly Font Section = new("Segoe UI Semibold", 8f);
    public static readonly Font Title   = new("Segoe UI Semibold", 11f);
    public static readonly Font Mono    = CreateFont("Cascadia Mono", "Consolas", 9f);
    public static readonly Font MonoSmall = CreateFont("Cascadia Mono", "Consolas", 8f);

    /// <summary>Dark title bar (DWMWA_USE_IMMERSIVE_DARK_MODE), dark menu renderer.</summary>
    public static void Apply(Form form)
    {
        form.BackColor = Panel;
        form.ForeColor = Text;
        form.Font = Ui;
        form.HandleCreated += (_, _) => UseDarkTitleBar(form.Handle);
        if (form.IsHandleCreated) UseDarkTitleBar(form.Handle);
    }

    public static void Apply(MenuStrip menu)
    {
        menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false };
        menu.BackColor = Panel;
        menu.ForeColor = Text;
        foreach (ToolStripItem item in menu.Items) ApplyRecursive(item);
    }

    private static void ApplyRecursive(ToolStripItem item)
    {
        item.ForeColor = Text;
        if (item is ToolStripMenuItem menuItem)
            foreach (ToolStripItem child in menuItem.DropDownItems) ApplyRecursive(child);
    }

    /// <summary>GDI+ silently substitutes a missing font; fall back explicitly so numbers stay monospace.</summary>
    private static Font CreateFont(string preferred, string fallback, float size)
    {
        var font = new Font(preferred, size);
        if (string.Equals(font.Name, preferred, StringComparison.OrdinalIgnoreCase)) return font;
        font.Dispose();
        return new Font(fallback, size);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private static void UseDarkTitleBar(IntPtr handle)
    {
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        int enabled = 1;
        try { DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int)); }
        catch (DllNotFoundException) { /* pre-Windows 10: keep the default title bar */ }
        catch (EntryPointNotFoundException) { }
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuStripGradientBegin => Panel;
        public override Color MenuStripGradientEnd => Panel;
        public override Color ToolStripDropDownBackground => PanelRaised;
        public override Color ImageMarginGradientBegin => PanelRaised;
        public override Color ImageMarginGradientMiddle => PanelRaised;
        public override Color ImageMarginGradientEnd => PanelRaised;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Accent;
        public override Color MenuItemSelected => Border;
        public override Color MenuItemSelectedGradientBegin => Border;
        public override Color MenuItemSelectedGradientEnd => Border;
        public override Color MenuItemPressedGradientBegin => PanelRaised;
        public override Color MenuItemPressedGradientEnd => PanelRaised;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color CheckBackground => Border;
        public override Color CheckSelectedBackground => Border;
        public override Color CheckPressedBackground => Border;
    }
}
