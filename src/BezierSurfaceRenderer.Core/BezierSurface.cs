using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>Point and first derivatives of a surface at (u, v).</summary>
public readonly record struct SurfaceSample(Vector3 P, Vector3 Pu, Vector3 Pv, Vector3 N);

/// <summary>
/// Bicubic Bézier surface (n = m = 3). ControlPoints[i, j] = V_ij, where i pairs with u
/// and j with v. File line k (0..15) maps to i = k / 4, j = k % 4.
///   P(u,v)  = Σ_i Σ_j V_ij B_i,3(u) B_j,3(v)
///   Pu(u,v) = 3 Σ_i<3 Σ_j (V_i+1,j − V_ij) B_i,2(u) B_j,3(v)
///   Pv(u,v) = 3 Σ_i Σ_j<3 (V_i,j+1 − V_ij) B_i,3(u) B_j,2(v)
///   N = normalize(Pu × Pv)
/// </summary>
public sealed class BezierSurface
{
    public const int Degree = 3;
    public const int PointsPerSurface = 16;

    public Vector3[,] ControlPoints { get; }

    // Object colour IO, chosen independently for each surface.
    public Vector3 SolidColor { get; set; } = new(0.85f, 0.88f, 0.93f);
    public Texture? Texture { get; set; }
    public bool UseTexture { get; set; }

    // Normal map, also chosen independently for each surface.
    public Texture? NormalMap { get; set; }
    public bool UseNormalMap { get; set; }

    /// <summary>IO at (u, v): texel of this surface's texture or its solid colour.</summary>
    public Vector3 ObjectColor(float u, float v) =>
        UseTexture && Texture != null ? Texture.Sample(u, v) : SolidColor;

    public BezierSurface(Vector3[,] controlPoints)
    {
        if (controlPoints.GetLength(0) != Degree + 1 || controlPoints.GetLength(1) != Degree + 1)
            throw new ArgumentException("A bicubic surface needs a 4×4 control net.", nameof(controlPoints));
        ControlPoints = (Vector3[,])controlPoints.Clone();
    }

    public SurfaceSample Evaluate(float u, float v)
    {
        Span<float> bu3 = stackalloc float[4], bv3 = stackalloc float[4];
        Span<float> bu2 = stackalloc float[3], bv2 = stackalloc float[3];
        for (int k = 0; k <= 3; k++) { bu3[k] = Bernstein.B(k, 3, u); bv3[k] = Bernstein.B(k, 3, v); }
        for (int k = 0; k <= 2; k++) { bu2[k] = Bernstein.B(k, 2, u); bv2[k] = Bernstein.B(k, 2, v); }

        var V = ControlPoints;
        Vector3 p = Vector3.Zero, pu = Vector3.Zero, pv = Vector3.Zero;
        for (int i = 0; i <= 3; i++)
        for (int j = 0; j <= 3; j++)
        {
            p += V[i, j] * (bu3[i] * bv3[j]);
            if (i < 3) pu += (V[i + 1, j] - V[i, j]) * (bu2[i] * bv3[j]);
            if (j < 3) pv += (V[i, j + 1] - V[i, j]) * (bu3[i] * bv2[j]);
        }
        pu *= Degree;
        pv *= Degree;

        return new SurfaceSample(p, pu, pv, SafeNormal(pu, pv));
    }

    /// <summary>
    /// Pu × Pv normalised. At a degenerate point (e.g. collinear boundary control points) the cross
    /// product vanishes; fall back to +z, the "up" direction of a surface that is a function over xy.
    /// </summary>
    private static Vector3 SafeNormal(Vector3 pu, Vector3 pv)
    {
        Vector3 n = Vector3.Cross(pu, pv);
        float len = n.Length();
        return len > 1e-8f ? n / len : Vector3.UnitZ;
    }
}

public static class Bernstein
{
    private static readonly int[][] Binomial = [[1], [1, 1], [1, 2, 1], [1, 3, 3, 1]];

    /// <summary>B_i,n(t) = C(n,i) t^i (1−t)^(n−i), for n ≤ 3.</summary>
    public static float B(int i, int n, float t)
    {
        if ((uint)n > 3) throw new ArgumentOutOfRangeException(nameof(n));
        if (i < 0 || i > n) return 0f;
        return Binomial[n][i] * Pow(t, i) * Pow(1 - t, n - i);
    }

    private static float Pow(float x, int e) => e switch { 0 => 1f, 1 => x, 2 => x * x, _ => x * x * x };
}
