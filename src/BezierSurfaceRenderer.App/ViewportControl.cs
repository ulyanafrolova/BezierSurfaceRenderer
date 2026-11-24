using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;

namespace BezierSurfaceRenderer.App;

/// <summary>
/// Drawing area. Per frame:
///   1. SceneRenderer fills the FrameBuffer (if "Filled" is on) → copied into a Bitmap via LockBits.
///   2. Overlays with GDI+ using the transform (ScaleTransform(1,−1) + TranslateTransform(W/2, −H/2)),
///      so the origin is at the centre, y up: wireframe, Bézier control nets, light path + light marker.
/// Mouse: left drag → Arcball.TrackballIncrement;
/// in "edit light path" mode left drag moves curve control points instead (rotation disabled).
/// Pixel scale = 0.5·min(W,H) / (1.05·r), r = radius of the sphere around all control points; by the
/// convex-hull property the surfaces stay inside it, so they fit the viewport at any rotation.
/// </summary>
public sealed class ViewportControl : Control
{
    private enum DragMode { None, Trackball, LightPoint }

    private static readonly Color Background = Color.FromArgb(24, 24, 28);
    private static readonly Color MeshColor = Color.FromArgb(0, 200, 230);
    private static readonly Color NetColor = Color.Orange;
    private static readonly Color PathColor = Color.MediumPurple;

    /// <summary>Colours of the B-spline's Bézier segments (8 points → 8 segments; reused cyclically if there are more).</summary>
    private static readonly Color[] SegmentColors =
    [
        Color.Red, Color.Orange, Color.Yellow, Color.LimeGreen,
        Color.Cyan, Color.DodgerBlue, Color.MediumPurple, Color.HotPink,
    ];

    private const float FitMargin = 1.05f;
    private static readonly TimeSpan MaxStep = TimeSpan.FromSeconds(0.25);
    private const float HandleHitPixels = 9f;

    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private TimeSpan _lastPaint;
    private FrameBuffer? _frame;
    private Bitmap? _bitmap;
    private DragMode _drag;
    private Point _lastMouse;
    private int _dragPoint = -1;

