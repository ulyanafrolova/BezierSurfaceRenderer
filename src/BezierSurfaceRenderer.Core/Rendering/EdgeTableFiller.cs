using System.Numerics;

namespace BezierSurfaceRenderer.Core.Rendering;

/// <summary>Receives one horizontal run of pixels [xFrom, xTo] (inclusive) on scanline y.</summary>
public delegate void SpanCallback(int y, int xFrom, int xTo);

/// <summary>
/// Polygon scan conversion with bucket-sorted edges (spec §4, surnames A–K):
///   1. ET: array of buckets indexed by the edge's ymin; each bucket holds (ymax, x at ymin, 1/m).
///      Horizontal edges are skipped.
///   2. For y from the lowest non-empty bucket: move bucket[y] into AET, drop edges with ymax == y,
///      sort AET by x, fill between pairs, then x += 1/m for every active edge.
/// Pixel centres are sampled at (x + 0.5, y + 0.5) with a top-left style rule so triangles that share
/// an edge neither leave gaps nor draw the same pixel twice. Output is clipped to [0,W)×[0,H).
/// Purely geometric — shading is done by the caller per span.
/// </summary>
public sealed class EdgeTableFiller
{
    public void Fill(ReadOnlySpan<Vector2> polygonInScreenSpace, int width, int height, SpanCallback onSpan)
        => throw new NotImplementedException();
}
