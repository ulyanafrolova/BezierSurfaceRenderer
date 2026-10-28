using BezierSurfaceRenderer.App.Controls;
using BezierSurfaceRenderer.Core.SceneModel;

namespace BezierSurfaceRenderer.App;

/// <summary>
/// Layout (built in code, no designer file):
///   MenuStrip   File (Load texture Ctrl+T, Load normal map Ctrl+N, Exit) · Colors (Light color…, Object color…)
///               · Help (Documentation F1)
///   Left        ViewportControl (fills)
///   Right       control panel, grouped:
///     GEOMETRY  slider "Triangulation" · checkboxes Control net / Wireframe / Filled
///     MATERIAL  sliders kd, ks, m · radios Solid color / Texture
///     SURFACE   checkbox Normal map
///     LIGHT     slider Light z · button/checkbox Pause animation · checkbox Edit light path
/// Owns the render loop timer (≈60 Hz while the light animates; on-demand redraw otherwise).
/// Keyboard shortcuts are handled in ProcessCmdKey so they work regardless of focused control.
/// </summary>
public sealed class MainForm : Form
{
    public MainForm(Scene scene) => throw new NotImplementedException();
}
