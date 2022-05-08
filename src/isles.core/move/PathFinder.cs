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
    private readonly Dictionary<(int, int), FlowField<PathGridGraph>?> _flowFields = new();

    public IFlowField? GetFlowField(PathGrid grid, float pathWidth, Vector2 target)
    {
        var size = (int)MathF.Ceiling(pathWidth / grid.Step);
        var targetIndex = grid.GetIndex(target);

        if (!_flowFields.TryGetValue((targetIndex, size), out var result))
        {
            result = _flowFields[(targetIndex, size)] = FlowField<PathGridGraph>.Create(
                new PathGridGraph(grid, size), targetIndex);
        }

        return result;
    }

    struct PathGridGraph : IPathGraph2
    {
        private const float D = 1.414213562373095f;

        private static readonly (int dx, int dy, float cost)[] s_edges = new[]
        {
            (0, -1, 1), (1, -1, D), (1, 0, 1), (1, 1, D),
            (0, 1, 1), (-1, 1, D), (-1, 0, 1), (-1, -1, D),
        };

        private readonly PathGrid _grid;
        private readonly int _size;

        public PathGridGraph(PathGrid grid, int size)
        {
            _grid = grid;
            _size = size;
        }

        public int MaxEdgeCount => 8;

        public int NodeCount => _grid.Width * _grid.Height;

        public Vector2 GetPosition(int i)
        {
            var y = Math.DivRem(i, _grid.Width, out var x);
            return new((x + 0.5f) * _grid.Step, (y + 0.5f) * _grid.Step);
        }

        public int GetNodeIndex(Vector2 position)
        {
            return _grid.GetIndex(position);
        }

        public int GetEdges(int from, Span<(int to, float cost)> edges)
        {
            return _size == 1 ? GetEdges1(from, edges) : GetEdgesN(from, edges);
        }

        private int GetEdges1(int from, Span<(int to, float cost)> edges)
        {
            var count = 0;
            var y = Math.DivRem(from, _grid.Width, out var x);

            foreach (var (dx, dy, cost) in s_edges)
            {
                var xx = x + dx;
                var yy = y + dy;

                if (xx < 0 || xx >= _grid.Width || yy < 0 || yy >= _grid.Height)
                {
                    continue;
                }

                var i = xx + yy * _grid.Width;
                if (!_grid.Bits[i])
                {
                    edges[count++] = (i, _grid.Step);
                }
            }

            return count;
        }

        private int GetEdgesN(int from, Span<(int to, float cost)> edges)
        {
            var count = 0;
            var half = (_size - 1) / 2;
            var y = Math.DivRem(from, _grid.Width, out var x);

            foreach (var (dx, dy, cost) in s_edges)
            {
                var xx = x + dx;
                var yy = y + dy;
                var i = xx + yy * _grid.Width;
                var valid = true;

                if (dx == 0)
                {
                    xx -= half;
                    yy += dy > 0 ? _size - 1 - half : -half;
                }
                else
                {
                    yy -= half;
                    xx += dx > 0 ? _size - 1 - half : -half;
                }

                for (var k = 0; k < _size; k++)
                {
                    if (xx < 0 || xx >= _grid.Width || yy < 0 || yy >= _grid.Height)
                    {
                        valid = false;
                        continue;
                    }

                    if (_grid.Bits[xx + yy * _grid.Width])
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
                    edges[count++] = (i, _grid.Step);
                }
            }

            return count;
        }
    }
}
