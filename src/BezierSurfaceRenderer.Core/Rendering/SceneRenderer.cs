using System.Numerics;
using BezierSurfaceRenderer.Core.Geometry;

namespace BezierSurfaceRenderer.Core.Rendering;

public enum ObjectColorSource { SolidColor, Texture }

/// <summary>Everything the filled-triangle pass needs for one frame. Positions are in model units.</summary>
public sealed record RenderSettings(
    LightingParameters Lighting,
    Vector3 LightPosition,
    ObjectColorSource ColorSource,
    Vector3 SolidColor,
    Texture? Texture,
    bool UseNormalMap,
    Texture? NormalMap);

/// <summary>
/// Filled-triangle pass. For every rotated triangle independently:
///   project (x', y') → screen, scan-convert with <see cref="EdgeTableFiller"/>, and for each pixel:
///   barycentric (λ1, λ2, λ3) in screen space (exact under orthographic projection) →
///   interpolate z, N, Pu, Pv, u, v → depth test → [normal map] → IO (solid / texture(u,v)) → Lighting.Shade.
/// Rendering is split into horizontal bands processed in parallel; each band owns its rows of
/// the colour and depth buffers, so no locking is needed.
/// </summary>
public sealed class SceneRenderer
{
    private const int RowsPerBand = 32;

    /// <summary>Per-frame, read-only data of one projected triangle.</summary>
    private sealed class Projected(Vertex a, Vertex b, Vertex c, Vector2 sa, Vector2 sb, Vector2 sc)
    {
        public readonly Vertex A = a, B = b, C = c;
        public readonly Vector2 SA = sa, SB = sb, SC = sc;
        public readonly int RowMin = (int)MathF.Floor(MathF.Min(sa.Y, MathF.Min(sb.Y, sc.Y)));
        public readonly int RowMax = (int)MathF.Ceiling(MathF.Max(sa.Y, MathF.Max(sb.Y, sc.Y)));
    }

    public bool Parallel { get; set; } = true;

    public void RenderFilled(FrameBuffer target, IReadOnlyList<Mesh> meshes, RenderSettings settings)
    {
        var triangles = new List<Projected>();
        foreach (var mesh in meshes)
        foreach (var t in mesh.Triangles)
        {
            Vector2 sa = Project(target, t.A), sb = Project(target, t.B), sc = Project(target, t.C);
            if (MathF.Abs(Cross(sb - sa, sc - sa)) < 1e-6f) continue; // edge-on: covers no area
            triangles.Add(new Projected(t.A, t.B, t.C, sa, sb, sc));
        }

        int bandCount = (target.Height + RowsPerBand - 1) / RowsPerBand;
        if (Parallel)
            System.Threading.Tasks.Parallel.For(0, bandCount, () => new EdgeTableFiller(),
                (band, _, filler) => { RenderBand(band, filler); return filler; }, _ => { });
        else
        {
            var filler = new EdgeTableFiller();
            for (int band = 0; band < bandCount; band++) RenderBand(band, filler);
        }

        void RenderBand(int band, EdgeTableFiller filler)
        {
            int rowFrom = band * RowsPerBand, rowTo = Math.Min(target.Height, rowFrom + RowsPerBand);
            Span<Vector2> polygon = stackalloc Vector2[3];
            foreach (var t in triangles)
            {
                if (t.RowMax < rowFrom || t.RowMin >= rowTo) continue;
                polygon[0] = t.SA; polygon[1] = t.SB; polygon[2] = t.SC;
                var shader = new PixelShader(target, t, settings);
                filler.Fill(polygon, target.Width, rowFrom, rowTo, ref shader);
            }
        }
    }

    private static Vector2 Project(FrameBuffer fb, Vertex v) =>
        new(fb.ToColumn(v.Rotated.P.X), fb.ToRow(v.Rotated.P.Y));

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>Shades every pixel of a span: barycentric interpolation, depth test, lighting.</summary>
    private struct PixelShader : ISpanSink
    {
        private readonly FrameBuffer _fb;
        private readonly Projected _t;
        private readonly RenderSettings _s;
        private readonly float _invArea;

        public PixelShader(FrameBuffer fb, Projected t, RenderSettings s)
        {
            _fb = fb;
            _t = t;
            _s = s;
            _invArea = 1f / Cross(t.SB - t.SA, t.SC - t.SA);
        }

        public void Span(int y, int xFrom, int xTo)
        {
            var t = _t;
            SurfaceSample a = t.A.Rotated, b = t.B.Rotated, c = t.C.Rotated;
            int rowOffset = y * _fb.Width;

            for (int x = xFrom; x <= xTo; x++)
            {
                // Barycentric coordinates of the pixel centre (λa + λb + λc = 1).
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float la = Cross(t.SB - p, t.SC - p) * _invArea;
                float lb = Cross(t.SC - p, t.SA - p) * _invArea;
                float lc = 1f - la - lb;

                Vector3 point = a.P * la + b.P * lb + c.P * lc;
                if (!_fb.TestAndSetDepth(x, y, point.Z)) continue;

                float u = t.A.U * la + t.B.U * lb + t.C.U * lc;
                float v = t.A.V * la + t.B.V * lb + t.C.V * lc;

                Vector3 n = Vector3.Normalize(a.N * la + b.N * lb + c.N * lc);
                if (_s.UseNormalMap && _s.NormalMap is { } map)
                {
                    Vector3 pu = a.Pu * la + b.Pu * lb + c.Pu * lc;
                    Vector3 pv = a.Pv * la + b.Pv * lb + c.Pv * lc;
                    n = NormalMapping.Perturb(pu, pv, n, NormalMapping.Decode(map.Sample(u, v)));
                }

                Vector3 objectColor = _s.ColorSource == ObjectColorSource.Texture && _s.Texture is { } tex
                    ? tex.Sample(u, v)
                    : _s.SolidColor;

                _fb.Color[rowOffset + x] =
                    Lighting.ToArgb(Lighting.Shade(n, point, _s.LightPosition, objectColor, _s.Lighting));
            }
        }
    }
}
