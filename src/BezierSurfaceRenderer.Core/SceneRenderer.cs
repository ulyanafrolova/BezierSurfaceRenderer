using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>
/// Filled-triangle pass. For every rotated triangle independently:
///   project (x', y') → screen, scan-convert with <see cref="EdgeTableFiller"/>, and for each pixel:
///   barycentric (λa, λb, λc) in screen space (exact under orthographic projection) →
///   interpolate z, N, Pu, Pv, u, v → depth test → [normal map] → IO (surface colour / texture) → Lighting.Shade.
/// The image is split into horizontal bands rendered in parallel; each band owns its rows of the
/// colour and depth buffers, so no locking is needed.
/// </summary>
public static class SceneRenderer
{
    private const int RowsPerBand = 32;

    private sealed record Projected(Triangle T, BezierSurface Surface, Vector2 A, Vector2 B, Vector2 C, float InvArea)
    {
        public float MinY => MathF.Min(A.Y, MathF.Min(B.Y, C.Y));
        public float MaxY => MathF.Max(A.Y, MathF.Max(B.Y, C.Y));
    }

    public static void RenderFilled(FrameBuffer target, IReadOnlyList<Mesh> meshes, RenderSettings settings)
    {
        var triangles = new List<Projected>();
        foreach (var mesh in meshes)
        foreach (var t in mesh.Triangles)
        {
            Vector2 a = Project(target, t.A), b = Project(target, t.B), c = Project(target, t.C);
            float area = Cross(b - a, c - a);
            if (MathF.Abs(area) < 1e-6f) continue; // edge-on: covers no area
            triangles.Add(new Projected(t, mesh.Surface, a, b, c, 1f / area));
        }

        int bandCount = (target.Height + RowsPerBand - 1) / RowsPerBand;
        Parallel.For(0, bandCount, band =>
        {
            int rowFrom = band * RowsPerBand, rowTo = Math.Min(target.Height, rowFrom + RowsPerBand);
            var filler = new EdgeTableFiller();
            Projected current = null!;
            Action<int, int, int> shadeSpan = (y, xFrom, xTo) => ShadeSpan(target, current, settings, y, xFrom, xTo);

            foreach (var t in triangles)
            {
                if (t.MaxY < rowFrom || t.MinY >= rowTo) continue;
                current = t;
                filler.Fill([t.A, t.B, t.C], target.Width, rowFrom, rowTo, shadeSpan);
            }
        });
    }

    /// <summary>Shades pixels [xFrom, xTo] of row y: barycentric interpolation, depth test, lighting.</summary>
    private static void ShadeSpan(FrameBuffer fb, Projected t, RenderSettings s, int y, int xFrom, int xTo)
    {
        SurfaceSample a = t.T.A.Rotated, b = t.T.B.Rotated, c = t.T.C.Rotated;
        for (int x = xFrom; x <= xTo; x++)
        {
            // Barycentric coordinates of the pixel centre (λa + λb + λc = 1).
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float la = Cross(t.B - p, t.C - p) * t.InvArea;
            float lb = Cross(t.C - p, t.A - p) * t.InvArea;
            float lc = 1f - la - lb;

            Vector3 point = a.P * la + b.P * lb + c.P * lc;
            if (!fb.TestAndSetDepth(x, y, point.Z)) continue;

            float u = t.T.A.U * la + t.T.B.U * lb + t.T.C.U * lc;
            float v = t.T.A.V * la + t.T.B.V * lb + t.T.C.V * lc;

            Vector3 n = Vector3.Normalize(a.N * la + b.N * lb + c.N * lc);

            if (s.NormalMap != null)
            {
                Vector3 pu = a.Pu * la + b.Pu * lb + c.Pu * lc;
                Vector3 pv = a.Pv * la + b.Pv * lb + c.Pv * lc;
                n = NormalMapping.Perturb(pu, pv, n, NormalMapping.Decode(s.NormalMap.Sample(u, v)));
            }
            // Two-sided lighting: the visible side faces the observer V = [0,0,1]. Flipped only after the
            // normal map, so the relief is not inverted on the back side.
            if (n.Z < 0) n = -n;

            fb.Color[y * fb.Width + x] = Lighting.ToArgb(Lighting.Shade(n, point, t.Surface.ObjectColor(u, v), s));
        }
    }

    private static Vector2 Project(FrameBuffer fb, Vertex v) => new(fb.ToColumn(v.Rotated.P.X), fb.ToRow(v.Rotated.P.Y));

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
