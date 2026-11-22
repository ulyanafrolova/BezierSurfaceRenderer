using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>
/// Point light moving along a closed Bézier curve in the plane z = Z (spec §4.5).
/// The curve has degree n = Points.Count and control points V0 … V(n−1), Vn = V0 (closing point is
/// not stored, so dragging V0 also moves Vn). The default curve has 8 distinct points (V8 = V0).
/// The path lives in the observer's frame: it does not rotate with the scene.
/// </summary>
public sealed class LightSource
{
    public const int MinimumPoints = 7;

    public List<Vector2> Points { get; }
    public float Z { get; set; }
    public bool IsRunning { get; set; } = true;

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

    /// <summary>De Casteljau on V0 … V(n−1), V0; t wraps to [0,1).</summary>
    public Vector2 Evaluate(float t)
    {
        t -= MathF.Floor(t);
        int n = Points.Count;
        Span<Vector2> q = stackalloc Vector2[n + 1];
        for (int k = 0; k < n; k++) q[k] = Points[k];
        q[n] = Points[0];

        for (int level = n; level > 0; level--)
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
