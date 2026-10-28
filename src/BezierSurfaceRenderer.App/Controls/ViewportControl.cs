using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using BezierSurfaceRenderer.App.Imaging;
using BezierSurfaceRenderer.Core.Geometry;
using BezierSurfaceRenderer.Core.Interaction;
using BezierSurfaceRenderer.Core.Rendering;
using BezierSurfaceRenderer.Core.SceneModel;
using T = BezierSurfaceRenderer.App.Theme.Theme;

namespace BezierSurfaceRenderer.App.Controls;

/// <summary>
/// Drawing area. Per frame:
///   1. SceneRenderer fills the FrameBuffer (if "Filled" is on) → copied into a Bitmap via LockBits.
///   2. Overlays with GDI+ using the spec's transform (ScaleTransform(1,−1) + TranslateTransform(W/2, −H/2)),
///      so the origin is at the centre, y up: wireframe, Bézier control nets, light path + light marker.
///   3. HUD and orientation gizmo in plain screen coordinates.
/// Mouse: left drag → ArcballController.TrackballIncrement; Shift+left or right drag → RollIncrement;
/// in "edit light path" mode left drag moves curve control points instead (rotation disabled).
/// Pixel scale = 0.5·min(W,H) / (1.05·r), r = radius of the sphere around all control points; by the
/// convex-hull property the surfaces stay inside it, so they fit the viewport at any rotation.
/// </summary>
public sealed class ViewportControl : Control
{
    private enum DragMode { None, Trackball, Roll, LightPoint }

    private const float FitMargin = 1.05f;
    private const float HandleHitPixels = 9f;

    private readonly SceneRenderer _renderer = new();
    private readonly ArcballController _arcball = new();
    private readonly Stopwatch _frameClock = new();

    private FrameBuffer? _frame;
    private Bitmap? _bitmap;
    private DragMode _drag;
    private Point _lastMouse;
    private int _dragPoint = -1;
    private int _hoverPoint = -1;
    private double _lastFrameMs;

