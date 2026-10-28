using System.Numerics;

namespace BezierSurfaceRenderer.Core.Rendering;

/// <summary>Material/light parameters shared by all triangles (spec §4). Colours are RGB in [0,1].</summary>
public sealed record LightingParameters(float Kd, float Ks, int M, Vector3 LightColor)
{
    public static LightingParameters Default => new(0.5f, 0.5f, 20, Vector3.One);
}

/// <summary>
/// I = kd·IL·IO·cos(N,L) + ks·IL·IO·cos^m(V,R), evaluated per channel (component-wise Vector3).
///   L = normalize(lightPos − P), V = [0,0,1], R = 2⟨N,L⟩N − L (normalised)
///   negative cosines clamp to 0; result is in [0,1] until <see cref="ToArgb"/> scales ×255 and clamps.
/// </summary>
public static class Lighting
{
    public static readonly Vector3 ViewVector = new(0, 0, 1);

    /// <param name="n">Unit surface normal.</param>
    public static Vector3 Shade(Vector3 n, Vector3 point, Vector3 lightPosition, Vector3 objectColor,
                                LightingParameters p)
    {
        Vector3 l = Vector3.Normalize(lightPosition - point);
        float nDotL = Vector3.Dot(n, l);
        float cosNL = MathF.Max(0f, nDotL);

        Vector3 r = Vector3.Normalize(2f * nDotL * n - l);
        float cosVR = MathF.Max(0f, Vector3.Dot(ViewVector, r));
        float specular = cosVR == 0f ? 0f : MathF.Pow(cosVR, p.M);

        return p.LightColor * objectColor * (p.Kd * cosNL + p.Ks * specular);
    }

    public static int ToArgb(Vector3 rgb01)
    {
        int r = ToByte(rgb01.X), g = ToByte(rgb01.Y), b = ToByte(rgb01.Z);
        return unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
    }

    private static int ToByte(float c) => (int)MathF.Min(255f, MathF.Max(0f, MathF.Round(c * 255f)));
}
