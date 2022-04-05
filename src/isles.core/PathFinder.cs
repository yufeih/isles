// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Isles;

public record PathGrid(int Width, int Height, float Step, BitArray Bits)
{
    public int GetIndex(Vector2 position)
    {
        var x = Math.Min(Width - 1, Math.Max(0, (int)(position.X / Step)));
        var y = Math.Min(Height - 1, Math.Max(0, (int)(position.Y / Step)));

        return y * Width + x;
    }

    public Vector2 GetPosition(int index)
    {
        var y = Math.DivRem(index, Width, out var x);

        return new(x * Step + Step / 2, y * Step + Step / 2);
    }
}

public class PathFinder
{
    private readonly GraphSearchAStar _search = new();

    public ReadOnlySpan<Vector2> FindPath(PathGrid grid, Vector2 start, Vector2 end)
    {
        if (!_search.Search(new PathGridGraph(grid), grid.GetIndex(start), grid.GetIndex(end)))
        {
            return Array.Empty<Vector2>().AsSpan();
        }

        return _search.Path.Select(grid.GetPosition).ToArray().AsSpan();
    }

    record PathGridGraph(PathGrid grid) : IGraph
    {
        // 6  7  0
        //   \|/
        // 5 - - 1
        //   /|\
        // 4  3  2
        private static readonly (int dx, int dy, float cost)[] s_edges = new[]
        {
            (1, -1, 1.41421356237f),
            (1, 0, 1.0f),
            (1, 1, 1.41421356237f),
            (0, 1, 1.0f),
            (-1, 1, 1.41421356237f),
            (-1, 0, 1.0f),
            (-1, -1, 1.41421356237f),
            (0, -1, 1.0f),
        };

        public int NodeCount => grid.Width * grid.Height;

        public IEnumerable<GraphEdge> GetEdges(int nodeIndex)
        {
            var y = Math.DivRem(nodeIndex, grid.Width, out var x);
            if (grid.Bits[x + y * grid.Width])
            {
                yield break;
            }

            foreach (var edge in s_edges)
            {
                var xx = x + edge.dx;
                var yy = y + edge.dy;
                if (xx < 0 || xx >= grid.Width || yy < 0 || yy >= grid.Height)
                {
                    continue;
                }

                var i = xx + yy * grid.Width;
                if (grid.Bits[i])
                {
                    continue;
                }

                yield return new() { From = nodeIndex, To = i, Cost = edge.cost };
            }
        }

        public float GetHeuristicValue(int currentIndex, int endIndex)
        {
            return (grid.GetPosition(endIndex) - grid.GetPosition(currentIndex)).Length();
        }
    }
}