    public ViewportControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = T.Background;
        TabStop = true;
    }

    public Scene? Scene { get; set; }
    public LightAnimator? Light { get; set; }

    /// <summary>Supplies the current material/light settings; called once per rendered frame.</summary>
    public Func<RenderSettings>? SettingsSource { get; set; }

    public bool ShowControlNet { get; set; }
    public bool ShowWireframe { get; set; } = true;
    public bool ShowFilled { get; set; } = true;

    private bool _editLightPath;
    public bool EditLightPath
    {
        get => _editLightPath;
        set { _editLightPath = value; _hoverPoint = -1; Cursor = Cursors.Default; Invalidate(); }
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
                _dragPoint = Light.Path.HitTest(ToModel(e.Location), HandleHitPixels / PixelsPerUnit) ?? -1;
                _drag = _dragPoint >= 0 ? DragMode.LightPoint : DragMode.None;
            }
            return;
        }

        _drag = e.Button switch
        {
            MouseButtons.Left when (ModifierKeys & Keys.Shift) != 0 => DragMode.Roll,
            MouseButtons.Left => DragMode.Trackball,
            MouseButtons.Right => DragMode.Roll,
            _ => DragMode.None,
        };
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        switch (_drag)
        {
            case DragMode.Trackball when Scene != null:
                _arcball.Radius = 0.5f * Math.Min(ClientSize.Width, ClientSize.Height);
                Scene.Rotate(_arcball.TrackballIncrement(ToCentred(_lastMouse), ToCentred(e.Location)));
                Invalidate();
                break;
            case DragMode.Roll when Scene != null:
                Scene.Rotate(_arcball.RollIncrement(ToCentred(_lastMouse), ToCentred(e.Location)));
                Invalidate();
                break;
            case DragMode.LightPoint when Light != null:
                Light.Path.MovePoint(_dragPoint, ToModel(e.Location));
                Invalidate();
                break;
            case DragMode.None when EditLightPath && Light != null:
                int hover = Light.Path.HitTest(ToModel(e.Location), HandleHitPixels / PixelsPerUnit) ?? -1;
                if (hover != _hoverPoint)
                {
                    _hoverPoint = hover;
                    Cursor = hover >= 0 ? Cursors.SizeAll : Cursors.Default;
                    Invalidate();
                }
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
        var g = e.Graphics;
        int w = ClientSize.Width, h = ClientSize.Height;
        g.Clear(T.Background);
        if (Scene == null || w <= 0 || h <= 0) return;

        _frameClock.Restart();
        var settings = SettingsSource?.Invoke();

        if (ShowFilled && settings != null)
        {
            EnsureBuffers(w, h);
            _frame!.PixelsPerUnit = PixelsPerUnit;
            _frame.Clear(T.Background.ToArgb());
            _renderer.RenderFilled(_frame, Scene.Meshes, settings);
            BitmapInterop.CopyToBitmap(_frame, _bitmap!);
            // 1:1 copy; explicit size so GDI+ never rescales by the bitmap's DPI.
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.DrawImage(_bitmap!, new Rectangle(0, 0, w, h), 0, 0, w, h, GraphicsUnit.Pixel);
            g.CompositingMode = CompositingMode.SourceOver;
        }

        // Origin in the centre, y up — exactly as suggested in the assignment.
        var state = g.Save();
        g.ScaleTransform(1, -1);
        g.TranslateTransform(w / 2f, -h / 2f);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        DrawAxesCross(g, w, h);
        if (ShowWireframe) DrawWireframe(g);
        if (ShowControlNet) DrawControlNets(g);
        if (Light != null) DrawLight(g);

        g.Restore(state);
        _lastFrameMs = _frameClock.Elapsed.TotalMilliseconds;

        DrawGizmo(g, h);
        DrawHud(g);
    }

    private void EnsureBuffers(int w, int h)
    {
        if (_frame != null && _frame.Width == w && _frame.Height == h) return;
        _bitmap?.Dispose();
        _frame = new FrameBuffer(w, h);
        _bitmap = new Bitmap(w, h, PixelFormat.Format32bppPArgb); // alpha is always 255, so ARGB == PARGB
    }

    private static void DrawAxesCross(Graphics g, int w, int h)
    {
        using var pen = new Pen(Color.FromArgb(28, T.Accent), 1);
        g.DrawLine(pen, -w / 2f, 0, w / 2f, 0);
        g.DrawLine(pen, 0, -h / 2f, 0, h / 2f);
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
        using var pen = new Pen(Color.FromArgb(ShowFilled ? 110 : 190, T.Accent), 1f);
        g.DrawPath(pen, path);
    }

    private void DrawControlNets(Graphics g)
    {
        using var pen = new Pen(T.ControlNet, 1.5f);
        using var dot = new SolidBrush(T.ControlNet);
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

    private void DrawLight(Graphics g)
    {
        var light = Light!;
        var curve = light.Path;

        const int samples = 240;
        var pts = new PointF[samples + 1];
        for (int k = 0; k <= samples; k++) pts[k] = ToOverlay(curve.Evaluate((float)k / samples));
        using (var pen = new Pen(Color.FromArgb(EditLightPath ? 230 : 150, T.AccentAlt), EditLightPath ? 2f : 1.5f))
        {
            if (!EditLightPath) pen.DashPattern = [4f, 3f];
            g.DrawLines(pen, pts);
        }

        if (EditLightPath)
        {
            using var handlePen = new Pen(Color.FromArgb(150, T.AccentAlt), 1f);
            using var jointBrush = new SolidBrush(T.AccentAlt);
            using var handleBrush = new SolidBrush(T.Background);
            using var hoverPen = new Pen(T.Text, 2f);
            int n = curve.Points.Count;
            for (int k = 0; k < n; k += 3)
            {
                g.DrawLine(handlePen, ToOverlay(curve.Points[(k + n - 1) % n]), ToOverlay(curve.Points[k]));
                g.DrawLine(handlePen, ToOverlay(curve.Points[k]), ToOverlay(curve.Points[k + 1]));
            }
            for (int k = 0; k < n; k++)
            {
                var p = ToOverlay(curve.Points[k]);
                var rect = new RectangleF(p.X - 5, p.Y - 5, 10, 10);
                if (ClosedBezierCurve.IsJoint(k)) g.FillRectangle(jointBrush, rect);
                else { g.FillEllipse(handleBrush, rect); g.DrawEllipse(handlePen, rect); }
                if (k == _hoverPoint || k == _dragPoint) g.DrawRectangle(hoverPen, p.X - 7, p.Y - 7, 14, 14);
            }
        }

        // Current light position on the curve: soft glow + core.
        var c = ToOverlay(light.Position);
        for (int ring = 4; ring >= 1; ring--)
        {
            float r = 4 + ring * 4;
            using var glow = new SolidBrush(Color.FromArgb(22 * (5 - ring), T.LightMarker));
            g.FillEllipse(glow, c.X - r, c.Y - r, 2 * r, 2 * r);
        }
        using var core = new SolidBrush(T.LightMarker);
        g.FillEllipse(core, c.X - 5, c.Y - 5, 10, 10);
    }

    /// <summary>Orientation gizmo: the rotated x/y/z axes, drawn back-to-front.</summary>
    private void DrawGizmo(Graphics g, int h)
    {
        var o = Scene!.Orientation;
        var centre = new PointF(56, h - 56);
        const float length = 34;
        (Vector3 axis, string label, Color color)[] axes =
        [
            (o.Column1, "x", Color.FromArgb(0xF8, 0x71, 0x71)),
            (o.Column2, "y", Color.FromArgb(0x4A, 0xDE, 0x80)),
            (o.Column3, "z", Color.FromArgb(0x60, 0xA5, 0xFA)),
        ];
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var ring = new Pen(T.Border, 1))
            g.DrawEllipse(ring, centre.X - length - 8, centre.Y - length - 8, 2 * (length + 8), 2 * (length + 8));

        foreach (var (axis, label, color) in axes.OrderBy(a => a.axis.Z))
        {
            var tip = new PointF(centre.X + axis.X * length, centre.Y - axis.Y * length);
            int alpha = axis.Z < 0 ? 110 : 255;
            using var pen = new Pen(Color.FromArgb(alpha, color), 2f);
            g.DrawLine(pen, centre, tip);
            TextRenderer.DrawText(g, label, T.MonoSmall,
                new Point((int)(centre.X + axis.X * (length + 9)) - 4, (int)(centre.Y - axis.Y * (length + 9)) - 7),
                Color.FromArgb(alpha, color));
        }
    }

    private void DrawHud(Graphics g)
    {
        int triangles = Scene!.Meshes.Sum(m => m.Triangles.Count);
        var lines = new List<string>
        {
            $"FRAME  {_lastFrameMs,6:0.0} ms",
            $"TRIS   {triangles,6:N0}",
        };
        if (Light != null)
        {
            var p = Light.Position;
            lines.Add($"LIGHT  {p.X,5:0.00} {p.Y,5:0.00} {p.Z,5:0.00}");
            lines.Add(Light.IsRunning ? "ANIM   running" : "ANIM   paused");
        }
        if (EditLightPath) lines.Add("MODE   edit light path");

        int y = 10;
        foreach (string line in lines)
        {
            TextRenderer.DrawText(g, line, T.MonoSmall, new Point(12, y), T.TextMuted);
            y += 15;
        }
    }
}
