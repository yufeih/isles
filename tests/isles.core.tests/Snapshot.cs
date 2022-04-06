using Xunit;

namespace Isles;

public static class Snapshot
{
    private static readonly bool s_isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
    private static readonly string s_baseDirectory = FindRepositoryRoot();

    public static void Save(string name, string actual)
    {
        var path = Path.Combine(s_baseDirectory, "tests", "snapshots", name);
        var expected = File.Exists(path) ? File.ReadAllText(path) : null;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, actual);

        if (expected is null && s_isCI)
        {
            throw new InvalidOperationException($"Cannot find snapshot file '{path}'");
        }

        if (expected is null)
        {
            return;
        }

        try
        {
            Assert.Equal(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }
        catch when (s_isCI)
        {
            var failedPath = Path.Combine(s_baseDirectory, "tests", "failed", name);
            Directory.CreateDirectory(Path.GetDirectoryName(failedPath)!);
            File.WriteAllText(failedPath, actual);
            throw;
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !Directory.Exists(Path.Combine(directory, ".git")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory!;
    }
}
