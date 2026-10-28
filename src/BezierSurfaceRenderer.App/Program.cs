using BezierSurfaceRenderer.Core.Geometry;
using BezierSurfaceRenderer.Core.SceneModel;

namespace BezierSurfaceRenderer.App;

internal static class Program
{
    private const int InitialDivisions = 16;

    /// <summary>
    /// Startup: loads Assets/surfaces.txt (or the path given as the first argument) BEFORE the form
    /// is shown (spec §1). On a read/parse error shows a message box naming the file and line, then exits.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        string path = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(AppContext.BaseDirectory, "Assets", "surfaces.txt");

        IReadOnlyList<BezierSurface> surfaces;
        try
        {
            surfaces = SurfaceFileParser.Load(path);
        }
        catch (Exception ex) when (ex is SurfaceFileException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Cannot load Bézier surfaces from:\n{path}\n\n{ex.Message}",
                "Bézier Surface Renderer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm(new Scene(surfaces, InitialDivisions)));
    }
}
