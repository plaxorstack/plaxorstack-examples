namespace DatasetBuilder;

internal static class RepoPaths
{
    // The repository root is the first parent holding DATA-LICENSES.md, so the tool works whether
    // it is started from the root, from its own folder or from bin/.
    public static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DATA-LICENSES.md")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Run this from inside the plaxorstack-examples repository.");
    }

    public static string DataFolder(string repoRoot, string sample)
    {
        var path = Path.Combine(repoRoot, sample, "data");
        Directory.CreateDirectory(path);
        return path;
    }
}
