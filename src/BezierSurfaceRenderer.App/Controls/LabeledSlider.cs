namespace BezierSurfaceRenderer.App.Controls;

/// <summary>
/// Owner-drawn slider in the app theme: caption on the left, monospace value readout on the right,
/// thin track with an accent-coloured fill and a round thumb. Keyboard (arrows/PgUp/PgDn) and
/// mouse wheel supported. Maps an integer step range onto a float range with a display format.
/// </summary>
public sealed class LabeledSlider : Control
{
    public string Caption { get; set; } = "";
    public float Minimum { get; set; }
    public float Maximum { get; set; } = 1;
    public float Step { get; set; } = 0.01f;
    public string Format { get; set; } = "0.00";
    public float Value { get; set; }

    public event EventHandler? ValueChanged;
}
