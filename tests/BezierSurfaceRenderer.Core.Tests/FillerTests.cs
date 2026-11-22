using System.Numerics;

namespace BezierSurfaceRenderer.Core.Tests;

public class FillerTests
{
    private const int W = 64, H = 48;

    /// <summary>Span callback that counts how many times each pixel is written.</summary>
    private static Action<int, int, int> Counter(int[] counts) => (y, xFrom, xTo) =>
    {
        Assert.True(xFrom <= xTo);
        for (int x = xFrom; x <= xTo; x++) counts[y * W + x]++;
    };

    private static int[] Fill(Vector2[] polygon) => FillAll([polygon]);

    private static int[] FillAll(Vector2[][] polygons)
    {
        var counts = new int[W * H];
        var filler = new EdgeTableFiller();
        foreach (var poly in polygons)
            filler.Fill(poly, W, 0, H, Counter(counts));
        return counts;
    }

    /// <summary>Reference: pixel centre inside a CCW/CW triangle (half-open via top-left rule is not
    /// needed for strictly interior centres; tests only use configurations with no centre on an edge).</summary>
    private static bool Inside(Vector2[] t, Vector2 p)
    {
        static float E(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
        float e0 = E(t[0], t[1], p), e1 = E(t[1], t[2], p), e2 = E(t[2], t[0], p);
        return (e0 > 0 && e1 > 0 && e2 > 0) || (e0 < 0 && e1 < 0 && e2 < 0);
    }

    [Fact]
    public void AxisAlignedRectangleCoversExactPixels()
    {
        var counts = Fill([new(2, 3), new(10, 3), new(10, 8), new(2, 8)]);
        Assert.Equal(8 * 5, counts.Sum());
        Assert.Equal(1, counts[3 * W + 2]);
        Assert.Equal(1, counts[7 * W + 9]);
        Assert.Equal(0, counts[8 * W + 9]); // bottom edge exclusive
        Assert.Equal(0, counts[3 * W + 10]); // right edge exclusive
    }

    [Fact]
    public void TriangleMatchesPointInTriangleReference()
    {
        Vector2[] t = [new(3.3f, 2.1f), new(50.71f, 17.93f), new(12.2f, 41.6f)];
        var counts = Fill(t);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
            Assert.True((Inside(t, new Vector2(x + 0.5f, y + 0.5f)) ? 1 : 0) == counts[y * W + x], $"pixel ({x},{y}) count {counts[y * W + x]}");
    }

    [Fact]
    public void SharedEdgesHaveNoGapsAndNoOverdraw()
    {
        // A fan of triangles around a common vertex tiling a quad, with non-integer coordinates.
        Vector2 c = new(31.37f, 22.61f);
        Vector2[] ring = [new(4.2f, 3.7f), new(58.9f, 5.1f), new(60.3f, 44.4f), new(2.6f, 42.8f)];
        var tris = Enumerable.Range(0, 4).Select(k => new[] { c, ring[k], ring[(k + 1) % 4] }).ToArray();
        var counts = FillAll(tris);
        var whole = Fill(ring);

        Assert.All(counts, n => Assert.InRange(n, 0, 1));
        Assert.Equal(whole, counts);
    }

    [Fact]
    public void DegenerateTrianglesDrawNothing()
    {
        Assert.Equal(0, Fill([new(1, 1), new(20, 1), new(40, 1)]).Sum());     // horizontal line
        Assert.Equal(0, Fill([new(5, 1), new(5, 1), new(5, 30)]).Sum());      // vertical line
        Assert.Equal(0, Fill([new(5.1f, 5.1f), new(5.3f, 5.2f), new(5.2f, 5.4f)]).Sum()); // no centre inside
    }

    [Fact]
    public void ClipsToBufferAndRowWindow()
    {
        var counts = new int[W * H];
        new EdgeTableFiller().Fill([new(-100, -100), new(500, -100), new(500, 500), new(-100, 500)], W, 10, 20, Counter(counts));
        Assert.Equal(W * 10, counts.Sum());
        Assert.Equal(1, counts[10 * W]);
        Assert.Equal(0, counts[9 * W]);
        Assert.Equal(0, counts[20 * W]);
    }

    [Fact]
    public void ConcavePolygonFilledBetweenEdgePairs()
    {
        // "U" shape: the notch between x = 8..16 above y = 10 must stay empty.
        var counts = Fill([new(0, 0), new(8, 0), new(8, 10), new(16, 10), new(16, 0), new(24, 0), new(24, 20), new(0, 20)]);
        Assert.Equal(0, counts[5 * W + 12]);
        Assert.Equal(1, counts[5 * W + 4]);
        Assert.Equal(1, counts[15 * W + 12]);
        Assert.Equal(24 * 20 - 8 * 10, counts.Sum());
    }
}
