
namespace BezierSurfaceRenderer.App;

internal static class Program
{
    private const int InitialDivisions = 16;

    /// <summary>
    /// Startup: loads Assets/surfaces.txt before the form
    /// is shown. On a read/parse error shows a message box naming the file and line, then exits.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "surfaces.txt");

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
