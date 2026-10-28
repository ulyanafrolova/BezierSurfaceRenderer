using System.Numerics;
using BezierSurfaceRenderer.Core.Geometry;
using static BezierSurfaceRenderer.Core.Tests.TestUtil;

namespace BezierSurfaceRenderer.Core.Tests;

public class BezierSurfaceTests
{
    private static BezierSurface Dome => SurfaceFileParser.Load(SurfacesFile)[0];

    [Theory]
    [InlineData(0.37f)]
    [InlineData(0f)]
    [InlineData(1f)]
    public void BernsteinIsPartitionOfUnity(float t)
    {
        for (int n = 0; n <= 3; n++)
        {
            float sum = 0;
            for (int i = 0; i <= n; i++) sum += Bernstein.B(i, n, t);
            Near(1f, sum);
        }
    }

    [Fact]
    public void CornersInterpolateCornerControlPoints()
    {
        var s = Dome;
        Near(s.ControlPoints[0, 0], s.Evaluate(0, 0).P);
        Near(s.ControlPoints[3, 0], s.Evaluate(1, 0).P);
        Near(s.ControlPoints[0, 3], s.Evaluate(0, 1).P);
        Near(s.ControlPoints[3, 3], s.Evaluate(1, 1).P);
    }

    [Theory]
    [InlineData(0.2f, 0.7f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(0.9f, 0.05f)]
    public void TangentsMatchFiniteDifferences(float u, float v)
    {
        var s = Dome;
        const float h = 1e-3f;
        Vector3 fdU = (s.Evaluate(u + h, v).P - s.Evaluate(u - h, v).P) / (2 * h);
        Vector3 fdV = (s.Evaluate(u, v + h).P - s.Evaluate(u, v - h).P) / (2 * h);
        var sample = s.Evaluate(u, v);
        Near(fdU, sample.Pu, 2e-2f);
        Near(fdV, sample.Pv, 2e-2f);
    }

    [Fact]
    public void NormalIsUnitCrossProductOfTangents()
    {
        var sample = Dome.Evaluate(0.3f, 0.6f);
        Near(1f, sample.N.Length());
        Near(Vector3.Normalize(Vector3.Cross(sample.Pu, sample.Pv)), sample.N);
    }

    [Fact]
    public void TopOfDomeFacesTheViewer()
    {
        // i ↔ x, j ↔ y ⇒ Pu ≈ +x, Pv ≈ +y ⇒ N = Pu × Pv points to +z (towards V = [0,0,1]).
        Near(Vector3.UnitZ, Dome.Evaluate(0.5f, 0.5f).N, 1e-3f);
    }
}
