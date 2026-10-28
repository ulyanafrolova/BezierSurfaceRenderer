using System.Numerics;
using BezierSurfaceRenderer.Core.Geometry;
using BezierSurfaceRenderer.Core.SceneModel;
using static BezierSurfaceRenderer.Core.Tests.TestUtil;

namespace BezierSurfaceRenderer.Core.Tests;

public class LightPathTests
{
    private static void Near2(Vector2 a, Vector2 b, float eps = 1e-4f) =>
        Assert.True(Vector2.Distance(a, b) <= eps, $"expected {a}, got {b}");

    [Fact]
    public void CurveIsClosed()
    {
        var c = ClosedBezierCurve.CreateDefault(1.2f);
        Near2(c.Evaluate(0f), c.Evaluate(0.99999f), 1e-3f);
        Near2(c.Evaluate(0.25f), c.Evaluate(1.25f));
    }

    [Fact]
    public void CurvePassesThroughJoints()
    {
        var c = ClosedBezierCurve.CreateDefault(1f);
        for (int k = 0; k < c.SegmentCount; k++)
            Near2(c.Points[3 * k], c.Evaluate((float)k / c.SegmentCount));
    }

    [Fact]
    public void EditingHandleKeepsC1AtJoint()
    {
        var c = ClosedBezierCurve.CreateDefault(1f);
        c.MovePoint(1, new Vector2(1.4f, 0.9f)); // outgoing handle of joint 0; opposite is the last point
        Vector2 joint = c.Points[0];
        Near2(joint - c.Points[^1], c.Points[1] - joint);

        c.MovePoint(11, new Vector2(0.3f, -0.8f)); // the wrapped incoming handle of joint 0
        Near2(joint - c.Points[11], c.Points[1] - joint);
    }

    [Fact]
    public void MovingJointCarriesHandles()
    {
        var c = ClosedBezierCurve.CreateDefault(1f);
        Vector2 before1 = c.Points[2], before2 = c.Points[4];
        var delta = new Vector2(0.1f, -0.2f);
        c.MovePoint(3, c.Points[3] + delta);
        Near2(before1 + delta, c.Points[2]);
        Near2(before2 + delta, c.Points[4]);
    }

    [Fact]
    public void HitTestFindsNearestWithinRadius()
    {
        var c = ClosedBezierCurve.CreateDefault(1f);
        Assert.Equal(0, c.HitTest(new Vector2(0.98f, 0.01f), 0.05f));
        Assert.Null(c.HitTest(new Vector2(0f, 0f), 0.05f));
    }

    [Fact]
    public void AnimatorAdvancesWrapsAndPauses()
    {
        var light = new LightAnimator(ClosedBezierCurve.CreateDefault(1f), 2.5f) { Speed = 0.5f };
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
