namespace BezierSurfaceRenderer.App.Theme;

/// <summary>Single source of truth for colours and fonts; every control reads from here.</summary>
internal static class Theme
{
    public static readonly Color Background   = ColorTranslator.FromHtml("#0B0E14");
    public static readonly Color Panel        = ColorTranslator.FromHtml("#11151D");
    public static readonly Color Border       = ColorTranslator.FromHtml("#1E2633");
    public static readonly Color Text         = ColorTranslator.FromHtml("#E6EDF3");
    public static readonly Color TextMuted    = ColorTranslator.FromHtml("#8B98A9");
    public static readonly Color Accent       = ColorTranslator.FromHtml("#22D3EE"); // cyan: wireframe, slider fill
    public static readonly Color AccentAlt    = ColorTranslator.FromHtml("#A78BFA"); // violet: light path
    public static readonly Color ControlNet   = ColorTranslator.FromHtml("#F5B942"); // amber: Bézier polygon
    public static readonly Color LightMarker  = ColorTranslator.FromHtml("#FFF4C2");

    public static readonly Font Ui      = new("Segoe UI", 9f);
    public static readonly Font Section = new("Segoe UI Semibold", 8f);
    public static readonly Font Mono    = new("Cascadia Mono", 9f); // falls back to Consolas if missing

    /// <summary>Dark title bar (DWMWA_USE_IMMERSIVE_DARK_MODE), dark menu renderer.</summary>
    public static void Apply(Form form) => throw new NotImplementedException();
}
