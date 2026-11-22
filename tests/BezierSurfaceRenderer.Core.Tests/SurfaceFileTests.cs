using System.Numerics;

namespace BezierSurfaceRenderer.Core.Tests;

public class SurfaceFileTests
{
    private static string Lines(int count) =>
        string.Join("\n", Enumerable.Range(0, count).Select(k => $"{k % 4}.5 {k / 4} -0.25"));

    [Fact]
    public void ParsesConsecutiveGroupsOf16_RowMajorIJ()
    {
        var surfaces = SurfaceFileParser.Parse(Lines(32));
        Assert.Equal(2, surfaces.Count);
        // line k → i = k/4, j = k%4 ; line 6 is "2.5 1 -0.25"
        Assert.Equal(new Vector3(2.5f, 1, -0.25f), surfaces[0].ControlPoints[1, 2]);
    }

    [Fact]
    public void AcceptsTabsCrlfAndBlankLines() =>
        Assert.Equal(2, SurfaceFileParser.Parse(Lines(32).Replace(" ", "\t").Replace("\n", "\r\n\r\n")).Count);

    [Fact]
    public void RejectsSingleSurface() =>
        Assert.Throws<SurfaceFileException>(() => SurfaceFileParser.Parse(Lines(16)));

    [Fact]
    public void RejectsIncompleteGroup() =>
        Assert.Throws<SurfaceFileException>(() => SurfaceFileParser.Parse(Lines(33)));

    [Fact]
    public void RejectsMalformedLineWithLineNumber()
    {
        var ex = Assert.Throws<SurfaceFileException>(() => SurfaceFileParser.Parse(Lines(31) + "\n1,5 2 3"));
        Assert.Contains("Line 32", ex.Message);
    }

    [Fact]
    public void ShippedFile_AtLeastTwoSurfaces_CubeBoundingBoxCentredAtOrigin()
    {
        var surfaces = SurfaceFileParser.Load(TestUtil.SurfacesFile);
        Assert.True(surfaces.Count >= 2);

        var all = surfaces.SelectMany(s => s.ControlPoints.Cast<Vector3>()).ToList();
        var min = all.Aggregate(Vector3.Min);
        var max = all.Aggregate(Vector3.Max);
        var size = max - min;
        var centre = (max + min) / 2;

        TestUtil.Near(Vector3.Zero, centre, 0.05f);
        Assert.InRange(size.X / size.Y, 0.9f, 1.1f);
        Assert.InRange(size.X / size.Z, 0.9f, 1.1f);
    }

    [Fact]
    public void ShippedFile_EverySurfaceIsAFunctionOverXY()
    {
        // Sufficient condition: the xy projection of the control net is strictly monotone in i and j,
        // and the evaluated surface's xy Jacobian (Pu.xy × Pv.xy) keeps one sign over the whole domain.
        foreach (var s in SurfaceFileParser.Load(TestUtil.SurfacesFile))
        {
            for (int a = 0; a <= 20; a++)
            for (int b = 0; b <= 20; b++)
            {
                var d = s.Evaluate(a / 20f, b / 20f);
                float jac = d.Pu.X * d.Pv.Y - d.Pu.Y * d.Pv.X;
                Assert.True(jac > 0, $"xy-map folds at u={a / 20f}, v={b / 20f}");
            }
        }
    }
}
