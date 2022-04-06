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
        var grid = CreateRandomGrid(random);

        svg.AddGrid(grid);

        var i = 0;
        var pathfinder = new PathFinder();
        while (i < 10)
        {
            var path = pathfinder.FindPath(
                grid,
                pathWidth,
                new(random.NextSingle() * grid.Width * grid.Step, random.NextSingle() * grid.Width * grid.Step),
                new(random.NextSingle() * grid.Width * grid.Step, random.NextSingle() * grid.Width * grid.Step));

            if (path.Length > 0)
            {
                i++;
                var lines = path.ToArray().Select(p => p + Vector2.One * (pathWidth % 2 == 0 ? 0.5f : 0)).ToArray();
                svg.AddLineSegments(lines, pathWidth, data: new() { Opacity = 0.5f });
            }
        }

        Snapshot.Save($"move/pathfinder-{pathWidth}.svg", svg.ToString());
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
