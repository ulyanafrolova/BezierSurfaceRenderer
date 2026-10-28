using System.Numerics;
using BezierSurfaceRenderer.Core.Geometry;
using BezierSurfaceRenderer.Core.Mathematics;
using BezierSurfaceRenderer.Core.Rendering;
using BezierSurfaceRenderer.Core.SceneModel;
using static BezierSurfaceRenderer.Core.Tests.TestUtil;

namespace BezierSurfaceRenderer.Core.Tests;

public class ShadingTests
{
    [Fact]
    public void LightStraightAboveGivesKdPlusKs()
    {
        // N = L = V = z ⇒ cos(N,L) = 1, R = z ⇒ cos(V,R) = 1.
        var p = new LightingParameters(0.6f, 0.3f, 50, Vector3.One);
        var color = Lighting.Shade(Vector3.UnitZ, Vector3.Zero, new Vector3(0, 0, 10), new Vector3(0.5f, 1f, 0.2f), p);
        Near(new Vector3(0.45f, 0.9f, 0.18f), color);
    }

    [Fact]
    public void ChannelsAreIndependent()
    {
        var p = new LightingParameters(1f, 0f, 1, new Vector3(1f, 0.5f, 0f));
        var color = Lighting.Shade(Vector3.UnitZ, Vector3.Zero, Vector3.UnitZ, new Vector3(0.8f, 0.8f, 0.8f), p);
        Near(new Vector3(0.8f, 0.4f, 0f), color);
    }

    [Fact]
    public void Light45DegreesMatchesHandComputation()
    {
        // L = (1,0,1)/√2: cosNL = √2/2; R = (−1,0,1)/√2 ⇒ cosVR = √2/2.
        var p = new LightingParameters(0.5f, 0.5f, 2, Vector3.One);
        var color = Lighting.Shade(Vector3.UnitZ, Vector3.Zero, new Vector3(5, 0, 5), Vector3.One, p);
        float expected = 0.5f * MathF.Sqrt(0.5f) + 0.5f * 0.5f;
        Near(new Vector3(expected), color);
    }

    [Fact]
    public void NegativeCosinesClampToZero()
    {
        var p = new LightingParameters(1f, 1f, 10, Vector3.One);
        Near(Vector3.Zero, Lighting.Shade(Vector3.UnitZ, Vector3.Zero, new Vector3(0, 0, -3), Vector3.One, p));
    }

    [Fact]
    public void ToArgbScalesAtTheEndAndClampsTo255()
    {
        Assert.Equal(unchecked((int)0xFFFF8000), Lighting.ToArgb(new Vector3(1.7f, 0.502f, -0.2f)));
    }

    [Fact]
    public void NormalMapDecodeAndFlatMapIsIdentity()
    {
        Near(Vector3.UnitZ, NormalMapping.Decode(new Vector3(127, 127, 255) / 255f), 1e-2f);

        Vector3 pu = new(2, 0, 0.3f), pv = new(0.1f, 3, -0.2f);
        Vector3 n = Vector3.Normalize(Vector3.Cross(pu, pv));
        Near(n, NormalMapping.Perturb(pu, pv, n, Vector3.UnitZ));
    }

    [Fact]
    public void NormalMapUsesMatrixWithColumnsPuPvN()
    {
        Vector3 pu = Vector3.UnitX * 4, pv = Vector3.UnitY * 2, n = Vector3.UnitZ;
        var nTex = Vector3.Normalize(new Vector3(1, 0, 1));
        Near(nTex, NormalMapping.Perturb(pu, pv, n, nTex)); // M = I after normalising Pu, Pv
        Near(n, NormalMapping.Perturb(Vector3.Zero, pv, n, nTex)); // degenerate tangent → fallback
    }

    [Fact]
    public void TextureSampledByUV()
    {
        // 2×2: top-left red, top-right green, bottom-left blue, bottom-right white.
        var tex = new Texture(2, 2, [unchecked((int)0xFFFF0000), unchecked((int)0xFF00FF00), unchecked((int)0xFF0000FF), -1]);
        Near(Vector3.UnitX, tex.Sample(0.1f, 0.1f));
        Near(Vector3.UnitY, tex.Sample(0.9f, 0.1f));
        Near(Vector3.UnitZ, tex.Sample(0.1f, 0.9f));
        Near(Vector3.One, tex.Sample(1f, 1f));
    }

