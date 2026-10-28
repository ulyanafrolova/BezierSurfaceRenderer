using System.Numerics;
using BezierSurfaceRenderer.Core.Geometry;
using BezierSurfaceRenderer.Core.Interaction;
using BezierSurfaceRenderer.Core.Mathematics;
using BezierSurfaceRenderer.Core.SceneModel;
using static BezierSurfaceRenderer.Core.Tests.TestUtil;

namespace BezierSurfaceRenderer.Core.Tests;

public class SceneTests
{
    private static IReadOnlyList<BezierSurface> Surfaces => SurfaceFileParser.Load(SurfacesFile);

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(64)]
    public void TriangulationProducesTwoTrianglesPerCellAndCoversDomain(int d)
    {
        var mesh = Triangulator.Triangulate(Surfaces[0], d);
        Assert.Equal(2 * d * d, mesh.Triangles.Count);
        Assert.Equal((d + 1) * (d + 1), mesh.Vertices.Count);
        Assert.Contains(mesh.Vertices, v => v.U == 0 && v.V == 0);
        Assert.Contains(mesh.Vertices, v => v.U == 1 && v.V == 1);
    }

    [Fact]
    public void RotationAppliesToPointsTangentsAndNormal()
    {
        var scene = new Scene(Surfaces, 4);
        var r = Mat3.RotationY(0.8f) * Mat3.RotationX(-0.4f);
        scene.Rotate(r);

        foreach (var v in scene.Meshes[1].Vertices)
        {
            Near(r * v.Original.P, v.Rotated.P);
            Near(r * v.Original.Pu, v.Rotated.Pu);
            Near(r * v.Original.Pv, v.Rotated.Pv);
            Near(1f, v.Rotated.N.Length());
        }
    }

    [Fact]
    public void RotationIsIncrementalComposition()
    {
        var scene = new Scene(Surfaces, 2);
        var a = Mat3.RotationX(0.5f);
        var b = Mat3.RotationZ(1.1f);
        scene.Rotate(a);
        scene.Rotate(b);
        var expected = b * a; // R_new = R_inc · R_current
        Near(expected.Column1, scene.Orientation.Column1);
        Near(expected.Column2, scene.Orientation.Column2);
        Near(expected.Column3, scene.Orientation.Column3);
    }

    [Fact]
    public void ChangingDensityKeepsOrientation()
    {
        var scene = new Scene(Surfaces, 3);
        scene.Rotate(Mat3.RotationX(1f));
        scene.SetDivisions(10);
        var v = scene.Meshes[0].Vertices[5];
        Near(scene.Orientation * v.Original.P, v.Rotated.P);
    }

    [Fact]
    public void UnboundedRotation_StaysOrthonormal()
    {
        var scene = new Scene(Surfaces, 1);
        var arcball = new ArcballController { Radius = 300 };
        var rng = new Random(1);
        var p = new Vector2(0, 0);
        for (int k = 0; k < 10000; k++)
        {
            var q = p + new Vector2(rng.Next(-15, 16), rng.Next(-15, 16));
            scene.Rotate(arcball.TrackballIncrement(p, q));
            p = Vector2.Clamp(q, new(-400), new(400));
        }
        Mat3Tests.AssertOrthonormal(scene.Orientation, 1e-5f);
    }

    [Fact]
    public void Arcball_PointUnderCursorFollowsCursor()
    {
        var arcball = new ArcballController { Radius = 100 };
        Vector2 p0 = new(10, -20), p1 = new(35, 5);
        var r = arcball.TrackballIncrement(p0, p1);
        Near(arcball.ProjectToSphere(p1), r * arcball.ProjectToSphere(p0));
    }

    [Fact]
    public void Arcball_DragRightRotatesFrontTowardsPlusX()
    {
        var arcball = new ArcballController { Radius = 100 };
        var front = arcball.TrackballIncrement(Vector2.Zero, new Vector2(20, 0)) * Vector3.UnitZ;
        Assert.True(front.X > 0);
        Near(0f, front.Y);
    }

    [Fact]
    public void Arcball_ZeroMovementIsIdentity()
    {
        var r = new ArcballController { Radius = 100 }.TrackballIncrement(new(5, 5), new(5, 5));
        Near(Vector3.UnitX, r * Vector3.UnitX);
    }

    [Fact]
    public void Roll_CounterClockwiseIsPositiveZRotation()
    {
        var r = new ArcballController().RollIncrement(new Vector2(100, 0), new Vector2(0, 100));
        Near(Vector3.UnitY, r * Vector3.UnitX);
    }

    [Fact]
    public void BoundingRadiusCoversAllControlPoints()
    {
        var scene = new Scene(Surfaces, 1);
        Assert.InRange(scene.BoundingRadius, 1.6f, MathF.Sqrt(3) + 1e-3f);
    }
}
