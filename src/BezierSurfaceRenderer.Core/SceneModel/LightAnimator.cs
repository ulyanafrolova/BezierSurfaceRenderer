using System.Numerics;
using BezierSurfaceRenderer.Core.Geometry;

namespace BezierSurfaceRenderer.Core.SceneModel;

/// <summary>
/// Point light moving along a closed Bézier path in the view-space plane z = Z (spec §4).
/// The path is not affected by scene rotation (it is defined relative to the observer).
/// </summary>
public sealed class LightAnimator
{
    public ClosedBezierCurve Path { get; }
    public float Z { get; set; }
    public bool IsRunning { get; set; } = true;

    /// <summary>Full loops per second.</summary>
    public float Speed { get; set; } = 0.1f;

    /// <summary>Curve parameter t ∈ [0,1).</summary>
    public float Phase { get; private set; }

    public LightAnimator(ClosedBezierCurve path, float z) => throw new NotImplementedException();

    /// <summary>Advances Phase by Speed·dt when running (dt-based, so speed is frame-rate independent).</summary>
    public void Advance(TimeSpan dt) => throw new NotImplementedException();

    public Vector3 Position => throw new NotImplementedException();
}
