using System.Numerics;
using static BezierSurfaceRenderer.Core.Tests.TestUtil;

namespace BezierSurfaceRenderer.Core.Tests;

public class LightPathTests
{
    private static void Near2(Vector2 a, Vector2 b, float eps = 1e-4f) =>
        Assert.True(Vector2.Distance(a, b) <= eps, $"expected {a}, got {b}");

    [Fact]
    public void DefaultPathHasAtLeastSevenPoints()
    {
        Assert.True(LightSource.CreateDefault(1f, 0f).Points.Count >= LightSource.MinimumPoints);
        Assert.Throws<ArgumentException>(() => new LightSource(new Vector2[6], 0f));
    }

    [Fact]
    public void CurveIsClosedAndStartsAtV0()
    {
        var light = LightSource.CreateDefault(1.2f, 0f);
        Near2(light.Points[0], light.Evaluate(0f));
        Near2(light.Points[0], light.Evaluate(0.99999f), 1e-3f);
        Near2(light.Evaluate(0.25f), light.Evaluate(1.25f));
    }

    [Fact]
    public void EvaluateMatchesBernsteinForm()
    {
        var light = LightSource.CreateDefault(1f, 0f);
        light.Points[3] = new Vector2(0.4f, -2f); // break symmetry
        int n = light.Points.Count;
        const float t = 0.37f;

        Vector2 expected = Vector2.Zero;
        for (int k = 0; k <= n; k++)
        {
            float binomial = 1;
            for (int j = 1; j <= k; j++) binomial = binomial * (n - j + 1) / j;
            expected += light.Points[k % n] * (binomial * MathF.Pow(t, k) * MathF.Pow(1 - t, n - k));
        }
        Near2(expected, light.Evaluate(t));
    }

    [Fact]
    public void DefaultPathCirclesAroundTheScene()
    {
        // The scene fits in the unit-ish cube; the light path should orbit it, not sit in its centre.
        var light = LightSource.CreateDefault(2.0f, 0f);
        for (int k = 0; k < 100; k++)
            Assert.InRange(light.Evaluate(k / 100f).Length(), 0.4f, 2.0f);
    }

    [Fact]
    public void HitTestFindsNearestWithinRadius()
    {
        var light = LightSource.CreateDefault(1f, 0f);
        Assert.Equal(0, light.HitTest(new Vector2(0.98f, 0.01f), 0.05f));
        Assert.Null(light.HitTest(new Vector2(0f, 0f), 0.05f));
    }

    [Fact]
    public void AnimationAdvancesWrapsAndPauses()
    {
        var light = LightSource.CreateDefault(1f, 2.5f);
        light.Speed = 0.5f;
        light.Advance(TimeSpan.FromSeconds(1.5));
        Near(0.75f, light.Phase);
        light.Advance(TimeSpan.FromSeconds(1));
        Near(0.25f, light.Phase);

        light.IsRunning = false;
        light.Advance(TimeSpan.FromSeconds(10));
        Near(0.25f, light.Phase);
        Near(2.5f, light.Position.Z);
    }

    [Fact]
    public void BSplineHasOneCubicSegmentPerVertexJoinedIntoAClosedCurve()
    {
        var light = LightSource.CreateDefault(1f, 0f);
        light.Points[2] = new Vector2(0.3f, 1.4f); // break symmetry
        var segments = light.BSplineSegments();
        int n = light.Points.Count;

        Assert.Equal(n, segments.Length);
        for (int i = 0; i < n; i++)
        {
            Assert.Equal(4, segments[i].Length);
            Near2(segments[i][3], segments[(i + 1) % n][0]); // end of one segment = start of the next
        }
    }

    [Fact]
    public void BSplineConstructionMatchesKnownCubicBSplineProperties()
    {
        var light = LightSource.CreateDefault(1f, 0f);
        light.Points[5] = new Vector2(-0.2f, -1.3f);
        var v = light.Points;
        int n = v.Count;
        var segments = light.BSplineSegments();

        for (int i = 0; i < n; i++)
        {
            // Inner Bézier points divide edge V(i)V(i+1) into thirds; the joint is (V(i−1) + 4V(i) + V(i+1)) / 6.
            Vector2 prev = v[(i + n - 1) % n], next = v[(i + 1) % n];
            Near2(v[i] + (next - v[i]) / 3f, segments[i][1]);
            Near2(v[i] + 2f * (next - v[i]) / 3f, segments[i][2]);
            Near2((prev + 4f * v[i] + next) / 6f, segments[i][0]);

            // Tangent continuity at the joint (C1): S(i) is the midpoint of B(i−1) and A(i).
            Near2(segments[i][0] - segments[(i + n - 1) % n][2], segments[i][1] - segments[i][0]);
        }
    }

    [Fact]
    public void BSplinePathVisitsSegmentsInOrder()
    {
        var light = LightSource.CreateDefault(1f, 0f);
        light.UseBSpline = true;
        var segments = light.BSplineSegments();
        int n = segments.Length;
        for (int k = 0; k < n; k++)
        {
            Near2(segments[k][0], light.Evaluate((float)k / n));
            Near2(LightSource.DeCasteljau(segments[k], 0.5f), light.Evaluate((k + 0.5f) / n));
        }
        Near2(segments[0][0], light.Evaluate(0.99999f), 1e-3f);
    }

    [Fact]
    public void ThreeRgbLightsTravelAThirdOfALoopApart()
    {
        var light = LightSource.CreateDefault(1f, 2.5f);
        light.Advance(TimeSpan.FromSeconds(1.3));
        var lights = light.RgbLights();

        Assert.Equal([Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ], lights.Select(l => l.Color));
        for (int k = 0; k < 3; k++)
        {
            Vector2 expected = light.Evaluate(light.Phase + k / 3f);
            Near2(expected, new Vector2(lights[k].Position.X, lights[k].Position.Y));
            Near(2.5f, lights[k].Position.Z);
        }
        Near2(new Vector2(light.Position.X, light.Position.Y), new Vector2(lights[0].Position.X, lights[0].Position.Y));
    }
}
