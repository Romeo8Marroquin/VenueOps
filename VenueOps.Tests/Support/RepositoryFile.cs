namespace VenueOps.Tests.Support;

/// <summary>
/// Reads a file of the repository as text, for checks that cannot run the code: XAML markup,
/// and sources that are not compiled for the plain net10.0 test target (for example MauiProgram).
/// </summary>
internal static class RepositoryFile
{
    private const string Marker = "VenueOps.slnx";

    public static string ReadAllText(string relativePath)
    {
        string root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, relativePath));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, Marker)))
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new DirectoryNotFoundException($"Repository root (folder containing {Marker}) not found above {AppContext.BaseDirectory}.");
    }
}
