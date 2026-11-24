using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>
/// Per-frame parameters shared by all triangles (spec §4). Colours are RGB in [0,1].
/// </summary>
public sealed record RenderSettings(float Kd, float Ks, int M, Vector3 LightColor, Vector3 LightPosition);

/// <summary>
/// I = kd·IL·IO·cos(N,L) + ks·IL·IO·cos^m(V,R), evaluated per channel (component-wise Vector3).
///   L = normalize(lightPos − P), V = [0,0,1], R = 2⟨N,L⟩N − L (normalised).
/// Both sides of the surface are lit: the caller turns N towards the observer (see SceneRenderer).
/// A cosine below 0 means the light is behind the surface: the diffuse term is 0 and there is no highlight.
/// The result stays in [0,1] until <see cref="ToArgb"/> scales ×255 and clamps.
/// </summary>
public static class Lighting
{
    public static readonly Vector3 ViewVector = new(0, 0, 1);

    /// <param name="n">Unit surface normal.</param>
    public static Vector3 Shade(Vector3 n, Vector3 point, Vector3 objectColor, RenderSettings s)
    {
        Vector3 l = Vector3.Normalize(s.LightPosition - point);
        float nDotL = Vector3.Dot(n, l);
        float cosNL = MathF.Max(0f, nDotL);

        Vector3 r = Vector3.Normalize(2f * nDotL * n - l);
        float cosVR = MathF.Max(0f, Vector3.Dot(ViewVector, r));
        float specular = nDotL > 0 ? MathF.Pow(cosVR, s.M) : 0f; // no highlight on the side the light does not reach

        return s.LightColor * objectColor * (s.Kd * cosNL + s.Ks * specular);
    }

    public static int ToArgb(Vector3 rgb01)
    {
        int r = ToByte(rgb01.X), g = ToByte(rgb01.Y), b = ToByte(rgb01.Z);
        return unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
    }

    private static int ToByte(float c) => (int)MathF.Min(255f, MathF.Max(0f, MathF.Round(c * 255f)));
}
