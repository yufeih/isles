using System.Collections;
using Xunit;

namespace Isles;

public class PathFinderTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FindPath(int pathWidth)
    {
        var random = new Random(0);
        var svg = new SvgBuilder();
        var svgSmooth = new SvgBuilder();
        var grid = CreateRandomGrid(random);

        svg.AddGrid(grid);
        svgSmooth.AddGrid(grid);

        var i = 0;
        var pathfinder = new PathFinder();
        while (i < 10)
        {
            var start = new Vector2(random.NextSingle() * grid.Width, random.NextSingle() * grid.Height);
            var end = new Vector2(random.NextSingle() * grid.Width, random.NextSingle() * grid.Height);

            var path = pathfinder.FindPath(grid, pathWidth, start, end, smoothPath: false);
            if (path.Length > 0)
            {
                i++;
                var lines = new[]{ start }.Concat(path.ToArray()).Select(p => p + Vector2.One * (pathWidth % 2 == 0 ? 0.5f : 0)).ToArray();
                svg.AddLine(lines, pathWidth, data: new() { Opacity = 0.5f });

                var smoothPath = pathfinder.FindPath(grid, pathWidth, start, end, smoothPath: true);
                var smoothLines = new[]{ start }.Concat(smoothPath.ToArray()).Select(p => p + Vector2.One * (pathWidth % 2 == 0 ? 0.5f : 0)).ToArray();
                svgSmooth.AddLine(smoothLines, pathWidth, data: new() { Opacity = 0.5f });
            }
        }

        Snapshot.Save($"move/pathfinder-{pathWidth}.svg", svg.ToString());
        Snapshot.Save($"move/pathfinder-smooth-{pathWidth}.svg", svgSmooth.ToString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LineOfSight(int pathWidth)
    {
        var random = new Random(0);
        var svg = new SvgBuilder();
        var grid = CreateRandomGrid(random, density: 0.05f);

        svg.AddGrid(grid);

        var red = 0;
        var green = 0;
        while (red < 5 || green < 5)
        {
            var start = new Vector2(random.NextSingle() * grid.Width, random.NextSingle() * grid.Height);
            var end = new Vector2(random.NextSingle() * grid.Width, random.NextSingle() * grid.Height);
            var reachable = PathFinder.LineOfSightTest(grid, pathWidth, start, end);

            if ((reachable && green++ < 5) || (!reachable && red++ < 5))
            {
                svg.AddLine(new[] { start, end }, pathWidth, reachable ? "green" : "red", new() { Opacity = 0.2f });
            }
        }

        Snapshot.Save($"move/pathfinder-lineofsight-{pathWidth}.svg", svg.ToString());
    }

    private static PathGrid CreateRandomGrid(Random random, int w = 64, int h = 64, float density = 0.1f)
    {
        var bits = new BitArray(w * h);
        for (var i = 0; i < (int)(w * h * density); i++)
        {
            bits[random.Next(bits.Length)] = true;
        }
        return new PathGrid(w, h, 1, bits);
    }
}
