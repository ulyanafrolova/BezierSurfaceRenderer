using System.Numerics;

namespace BezierSurfaceRenderer.Core.Tests;

internal static class TestUtil
{
    public static void Near(float expected, float actual, float eps = 1e-4f) =>
        Assert.True(MathF.Abs(expected - actual) <= eps, $"expected {expected}, got {actual}");

    public static void Near(Vector3 expected, Vector3 actual, float eps = 1e-4f) =>
        Assert.True(Vector3.Distance(expected, actual) <= eps, $"expected {expected}, got {actual}");

    /// <summary>1×1 texture of one colour.</summary>
    public static Texture SolidTexture(Vector3 rgb01) => new(1, 1, [Lighting.ToArgb(rgb01)]);

    public static string RepoFile(params string[] relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BezierSurfaceRenderer.sln")))
            dir = dir.Parent;
        return Path.Combine([dir!.FullName, .. relative]);
    }

    public static string SurfacesFile => RepoFile("src", "BezierSurfaceRenderer.App", "Assets", "surfaces.txt");
}
