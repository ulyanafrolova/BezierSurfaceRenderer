using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BezierSurfaceRenderer.App;

/// <summary>The only place GDI+ bitmaps meet Core types (keeps Core testable on any OS).</summary>
internal static class BitmapInterop
{
    /// <summary>Loads an image file (any GDI+ format) into a Core Texture via LockBits.</summary>
    public static Texture LoadTexture(string path)
    {
        using var source = new Bitmap(path);
        int w = source.Width, h = source.Height;
        using var argb = source.Clone(new Rectangle(0, 0, w, h), PixelFormat.Format32bppArgb);

        var pixels = new int[w * h];
        var data = argb.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (int row = 0; row < h; row++)
                Marshal.Copy(data.Scan0 + row * data.Stride, pixels, row * w, w);
        }
        finally
        {
            argb.UnlockBits(data);
        }
        return new Texture(w, h, pixels) { Name = Path.GetFileName(path) };
    }

    /// <summary>Copies FrameBuffer.Color into a 32bpp bitmap of the same size.</summary>
    public static void CopyToBitmap(FrameBuffer source, Bitmap target)
    {
        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var data = target.LockBits(rect, ImageLockMode.WriteOnly, target.PixelFormat);
        try
        {
            if (data.Stride == source.Width * 4)
                Marshal.Copy(source.Color, 0, data.Scan0, source.Color.Length);
            else
                for (int row = 0; row < source.Height; row++)
                    Marshal.Copy(source.Color, row * source.Width, data.Scan0 + row * data.Stride, source.Width);
        }
        finally
        {
            target.UnlockBits(data);
        }
    }
}
