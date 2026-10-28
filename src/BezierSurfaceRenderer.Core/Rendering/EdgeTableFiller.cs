using System.Numerics;

namespace BezierSurfaceRenderer.Core.Rendering;

/// <summary>Receives one horizontal run of pixels [xFrom, xTo] (inclusive) on scanline y.</summary>
public interface ISpanSink
{
    void Span(int y, int xFrom, int xTo);
}

/// <summary>
/// Polygon scan conversion with bucket-sorted edges (spec §4, surnames A–K):
///   1. ET: array of buckets indexed by the edge's first scanline; each bucket is a linked list of
///      edges (last scanline, x at the first scanline, 1/m = dx/dy). Horizontal edges never enter the ET.
///   2. For each scanline y: move bucket[y] into the AET, drop edges whose last scanline has passed,
///      sort the AET by x, fill between pairs, then x += 1/m for every active edge.
/// Sampling is at pixel centres (x + 0.5, y + 0.5) with half-open intervals [start, end) both vertically
/// and horizontally — the classic top-left rule — so triangles sharing an edge leave no gaps and never
/// draw a pixel twice. Output is clipped to columns [0, width) and rows [rowFrom, rowTo).
/// Purely geometric: the caller shades each span. Instances hold scratch buffers, so use one per thread.
/// </summary>
public sealed class EdgeTableFiller
{
    private struct Edge
    {
        public int YEnd;     // first scanline NOT covered (exclusive)
        public float X;      // x at the current scanline centre
        public float DxDy;   // 1/m
        public int Next;     // next edge in the same bucket (−1 = end)
    }

    private Edge[] _edges = new Edge[8];
    private int[] _buckets = new int[64];
    private int[] _active = new int[8];

    public void Fill<TSink>(ReadOnlySpan<Vector2> polygon, int width, int rowFrom, int rowTo, ref TSink sink)
        where TSink : struct, ISpanSink
    {
        int n = polygon.Length;
        if (n < 3 || rowFrom >= rowTo) return;

        // Range of scanlines the polygon can cover, clipped to the requested rows.
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var p in polygon) { minY = MathF.Min(minY, p.Y); maxY = MathF.Max(maxY, p.Y); }
        int yFirst = Math.Max(rowFrom, FirstCentre(minY));
        int yLast = Math.Min(rowTo, FirstCentre(maxY)); // exclusive
        if (yFirst >= yLast) return;

        int bucketCount = yLast - yFirst;
        if (_buckets.Length < bucketCount) _buckets = new int[bucketCount * 2];
        _buckets.AsSpan(0, bucketCount).Fill(-1);
        if (_edges.Length < n) { _edges = new Edge[n * 2]; _active = new int[n * 2]; }

        // ---- Build the bucket-sorted edge table -------------------------------------------------
        int edgeCount = 0;
        for (int k = 0; k < n; k++)
        {
            Vector2 a = polygon[k], b = polygon[(k + 1) % n];
            if (a.Y > b.Y) (a, b) = (b, a);

            int yStart = FirstCentre(a.Y), yEnd = FirstCentre(b.Y);
            if (yStart >= yEnd) continue;                      // horizontal or covers no pixel centre
            yEnd = Math.Min(yEnd, yLast);
            if (yEnd <= yFirst) continue;                      // entirely above the clip window

            float dxdy = (b.X - a.X) / (b.Y - a.Y);
            int y0 = Math.Max(yStart, yFirst);
            if (y0 >= yEnd) continue;

            ref Edge e = ref _edges[edgeCount];
            e.YEnd = yEnd;
            e.DxDy = dxdy;
            e.X = a.X + (y0 + 0.5f - a.Y) * dxdy;
            int bucket = y0 - yFirst;
            e.Next = _buckets[bucket];
            _buckets[bucket] = edgeCount;
            edgeCount++;
        }
        if (edgeCount == 0) return;

        // ---- Scan with the active edge table ----------------------------------------------------
        int activeCount = 0;
        for (int y = yFirst; y < yLast; y++)
        {
            for (int e = _buckets[y - yFirst]; e >= 0; e = _edges[e].Next)
                _active[activeCount++] = e;

            // Remove edges that ended (their last covered scanline was y − 1).
            int kept = 0;
            for (int k = 0; k < activeCount; k++)
                if (_edges[_active[k]].YEnd > y) _active[kept++] = _active[k];
            activeCount = kept;

            // Insertion sort by x — the AET is tiny (2 edges for a triangle) and nearly sorted.
            for (int k = 1; k < activeCount; k++)
            {
                int cur = _active[k];
                float x = _edges[cur].X;
                int m = k - 1;
                while (m >= 0 && _edges[_active[m]].X > x) { _active[m + 1] = _active[m]; m--; }
                _active[m + 1] = cur;
            }

            for (int k = 0; k + 1 < activeCount; k += 2)
            {
                int xFrom = Math.Max(0, FirstCentre(_edges[_active[k]].X));
                int xTo = Math.Min(width, FirstCentre(_edges[_active[k + 1]].X)) - 1;
                if (xFrom <= xTo) sink.Span(y, xFrom, xTo);
            }

            for (int k = 0; k < activeCount; k++)
                _edges[_active[k]].X += _edges[_active[k]].DxDy;
        }
    }

    /// <summary>Index of the first pixel whose centre (i + 0.5) is ≥ coordinate c.</summary>
    private static int FirstCentre(float c) => (int)MathF.Ceiling(c - 0.5f);
}
