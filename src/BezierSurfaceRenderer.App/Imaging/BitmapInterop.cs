using BezierSurfaceRenderer.Core.Rendering;

namespace BezierSurfaceRenderer.App.Imaging;

/// <summary>The only place GDI+ bitmaps meet Core types (keeps Core testable on any OS).</summary>
internal static class BitmapInterop
{
    /// <summary>Loads an image file (any GDI+ format) into a Core Texture via LockBits.</summary>
    public static Texture LoadTexture(string path) => throw new NotImplementedException();

    /// <summary>Copies FrameBuffer.Color into a 32bpp bitmap of the same size in one Marshal.Copy.</summary>
    public static void CopyToBitmap(FrameBuffer source, Bitmap target) => throw new NotImplementedException();
}
