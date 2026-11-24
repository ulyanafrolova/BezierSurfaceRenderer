namespace BezierSurfaceRenderer.Core;

/// <summary>
/// Software colour + depth buffer. Pixels are 32-bit ARGB ints (in memory BGRA, i.e. GDI+
/// Format32bppArgb, so a single copy moves them into a Bitmap). Scene coordinates have the origin at
/// the centre with y up; <see cref="PixelsPerUnit"/> scales model units to pixels:
///   column = W/2 + x·s,  row = H/2 − y·s
/// Depth: observer looks along −z from +z (V = [0,0,1]), so larger z is closer; cleared to −∞.
/// </summary>
public sealed class FrameBuffer
{
    public int Width { get; }
    public int Height { get; }
    public int[] Color { get; }
    public float[] Depth { get; }
    public float PixelsPerUnit { get; set; } = 1f;

    public FrameBuffer(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        Color = new int[width * height];
        Depth = new float[width * height];
    }

    public void Clear(int argb)
    {
        Array.Fill(Color, argb);
        Array.Fill(Depth, float.NegativeInfinity);
    }

    public float ToColumn(float x) => Width / 2f + x * PixelsPerUnit;
    public float ToRow(float y) => Height / 2f - y * PixelsPerUnit;

    /// <summary>Depth test + write: returns true and stores z if z is nearer than the stored value.</summary>
    public bool TestAndSetDepth(int column, int row, float z)
    {
        int index = row * Width + column;
        if (z <= Depth[index]) return false;
        Depth[index] = z;
        return true;
    }
}
