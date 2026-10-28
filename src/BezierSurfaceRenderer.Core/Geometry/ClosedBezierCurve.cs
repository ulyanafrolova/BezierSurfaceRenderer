using System.Numerics;

namespace BezierSurfaceRenderer.Core.Geometry;

/// <summary>
/// Closed piecewise-cubic Bézier curve in the plane z = const (light path, spec §4).
/// Stored as K segments sharing endpoints: points [3k .. 3k+3] form segment k, the last
/// segment ends at point 0. Points with index % 3 == 0 are joints (on the curve); the others are
/// tangent handles. Editing keeps C1 continuity: dragging a joint carries its two handles along,
/// dragging a handle mirrors the opposite handle of the same joint.
/// </summary>
public sealed class ClosedBezierCurve
{
    public List<Vector2> Points { get; }
    public int SegmentCount => Points.Count / 3;

    public ClosedBezierCurve(IEnumerable<Vector2> points)
    {
        Points = points.ToList();
        if (Points.Count < 6 || Points.Count % 3 != 0)
            throw new ArgumentException("A closed cubic chain needs 3·K points (K ≥ 2).", nameof(points));
    }

    /// <summary>Default path: smooth loop (4 segments) inside the scene's xy extent.</summary>
    public static ClosedBezierCurve CreateDefault(float radius)
    {
        // Standard 4-arc circle approximation, slightly squashed so the motion is not perfectly uniform.
        const float k = 0.5523f;
        float rx = radius, ry = radius * 0.75f;
        return new ClosedBezierCurve(
        [
            new(rx, 0), new(rx, k * ry), new(k * rx, ry),
            new(0, ry), new(-k * rx, ry), new(-rx, k * ry),
            new(-rx, 0), new(-rx, -k * ry), new(-k * rx, -ry),
            new(0, -ry), new(k * rx, -ry), new(rx, -k * ry),
        ]);
    }

    public static bool IsJoint(int index) => index % 3 == 0;

    /// <summary>t ∈ [0,1) over the whole loop (wraps).</summary>
    public Vector2 Evaluate(float t)
    {
        t -= MathF.Floor(t);
        float s = t * SegmentCount;
        int seg = Math.Min((int)s, SegmentCount - 1);
        float w = s - seg, iw = 1 - w;

        Vector2 p0 = Points[3 * seg], p1 = Points[3 * seg + 1], p2 = Points[3 * seg + 2], p3 = Points[Wrap(3 * seg + 3)];
        return iw * iw * iw * p0 + 3 * iw * iw * w * p1 + 3 * iw * w * w * p2 + w * w * w * p3;
    }

    /// <summary>Moves a control point; for joints also moves both adjacent handles, for handles mirrors the opposite one.</summary>
    public void MovePoint(int index, Vector2 newPosition)
    {
        index = Wrap(index);
        if (IsJoint(index))
        {
            Vector2 delta = newPosition - Points[index];
            Points[index] = newPosition;
            Points[Wrap(index - 1)] += delta;
            Points[Wrap(index + 1)] += delta;
            return;
        }

        // Handle 3k+1 belongs to joint 3k (opposite handle 3k−1); handle 3k+2 belongs to joint 3k+3 (opposite 3k+4).
        bool outgoing = index % 3 == 1;
        int joint = Wrap(outgoing ? index - 1 : index + 1);
        int opposite = Wrap(outgoing ? index - 2 : index + 2);
        Points[index] = newPosition;
        Points[opposite] = 2 * Points[joint] - newPosition;
    }

    public int? HitTest(Vector2 position, float radius)
    {
        int? best = null;
        float bestDistance = radius;
        for (int k = 0; k < Points.Count; k++)
        {
            float d = Vector2.Distance(Points[k], position);
            if (d <= bestDistance) { bestDistance = d; best = k; }
        }
        return best;
    }

    private int Wrap(int index) => ((index % Points.Count) + Points.Count) % Points.Count;
}
