using System.Numerics;

namespace BezierSurfaceRenderer.Core.Geometry;

/// <summary>
/// Closed piecewise-cubic Bézier curve in the plane z = const (light path, spec §4).
/// Stored as K segments sharing endpoints: points [3k .. 3k+3] form segment k, the last
/// segment ends at point 0. Editing keeps C1 continuity by mirroring the neighbouring tangent
/// handle when a joint's handle is dragged.
/// </summary>
public sealed class ClosedBezierCurve
{
    public List<Vector2> Points { get; }
    public int SegmentCount => Points.Count / 3;

    public ClosedBezierCurve(IEnumerable<Vector2> points) => throw new NotImplementedException();

    /// <summary>Default path: smooth loop (4 segments) inside the scene's xy extent.</summary>
    public static ClosedBezierCurve CreateDefault(float radius) => throw new NotImplementedException();

    /// <summary>t ∈ [0,1) over the whole loop (wraps).</summary>
    public Vector2 Evaluate(float t) => throw new NotImplementedException();

    /// <summary>Moves a control point; for joints also moves both adjacent handles, for handles mirrors the opposite one.</summary>
    public void MovePoint(int index, Vector2 newPosition) => throw new NotImplementedException();

    public int? HitTest(Vector2 position, float radius) => throw new NotImplementedException();
}
