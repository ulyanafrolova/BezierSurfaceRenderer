using System.Numerics;

namespace BezierSurfaceRenderer.Core;

/// <summary>
/// UI-independent image (decoded once from a GDI+ Bitmap in the App layer).
/// Sampled at (u, v) ∈ [0,1]²: the whole image is stretched over each surface's parameter domain.
/// </summary>
public sealed class Texture
{
    private readonly Vector3[] _texels;

    public int Width { get; }
    public int Height { get; }
    public string Name { get; init; } = "";

    /// <param name="argbPixels">Row-major 32-bit ARGB, row 0 = top of image.</param>
    public Texture(int width, int height, int[] argbPixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (argbPixels.Length != width * height)
            throw new ArgumentException("Pixel count does not match the image size.", nameof(argbPixels));

        Width = width;
        Height = height;
        _texels = new Vector3[argbPixels.Length];
        for (int k = 0; k < argbPixels.Length; k++)
        {
            int c = argbPixels[k];
            _texels[k] = new Vector3((c >> 16) & 0xFF, (c >> 8) & 0xFF, c & 0xFF) / 255f;
        }
    }

    /// <summary>Single-colour texture (used in tests and as a fallback).</summary>
    public static Texture Solid(Vector3 rgb01) => new(1, 1, [Lighting.ToArgb(rgb01)]);

    /// <summary>Nearest-texel lookup, RGB in [0,1]. u → column, v → row.</summary>
    public Vector3 Sample(float u, float v)
    {
        int col = Math.Clamp((int)(u * Width), 0, Width - 1);
        int row = Math.Clamp((int)(v * Height), 0, Height - 1);
        return _texels[row * Width + col];
    }
}

public static class NormalMapping
{
    /// <summary>RGB ∈ [0,1] → N_tex ∈ [−1,1]³: n = 2c − 1, normalised (RGB(127,127,255) ≈ [0,0,1]).</summary>
    public static Vector3 Decode(Vector3 rgb01) => Vector3.Normalize(2f * rgb01 - Vector3.One);

    /// <summary>
    /// N = M · N_tex, M = [Pu, Pv, N_surface] as columns (spec §6). Pu, Pv are normalised first;
    /// falls back to N_surface when a tangent is degenerate. Result normalised.
    /// </summary>
    public static Vector3 Perturb(Vector3 pu, Vector3 pv, Vector3 nSurface, Vector3 nTexture)
    {
        float lu = pu.Length(), lv = pv.Length();
        if (lu < 1e-6f || lv < 1e-6f) return nSurface;

        Vector3 n = Mat3.FromColumns(pu / lu, pv / lv, nSurface) * nTexture;
        float ln = n.Length();
        return ln < 1e-6f ? nSurface : n / ln;
    }
}