    public ViewportControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Background;
        TabStop = true;
    }

    public Scene? Scene { get; set; }
    public LightSource? Light { get; set; }

    /// <summary>Supplies the current material/light settings; called once per rendered frame.</summary>
    public Func<RenderSettings>? SettingsSource { get; set; }

    public bool ShowControlNet { get; set; }
    public bool ShowWireframe { get; set; } = true;
    public bool ShowFilled { get; set; } = true;

    private bool _editLightPath;
    public bool EditLightPath
    {
        get => _editLightPath;
        set { _editLightPath = value; Invalidate(); }
    }

    private float PixelsPerUnit =>
        0.5f * Math.Min(ClientSize.Width, ClientSize.Height) / (FitMargin * (Scene?.BoundingRadius ?? 1f));

    // ---------------------------------------------------------------- coordinates

    /// <summary>Mouse position relative to the centre, y up, in pixels (arcball space).</summary>
    private Vector2 ToCentred(Point p) => new(p.X - ClientSize.Width / 2f, ClientSize.Height / 2f - p.Y);

    /// <summary>Mouse position in model units (light-path space).</summary>
    private Vector2 ToModel(Point p) => ToCentred(p) / PixelsPerUnit;

    private PointF ToOverlay(float x, float y) => new(x * PixelsPerUnit, y * PixelsPerUnit);
    private PointF ToOverlay(Vector3 p) => ToOverlay(p.X, p.Y);
    private PointF ToOverlay(Vector2 p) => ToOverlay(p.X, p.Y);

    // ---------------------------------------------------------------- mouse

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        _lastMouse = e.Location;

        if (EditLightPath)
        {
            if (e.Button == MouseButtons.Left && Light != null)
            {
                _dragPoint = Light.HitTest(ToModel(e.Location), HandleHitPixels / PixelsPerUnit) ?? -1;
                _drag = _dragPoint >= 0 ? DragMode.LightPoint : DragMode.None;
            }
            return;
        }

        _drag = e.Button == MouseButtons.Left ? DragMode.Trackball : DragMode.None;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        switch (_drag)
        {
            case DragMode.Trackball when Scene != null:
                float radius = 0.5f * Math.Min(ClientSize.Width, ClientSize.Height);
                Scene.Rotate(Arcball.TrackballIncrement(ToCentred(_lastMouse), ToCentred(e.Location), radius));
                Invalidate();
                break;
            case DragMode.LightPoint when Light != null:
                Light.Points[_dragPoint] = ToModel(e.Location);
                Invalidate();
                break;
        }
        _lastMouse = e.Location; // incremental: every move is measured from the previous position
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _drag = DragMode.None;
        _dragPoint = -1;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _bitmap?.Dispose();
        _bitmap = null;
        _frame = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _bitmap?.Dispose();
        base.Dispose(disposing);
    }

    // ---------------------------------------------------------------- painting

    protected override void OnPaint(PaintEventArgs e)
    {
        // Animate here rather than in the timer: while a slider or the scene is dragged, the WinForms timer
        // gets no messages (WM_TIMER has the lowest priority), but every repaint still moves the light.
        // The step is capped, so after a pause (or a minimised window) the light does not jump ahead.
        var now = _clock.Elapsed;
        var dt = now - _lastPaint;
        Light?.Advance(dt < MaxStep ? dt : MaxStep);
        _lastPaint = now;

        var g = e.Graphics;
        int w = ClientSize.Width, h = ClientSize.Height;
        g.Clear(Background);
        if (Scene == null || w <= 0 || h <= 0) return;

        var settings = SettingsSource?.Invoke();

        if (ShowFilled && settings != null)
        {
            EnsureBuffers(w, h);
            _frame!.PixelsPerUnit = PixelsPerUnit;
            _frame.Clear(Background.ToArgb());
            SceneRenderer.RenderFilled(_frame, Scene.Meshes, settings);
            BitmapInterop.CopyToBitmap(_frame, _bitmap!);
            // 1:1 copy; explicit size so GDI+ never rescales by the bitmap's DPI.
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.DrawImage(_bitmap!, new Rectangle(0, 0, w, h), 0, 0, w, h, GraphicsUnit.Pixel);
            g.CompositingMode = CompositingMode.SourceOver;
        }

        // Origin in the centre, y up.
        var state = g.Save();
        g.ScaleTransform(1, -1);
        g.TranslateTransform(w / 2f, -h / 2f);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (ShowWireframe) DrawWireframe(g);
        if (ShowControlNet) DrawControlNets(g);
        if (Light != null) DrawLight(g, settings);

        g.Restore(state);
    }

    private void EnsureBuffers(int w, int h)
    {
        if (_frame != null && _frame.Width == w && _frame.Height == h) return;
        _bitmap?.Dispose();
        _frame = new FrameBuffer(w, h);
        _bitmap = new Bitmap(w, h, PixelFormat.Format32bppPArgb); // alpha is always 255, so ARGB == PARGB
    }

    private void DrawWireframe(Graphics g)
    {
        using var path = new GraphicsPath();
        var tri = new PointF[3];
        foreach (var mesh in Scene!.Meshes)
        foreach (var t in mesh.Triangles)
        {
            tri[0] = ToOverlay(t.A.Rotated.P);
            tri[1] = ToOverlay(t.B.Rotated.P);
            tri[2] = ToOverlay(t.C.Rotated.P);
            path.AddPolygon(tri);
        }
        using var pen = new Pen(Color.FromArgb(ShowFilled ? 110 : 190, MeshColor), 1f);
        g.DrawPath(pen, path);
    }

    private void DrawControlNets(Graphics g)
    {
        using var pen = new Pen(NetColor, 1.5f);
        using var dot = new SolidBrush(NetColor);
        var line = new PointF[4];
        foreach (var surface in Scene!.Surfaces)
        {
            for (int a = 0; a < 4; a++)
            {
                for (int b = 0; b < 4; b++) line[b] = ToOverlay(Scene.RotatedControlPoint(surface, a, b));
                g.DrawLines(pen, line);
                for (int b = 0; b < 4; b++) line[b] = ToOverlay(Scene.RotatedControlPoint(surface, b, a));
                g.DrawLines(pen, line);
            }
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                var p = ToOverlay(Scene.RotatedControlPoint(surface, i, j));
                g.FillRectangle(dot, p.X - 3, p.Y - 3, 6, 6);
            }
        }
    }

    private void DrawLight(Graphics g, RenderSettings? settings)
    {
        var light = Light!;

        // Every Bézier segment of the path is drawn on its own: the single Bézier curve in PathColor,
        // the B-spline's segments each in a different colour.
        var segments = light.Segments();
        const int samplesPerSegment = 60;
        var pts = new PointF[samplesPerSegment + 1];
        for (int s = 0; s < segments.Length; s++)
        {
            for (int k = 0; k <= samplesPerSegment; k++)
                pts[k] = ToOverlay(LightSource.DeCasteljau(segments[s], (float)k / samplesPerSegment));
            Color color = light.UseBSpline ? SegmentColors[s % SegmentColors.Length] : PathColor;
            using var pen = new Pen(Color.FromArgb(EditLightPath ? 230 : 170, color), EditLightPath || light.UseBSpline ? 2f : 1.5f);
            if (!EditLightPath && !light.UseBSpline) pen.DashPattern = [4f, 3f];
            g.DrawLines(pen, pts);
        }

        if (EditLightPath)
        {
            // Control polygon V0 … V(n−1) closed back to V0 (= Vn); V0 drawn larger.
            using var polygonPen = new Pen(Color.FromArgb(150, PathColor), 1f);
            using var pointBrush = new SolidBrush(PathColor);
            g.DrawPolygon(polygonPen, light.Points.Select(p => ToOverlay(p)).ToArray());
            for (int k = 0; k < light.Points.Count; k++)
            {
                var p = ToOverlay(light.Points[k]);
                float r = k == 0 ? 7 : 5;
                g.FillRectangle(pointBrush, p.X - r, p.Y - r, 2 * r, 2 * r);
            }

            // B-spline construction: the Bézier control points of each segment (edge thirds and joints).
            if (light.UseBSpline)
                for (int s = 0; s < segments.Length; s++)
                {
                    using var brush = new SolidBrush(SegmentColors[s % SegmentColors.Length]);
                    foreach (var q in segments[s].Take(3))
                    {
                        var p = ToOverlay(q);
                        g.FillEllipse(brush, p.X - 3, p.Y - 3, 6, 6);
                    }
                }
        }

        // Current position of every light on the curve, filled with its colour (yellow for a white light).
        foreach (var source in settings?.Lights ?? [])
        {
            var c = ToOverlay(source.Position);
            var rgb = source.Color;
            Color fill = rgb == Vector3.One ? Color.Yellow : Color.FromArgb((int)(rgb.X * 255), (int)(rgb.Y * 255), (int)(rgb.Z * 255));
            using var brush = new SolidBrush(fill);
            const float r = 10f; // large enough to tell the red, green and blue lights apart at a glance
            g.FillEllipse(brush, c.X - r, c.Y - r, 2 * r, 2 * r);
            using var outline = new Pen(Color.White, 1.5f);
            g.DrawEllipse(outline, c.X - r, c.Y - r, 2 * r, 2 * r);

            // Spotlight: a short line from the light along its beam axis (projected onto the screen).
            if (source.SpotExponent > 0)
            {
                using var beam = new Pen(fill, 2f);
                var tip = ToOverlay(source.Position + 0.4f * source.SpotDirection);
                g.DrawLine(beam, c, tip);
            }
        }
    }
}
