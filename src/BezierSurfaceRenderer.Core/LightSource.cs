using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>
/// Point light moving along a closed curve in the plane z = Z (spec §4.5). Two kinds of path:
///  • Bézier: one curve of degree n = Points.Count with control points V0 … V(n−1), Vn = V0 (the closing
///    point is not stored, so dragging V0 also moves Vn). The default curve has 8 distinct points (V8 = V0).
///  • B-spline: V0 … V(n−1) are the vertices of a closed B-spline polygon; the cubic B-spline is built
///    geometrically as n cubic Bézier segments (see <see cref="BSplineSegments"/>).
/// The path lives in the observer's frame: it does not rotate with the scene.
/// </summary>
public sealed class LightSource
{
    public const int MinimumPoints = 7;

    public List<Vector2> Points { get; }
    public float Z { get; set; }
    public bool IsRunning { get; set; } = true;

    /// <summary>false: one Bézier curve of degree n; true: closed cubic B-spline (n Bézier segments).</summary>
    public bool UseBSpline { get; set; }

    /// <summary>Full loops per second.</summary>
    public float Speed { get; set; } = 0.1f;

    /// <summary>Curve parameter t ∈ [0,1).</summary>
    public float Phase { get; private set; }

    public LightSource(IEnumerable<Vector2> points, float z)
    {
        Points = points.ToList();
        if (Points.Count < MinimumPoints)
            throw new ArgumentException($"The light path needs at least {MinimumPoints} control points.", nameof(points));
        Z = z;
    }

    /// <summary>Default path: 8 control points on an ellipse (the curve runs inside them).</summary>
    public static LightSource CreateDefault(float radius, float z)
    {
        const int n = 8;
        var points = Enumerable.Range(0, n).Select(k =>
        {
            float a = 2 * MathF.PI * k / n;
            return new Vector2(radius * MathF.Cos(a), 0.75f * radius * MathF.Sin(a));
        });
        return new LightSource(points, z);
    }

    public Vector3 Position
    {
        get
        {
            Vector2 xy = Evaluate(Phase);
            return new Vector3(xy.X, xy.Y, Z);
        }
    }

    /// <summary>Advances Phase by Speed·dt when running (frame-rate independent).</summary>
    public void Advance(TimeSpan dt)
    {
        if (!IsRunning) return;
        float next = Phase + Speed * (float)dt.TotalSeconds;
        Phase = next - MathF.Floor(next);
    }

    /// <summary>
    /// Point of the current path for t ∈ [0,1) (t wraps). Bézier: de Casteljau on V0 … V(n−1), V0.
    /// B-spline: t·n selects segment k = ⌊t·n⌋ and the local parameter t·n − k inside it.
    /// </summary>
    public Vector2 Evaluate(float t)
    {
        t -= MathF.Floor(t);
        return UseBSpline ? EvaluateSegment(BSplineSegments(), t) : DeCasteljau([.. Points, Points[0]], t);
    }

    /// <summary>Bézier segments of the current path: the whole degree-n curve, or the n cubic B-spline pieces.</summary>
    public Vector2[][] Segments() => UseBSpline ? BSplineSegments() : [[.. Points, Points[0]]];

    /// <summary>
    /// Geometric construction of the closed cubic B-spline (no basis-function formulas):
    ///   1. every edge V(i) V(i+1) of the closed polygon is divided into three equal parts by
    ///      A(i) = V(i) + ⅓(V(i+1) − V(i)) and B(i) = V(i) + ⅔(V(i+1) − V(i));
    ///   2. around every vertex V(i) the neighbouring division points B(i−1) and A(i) are joined and the
    ///      segment is halved: S(i) = ½(B(i−1) + A(i));
    ///   3. segment i is the cubic Bézier curve with control points S(i), A(i), B(i), S(i+1).
    /// A closed polygon of n vertices therefore gives n segments (indices mod n).
    /// </summary>
    public Vector2[][] BSplineSegments()
    {
        int n = Points.Count;
        var a = new Vector2[n];
        var b = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 from = Points[i], to = Points[(i + 1) % n];
            a[i] = Vector2.Lerp(from, to, 1f / 3f);
            b[i] = Vector2.Lerp(from, to, 2f / 3f);
        }

        Vector2 Joint(int i) => Vector2.Lerp(b[(i + n - 1) % n], a[i], 0.5f);
        return Enumerable.Range(0, n).Select(i => new[] { Joint(i), a[i], b[i], Joint((i + 1) % n) }).ToArray();
    }

    /// <summary>Point of a chain of Bézier segments, each taking an equal share of t ∈ [0,1).</summary>
    private static Vector2 EvaluateSegment(Vector2[][] segments, float t)
    {
        float scaled = t * segments.Length;
        int k = Math.Min((int)scaled, segments.Length - 1);
        return DeCasteljau(segments[k], scaled - k);
    }

    /// <summary>De Casteljau algorithm: repeated linear interpolation of the control polygon.</summary>
    public static Vector2 DeCasteljau(Vector2[] controlPoints, float t)
    {
        var q = (Vector2[])controlPoints.Clone();
        for (int level = q.Length - 1; level > 0; level--)
            for (int k = 0; k < level; k++)
                q[k] = Vector2.Lerp(q[k], q[k + 1], t);
        return q[0];
    }

    /// <summary>Index of the control point nearest to <paramref name="position"/> within <paramref name="radius"/>.</summary>
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
}
