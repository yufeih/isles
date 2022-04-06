// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Isles;

public record PathGrid(int Width, int Height, float Step, BitArray Bits)
{
    public (int x, int y) GetPoint(Vector2 position)
    {
        var x = Math.Min(Width - 1, Math.Max(0, (int)(position.X / Step)));
        var y = Math.Min(Height - 1, Math.Max(0, (int)(position.Y / Step)));

        return (x, y);
    }

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
    private ArrayBuilder<Vector2> _result;

    public ReadOnlySpan<Vector2> FindPath(PathGrid grid, float pathWidth, Vector2 start, Vector2 end)
    {
        var size = (int)MathF.Ceiling(pathWidth / grid.Step);
        var path = _search.Search(new PathGridGraph(grid, size), grid.GetIndex(start), grid.GetIndex(end));

        return _result.ConvertAll(path, grid.GetPosition);
    }

    record PathGridGraph(PathGrid grid, int size) : IGraph
    {
        private static readonly (int dx, int dy)[] s_edges = new[]
        {
            (1, 0), (0, 1), (-1, 0), (0, -1),
        };

        public int NodeCount => grid.Width * grid.Height;

        public IEnumerable<(int to, float cost)> GetEdges(int from)
        {
            var half = (size - 1) / 2;
            var y = Math.DivRem(from, grid.Width, out var x);

            foreach (var (dx, dy) in s_edges)
            {
                var xx = x + dx;
                var yy = y + dy;
                var i = xx + yy * grid.Width;
                var valid = true;

                if (dx == 0)
                {
                    xx -= half;
                    yy += dy > 0 ? size - 1 - half : -half;
                }
                else
                {
                    yy -= half;
                    xx += dx > 0 ? size - 1 - half : -half;
                }

                for (var k = 0; k < size; k++)
                {
                    if (xx < 0 || xx >= grid.Width || yy < 0 || yy >= grid.Height)
                    {
                        valid = false;
                        continue;
                    }

                    if (grid.Bits[xx + yy * grid.Width])
                    {
                        valid = false;
                        continue;
                    }

                    if (dx == 0)
                    {
                        xx++;
                    }
                    else
                    {
                        yy++;
                    }
                }

                if (valid)
                {
                    yield return (i, grid.Step);
                }
            }
        }

        public float GetHeuristicValue(int from, int to)
        {
            return (grid.GetPosition(to) - grid.GetPosition(from)).Length();
        }
    }
}
