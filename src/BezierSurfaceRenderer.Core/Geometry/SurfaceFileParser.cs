using System.Globalization;
using System.Numerics;

namespace BezierSurfaceRenderer.Core.Geometry;

public sealed class SurfaceFileException(string message) : Exception(message);

/// <summary>
/// Reads groups of 16 lines "x y z" (invariant culture, any whitespace, blank lines ignored).
/// Requires ≥ 2 surfaces and a line count divisible by 16; errors name the offending line.
/// </summary>
public static class SurfaceFileParser
{
    public const int MinimumSurfaces = 2;

    private static readonly char[] Separators = [' ', '\t'];

    public static IReadOnlyList<BezierSurface> Parse(string text)
    {
        var points = new List<Vector3>();
        string[] lines = text.Split('\n');
        for (int lineNo = 1; lineNo <= lines.Length; lineNo++)
        {
            string line = lines[lineNo - 1].Trim();
            if (line.Length == 0) continue;

            string[] parts = line.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                throw new SurfaceFileException($"Line {lineNo}: expected 3 numbers \"x y z\", found {parts.Length} value(s).");

            var coords = new float[3];
            for (int k = 0; k < 3; k++)
            {
                if (!float.TryParse(parts[k], NumberStyles.Float, CultureInfo.InvariantCulture, out coords[k])
                    || !float.IsFinite(coords[k]))
                    throw new SurfaceFileException($"Line {lineNo}: \"{parts[k]}\" is not a valid real number (use '.' as decimal separator).");
            }
            points.Add(new Vector3(coords[0], coords[1], coords[2]));
        }

        if (points.Count % BezierSurface.PointsPerSurface != 0)
            throw new SurfaceFileException(
                $"Found {points.Count} control points; each surface needs exactly {BezierSurface.PointsPerSurface} lines.");

        int surfaceCount = points.Count / BezierSurface.PointsPerSurface;
        if (surfaceCount < MinimumSurfaces)
            throw new SurfaceFileException($"Found {surfaceCount} surface(s); at least {MinimumSurfaces} are required.");

        var surfaces = new List<BezierSurface>(surfaceCount);
        for (int s = 0; s < surfaceCount; s++)
        {
            var net = new Vector3[4, 4];
            for (int k = 0; k < BezierSurface.PointsPerSurface; k++)
                net[k / 4, k % 4] = points[s * BezierSurface.PointsPerSurface + k];
            surfaces.Add(new BezierSurface(net));
        }
        return surfaces;
    }

    public static IReadOnlyList<BezierSurface> Load(string path) => Parse(File.ReadAllText(path));
}
