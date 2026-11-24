using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>
/// Light source: position and colour IL (RGB in [0,1]). SpotExponent = 0: point light shining in every
/// direction. SpotExponent p > 0: spotlight shining along the unit vector SpotDirection (Warn model,
/// see <see cref="Lighting.SpotFactor"/>); a larger p gives a narrower beam.
/// </summary>
public readonly record struct PointLight(Vector3 Position, Vector3 Color, Vector3 SpotDirection = default, float SpotExponent = 0f);

/// <summary>
/// Per-frame parameters shared by all triangles. Colours are RGB in [0,1].
/// Lights: one light, or the three red/green/blue lights moving along the path.
/// </summary>
public sealed record RenderSettings(float Kd, float Ks, int M, IReadOnlyList<PointLight> Lights);

/// <summary>
/// I = Σ over lights [ kd·IL·IO·cos(N,L) + ks·IL·IO·cos^m(V,R) ], evaluated per channel (component-wise Vector3).
/// With several lights the single-light term is computed for each of them and the results are added.
/// For a spotlight IL is replaced by IL·cos^p(−L, D) (see <see cref="SpotFactor"/>).
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
        Vector3 sum = Vector3.Zero;
        foreach (var light in s.Lights)
            sum += ShadeOne(n, point, objectColor, light, s);
        return sum;
    }

    private static Vector3 ShadeOne(Vector3 n, Vector3 point, Vector3 objectColor, PointLight light, RenderSettings s)
    {
        Vector3 l = Vector3.Normalize(light.Position - point);
        float nDotL = Vector3.Dot(n, l);
        float cosNL = MathF.Max(0f, nDotL);

        Vector3 r = Vector3.Normalize(2f * nDotL * n - l);
        float cosVR = MathF.Max(0f, Vector3.Dot(ViewVector, r));
        float specular = nDotL > 0 ? MathF.Pow(cosVR, s.M) : 0f; // no highlight on the side the light does not reach

        return light.Color * SpotFactor(light, l) * objectColor * (s.Kd * cosNL + s.Ks * specular);
    }

    /// <summary>
    /// Spotlight (Warn model): the light reaching P is IL·cos^p(α), where α is the angle between the beam
    /// axis D and the direction from the light to P, i.e. −L. Points behind the light (cos α ≤ 0) get nothing.
    /// On the axis the factor is 1, and it drops faster away from the axis the larger p is. Point light: 1.
    /// </summary>
    public static float SpotFactor(PointLight light, Vector3 l)
    {
        if (light.SpotExponent <= 0f) return 1f;
        float cosAlpha = Vector3.Dot(-l, light.SpotDirection);
        return cosAlpha > 0f ? MathF.Pow(cosAlpha, light.SpotExponent) : 0f;
    }

    public static int ToArgb(Vector3 rgb01)
    {
        int r = ToByte(rgb01.X), g = ToByte(rgb01.Y), b = ToByte(rgb01.Z);
        return unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
    }

    private static int ToByte(float c) => (int)MathF.Min(255f, MathF.Max(0f, MathF.Round(c * 255f)));
}