    [Fact]
    public void ZBufferKeepsNearestFragment()
    {
        var fb = new FrameBuffer(4, 4);
        fb.Clear(0);
        Assert.True(fb.TestAndSetDepth(1, 1, -0.5f));
        Assert.True(fb.TestAndSetDepth(1, 1, 0.3f));   // closer (larger z) wins
        Assert.False(fb.TestAndSetDepth(1, 1, 0.1f));  // farther is rejected
        Assert.Equal(0.3f, fb.Depth[1 * 4 + 1]);
    }

    [Fact]
    public void FrameBufferOriginAtCentreWithYUp()
    {
        var fb = new FrameBuffer(200, 100) { PixelsPerUnit = 10 };
        Assert.Equal(100f, fb.ToColumn(0));
        Assert.Equal(50f, fb.ToRow(0));
        Assert.Equal(40f, fb.ToRow(1)); // y up ⇒ smaller row
        Assert.Equal(110f, fb.ToColumn(1));
    }

    private static RenderSettings Settings(Vector3 light) =>
        new(LightingParameters.Default, light, ObjectColorSource.SolidColor, Vector3.One, null, false, null);

    [Fact]
    public void RendererFillsSceneAndParallelMatchesSequential()
    {
        var scene = new Scene(SurfaceFileParser.Load(SurfacesFile), 12);
        scene.Rotate(Mat3.RotationX(0.7f) * Mat3.RotationZ(0.3f));
        var settings = Settings(new Vector3(0.5f, 0.5f, 3f));

        var seq = new FrameBuffer(160, 120) { PixelsPerUnit = 30 };
        var par = new FrameBuffer(160, 120) { PixelsPerUnit = 30 };
        seq.Clear(0);
        par.Clear(0);
        new SceneRenderer { Parallel = false }.RenderFilled(seq, scene.Meshes, settings);
        new SceneRenderer { Parallel = true }.RenderFilled(par, scene.Meshes, settings);

        Assert.Equal(seq.Color, par.Color);
        Assert.True(seq.Color.Count(c => c != 0) > 160 * 120 / 10);
    }

    [Fact]
    public void ZBufferShowsNearerSurfaceWhereTheyOverlap()
    {
        var surfaces = SurfaceFileParser.Load(SurfacesFile);
        var scene = new Scene(surfaces, 16);
        var fb = new FrameBuffer(101, 101) { PixelsPerUnit = 40 };
        fb.Clear(0);
        new SceneRenderer().RenderFilled(fb, scene.Meshes, Settings(new Vector3(0, 0, 5)));

        // At the centre the dome (z ≈ 0.56) is above the saddle (z ≈ 0): depth holds the dome's z.
        float domeZ = surfaces[0].Evaluate(0.5f, 0.5f).P.Z;
        float saddleZ = surfaces[1].Evaluate(0.5f, 0.5f).P.Z;
        Assert.True(domeZ > saddleZ);
        Near(domeZ, fb.Depth[50 * 101 + 50], 0.02f);
    }

    [Fact]
    public void PerPixelShadingProducesHighlightInsideALargeTriangle()
    {
        // Coarsest mesh (2 triangles per surface): Gouraud would put the brightest pixel at a vertex;
        // per-pixel interpolation of N puts the specular peak in the interior under the light.
        var scene = new Scene(SurfaceFileParser.Load(SurfacesFile).Take(1).ToList(), 1);
        var fb = new FrameBuffer(101, 101) { PixelsPerUnit = 40 };
        fb.Clear(0);
        var settings = new RenderSettings(new LightingParameters(0.2f, 0.8f, 30, Vector3.One),
            new Vector3(0, 0, 4), ObjectColorSource.SolidColor, Vector3.One, null, false, null);
        new SceneRenderer().RenderFilled(fb, scene.Meshes, settings);

        int best = Array.IndexOf(fb.Color, fb.Color.MaxBy(c => c & 0xFF));
        int row = best / 101, col = best % 101;
        Assert.InRange(row, 20, 80);
        Assert.InRange(col, 20, 80);
    }
}
