using BezierSurfaceRenderer.Core.Interaction;
using BezierSurfaceRenderer.Core.Rendering;
using BezierSurfaceRenderer.Core.SceneModel;

namespace BezierSurfaceRenderer.App.Controls;

/// <summary>
/// Drawing area. Per frame:
///   1. SceneRenderer fills the FrameBuffer (if "Filled" is on) → copied into a Bitmap via LockBits.
///   2. Overlays with GDI+ using the spec's transform (ScaleTransform(1,−1) + TranslateTransform(W/2, −H/2)),
///      so the origin is at the centre, y up: wireframe, Bézier control nets, light path + light marker,
///      orientation gizmo, HUD text.
/// Mouse: left drag → ArcballController.TrackballIncrement; Shift/right drag → RollIncrement;
/// in "edit light path" mode left drag moves curve control points instead (rotation disabled).
/// Pixel scale = 0.5·min(W,H) / (√3 · 1.05) so the scene fits at any rotation; recomputed on resize.
/// </summary>
public sealed class ViewportControl : Control
{
    public ViewportControl() => DoubleBuffered = true;

    public Scene? Scene { get; set; }
    public LightAnimator? Light { get; set; }
    public RenderSettings? Settings { get; set; }

    public bool ShowControlNet { get; set; }
    public bool ShowWireframe { get; set; } = true;
    public bool ShowFilled { get; set; } = true;
    public bool EditLightPath { get; set; }
}
