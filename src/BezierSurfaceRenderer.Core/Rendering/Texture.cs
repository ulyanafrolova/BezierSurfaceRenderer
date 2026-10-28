using System.Numerics;

namespace BezierSurfaceRenderer.Core.Rendering;

/// <summary>
/// UI-independent image (decoded once from a GDI+ Bitmap in the App layer).
/// Sampled at (u, v) ∈ [0,1]²: the whole image is stretched over each surface's parameter domain.
/// </summary>
public sealed class Texture
{
    public int Width { get; }
    public int Height { get; }

    /// <param name="argbPixels">Row-major 32-bit ARGB, row 0 = top of image.</param>
    public Texture(int width, int height, int[] argbPixels) => throw new NotImplementedException();

    /// <summary>Nearest-texel lookup, RGB in [0,1]. u → column, v → row.</summary>
    public Vector3 Sample(float u, float v) => throw new NotImplementedException();
}

public static class NormalMapping
{
    /// <summary>RGB ∈ [0,1] → N_tex ∈ [−1,1]³: n = 2c − 1, normalised (RGB(127,127,255) ≈ [0,0,1]).</summary>
    public static Vector3 Decode(Vector3 rgb01) => throw new NotImplementedException();

    /// <summary>
    /// N = M · N_tex, M = [Pu, Pv, N_surface] as columns (spec §6). Pu, Pv are normalised first;
    /// falls back to N_surface when a tangent is degenerate. Result normalised.
    /// </summary>
    public static Vector3 Perturb(Vector3 pu, Vector3 pv, Vector3 nSurface, Vector3 nTexture)
        => throw new NotImplementedException();
}
