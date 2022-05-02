using Xunit;

namespace Isles;

public static class Snapshot
{
    public static string SnapshotDirectory { get; }

    static Snapshot()
    {
        SnapshotDirectory = Path.Combine(FindRepositoryRoot(), "snapshots");
        Directory.CreateDirectory(SnapshotDirectory);
    }

    public static void Save(string name, string actual)
    {
        var path = Path.Combine(SnapshotDirectory, name);
        var expected = File.Exists(path) ? File.ReadAllText(path) : null;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, actual);

        if (expected != null)
            Assert.Equal(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
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
