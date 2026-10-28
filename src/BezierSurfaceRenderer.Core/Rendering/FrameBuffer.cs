namespace BezierSurfaceRenderer.Core.Rendering;

/// <summary>
/// Software colour + depth buffer. Pixels are 32-bit BGRA (matches GDI+ Format32bppArgb for a
/// direct copy via LockBits). Scene coordinates have the origin at the centre with y up (spec §1):
///   column = W/2 + x,  row = H/2 − y
/// Depth: observer looks along −z from +z (V = [0,0,1]), so LARGER z is closer; cleared to −∞.
/// </summary>
public sealed class FrameBuffer
{
    public int Width { get; }
    public int Height { get; }
    public int[] Color { get; }
    public float[] Depth { get; }

    public FrameBuffer(int width, int height) => throw new NotImplementedException();

    public void Clear(int argb) => throw new NotImplementedException();

    public float ToColumn(float x) => throw new NotImplementedException();
    public float ToRow(float y) => throw new NotImplementedException();

    /// <summary>Depth test + write: returns true and stores z if z is nearer than the stored value.</summary>
    public bool TestAndSetDepth(int column, int row, float z) => throw new NotImplementedException();
}
