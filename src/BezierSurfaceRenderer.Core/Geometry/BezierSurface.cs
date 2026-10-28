using System.Numerics;

namespace BezierSurfaceRenderer.Core.Geometry;

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

    public BezierSurface(Vector3[,] controlPoints) => throw new NotImplementedException();

    public SurfaceSample Evaluate(float u, float v) => throw new NotImplementedException();
}

public static class Bernstein
{
    /// <summary>B_i,n(t) = C(n,i) t^i (1−t)^(n−i), for n ≤ 3.</summary>
    public static float B(int i, int n, float t) => throw new NotImplementedException();
}
