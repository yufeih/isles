using System.Collections;
using Xunit;

namespace Isles;

public class PathFinderTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FlowField(int pathWidth)
    {
        var random = new Random(0);
        var svg = new SvgBuilder();
        var grid = CreateRandomGrid(random);

        svg.AddGrid(grid);

        var pathfinder = new PathFinder();
        var end = new Vector2(random.NextSingle() * grid.Width, random.NextSingle() * grid.Height);
        var flowField = pathfinder.GetFlowField(grid, pathWidth, end);

        svg.AddCircle(end.X, end.Y, pathWidth * 0.5f, "red");

        for (var y = 0; y < grid.Height; y++)
            for (var x = 0; x < grid.Width; x++)
            {
                var position = new Vector2(x * grid.Step, y * grid.Step);
                var v = flowField.GetVector(position);
                svg.AddLine(position, position + v, 0.1f, "gray", data: new() { Opacity = 0.5f });
            }

        Snapshot.Save($"move/flowfield-{pathWidth}.svg", svg.ToString());
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
