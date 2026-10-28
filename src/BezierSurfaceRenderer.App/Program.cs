namespace BezierSurfaceRenderer.App;

internal static class Program
{
    /// <summary>
    /// Startup: loads Assets/surfaces.txt (or the path given as the first argument) BEFORE the form
    /// is shown (spec §1). On a parse error shows a message box naming file + line and exits.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        throw new NotImplementedException();
    }
}
