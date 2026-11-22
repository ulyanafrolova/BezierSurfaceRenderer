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
}
