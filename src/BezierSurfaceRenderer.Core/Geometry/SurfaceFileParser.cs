namespace BezierSurfaceRenderer.Core.Geometry;

public sealed class SurfaceFileException(string message) : Exception(message);

/// <summary>
/// Reads groups of 16 lines "x y z" (invariant culture, any whitespace, blank lines ignored).
/// Requires ≥ 2 surfaces and a line count divisible by 16; errors name the offending line.
/// </summary>
public static class SurfaceFileParser
{
    public const int MinimumSurfaces = 2;

    public static IReadOnlyList<BezierSurface> Parse(string text) => throw new NotImplementedException();

    public static IReadOnlyList<BezierSurface> Load(string path) => Parse(File.ReadAllText(path));
}
