using System.Drawing.Drawing2D;
using T = BezierSurfaceRenderer.App.Theme.Theme;

namespace BezierSurfaceRenderer.App.Controls;

/// <summary>
/// Owner-drawn slider in the app theme: caption on the left, monospace value readout on the right,
/// thin track with an accent-coloured fill and a round thumb. Keyboard (arrows/PgUp/PgDn/Home/End)
/// and mouse wheel supported. Values snap to <see cref="Step"/> within [Minimum, Maximum].
/// Set Minimum, Maximum and Step before Value.
/// </summary>
public sealed class LabeledSlider : Control
{
    private const int Inset = 8;
    private const int TrackY = 32;
    private const int ThumbRadius = 7;

    private float _value;
    private bool _dragging;

    public LabeledSlider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        Height = 44;
        BackColor = T.Panel;
        Cursor = Cursors.Hand;
    }

    public string Caption { get; set; } = "";
    public float Minimum { get; set; }
    public float Maximum { get; set; } = 1;
    public float Step { get; set; } = 0.01f;
    public string Format { get; set; } = "0.00";

    public float Value
    {
        get => _value;
        set
        {
            float snapped = Minimum + MathF.Round((value - Minimum) / Step) * Step;
            snapped = Math.Clamp(snapped, Minimum, Maximum);
            if (snapped == _value) return;
            _value = snapped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ValueChanged;

    private float Fraction => Maximum > Minimum ? (_value - Minimum) / (Maximum - Minimum) : 0f;
    private float TrackWidth => Math.Max(1, Width - 2 * Inset);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        TextRenderer.DrawText(g, Caption, T.Ui, new Point(Inset - 3, 4), Focused ? T.Text : T.TextMuted);
        string text = _value.ToString(Format, System.Globalization.CultureInfo.InvariantCulture);
        var size = TextRenderer.MeasureText(text, T.Mono);
        TextRenderer.DrawText(g, text, T.Mono, new Point(Width - Inset - size.Width + 3, 5), T.Accent);

        float thumbX = Inset + Fraction * TrackWidth;
        using (var track = new SolidBrush(T.Border))
            g.FillRectangle(track, Inset, TrackY - 2, TrackWidth, 4);
        using (var fill = new SolidBrush(T.Accent))
            g.FillRectangle(fill, Inset, TrackY - 2, thumbX - Inset, 4);

        if (Focused)
            using (var glow = new SolidBrush(Color.FromArgb(60, T.Accent)))
                g.FillEllipse(glow, thumbX - ThumbRadius - 4, TrackY - ThumbRadius - 4, 2 * ThumbRadius + 8, 2 * ThumbRadius + 8);
        using (var thumb = new SolidBrush(T.Text))
            g.FillEllipse(thumb, thumbX - ThumbRadius, TrackY - ThumbRadius, 2 * ThumbRadius, 2 * ThumbRadius);
        using (var ring = new Pen(T.Accent, 2))
            g.DrawEllipse(ring, thumbX - ThumbRadius, TrackY - ThumbRadius, 2 * ThumbRadius, 2 * ThumbRadius);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        _dragging = true;
        SetFromX(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) SetFromX(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Value += Math.Sign(e.Delta) * Step;
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        float? next = e.KeyCode switch
        {
            Keys.Left or Keys.Down => _value - Step,
            Keys.Right or Keys.Up => _value + Step,
            Keys.PageDown => _value - 10 * Step,
            Keys.PageUp => _value + 10 * Step,
            Keys.Home => Minimum,
            Keys.End => Maximum,
            _ => null,
        };
        if (next is { } v) { Value = v; e.Handled = true; }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    private void SetFromX(int x) => Value = Minimum + Math.Clamp((x - Inset) / TrackWidth, 0f, 1f) * (Maximum - Minimum);
}
