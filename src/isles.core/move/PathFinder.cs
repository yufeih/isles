// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Isles;

public record PathGrid(int Width, int Height, float Step, BitArray Bits);

public class PathFinder
{
    private readonly Dictionary<(int, int), FlowField<PathGridGraph>> _flowFields = new();

    public IFlowField? GetFlowField(PathGrid grid, float pathWidth, Vector2 target)
    {
        var size = (int)MathF.Ceiling(pathWidth / grid.Step);
        var graph = new PathGridGraph(grid, size);
        var targetIndex = graph.GetNodeIndex(target);
        if (!graph.IsValidLocation(targetIndex))
            return null;

        if (!_flowFields.TryGetValue((targetIndex, size), out var result))
            result = _flowFields[(targetIndex, size)] = FlowField<PathGridGraph>.Create(graph, targetIndex);

        return result;
    }

    struct PathGridGraph : IPathGraph2
    {
        private const float DCost = 1.414213562373095f;

        private static readonly (int dx, int dy)[] s_steps = new[]
        {
            (0, -1), (1, 0), (0, 1), (-1, 0)
        };

        private static readonly (int multiplier, int lineX, int lineY)[] s_edges = new[]
        {
            (0, 1, 0), (1, 0, 1), (1, 1, 0), (0, 0, 1),
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

        public bool IsValidLocation(int index)
        {
            var y = Math.DivRem(index, _grid.Width, out var x);
            for (var yy = y; yy < y + _size; yy++)
                for (var xx = x; xx < x + _size; xx++)
                    if (CheckBit(xx, yy))
                        return false;
            return true;
        }

        public Vector2 GetPosition(int index)
        {
            var y = Math.DivRem(index, _grid.Width, out var x);
            return new((x + 0.5f * _size) * _grid.Step, (y + 0.5f * _size) * _grid.Step);
        }

        public int GetNodeIndex(Vector2 position)
        {
            var x = Math.Min(_grid.Width - 1, Math.Max(0, (int)(position.X / _grid.Step - 0.5f * (_size - 1))));
            var y = Math.Min(_grid.Height - 1, Math.Max(0, (int)(position.Y / _grid.Step - 0.5f * (_size - 1))));

            return y * _grid.Width + x;
        }

        public int GetEdges(int from, Span<(int to, float cost)> edges)
        {
            return _size == 1 ? GetEdges1(from, edges) : GetEdgesN(from, edges);
        }

        private int GetEdges1(int from, Span<(int to, float cost)> edges)
        {
            var count = 0;
            var y = Math.DivRem(from, _grid.Width, out var x);

            // Horizontal and vertical edges
            for (var i = 0; i < 4; i++)
            {
                var (dx, dy) = s_steps[i];
                var (xx, yy) = (x + dx, y + dy);
                if (CheckBit(xx, yy))
                    continue;

                edges[count++] = (xx + yy * _grid.Width, 1);
            }

            // Diagonal edges
            for (var i = 0; i < 4; i++)
            {
                var (e1, e2) = (i, (i + 1) % 4);
                var (dx1, dy1) = s_steps[e1];
                var (dx2, dy2) = s_steps[e2];
                var (xx, yy) = (x + dx1 + dx2, y + dy1 + dy2);

                if (CheckBit(xx, yy) || CheckBit(x + dx1, y + dy1) || CheckBit(x + dx2, y + dy2))
                    continue;

                edges[count++] = (xx + yy * _grid.Width, DCost);
            }

            return count;
        }

        private int GetEdgesN(int from, Span<(int to, float cost)> edges)
        {
            var count = 0;
            var y = Math.DivRem(from, _grid.Width, out var x);

            // Horizontal and vertical edges
            for (var i = 0; i < 4; i++)
            {
                var (m, lx, ly) = s_edges[i];
                var (dx, dy) = s_steps[i];
                var (xx, yy) = (x + dx, y + dy);

                dx *= (m * (_size - 1) + 1);
                dy *= (m * (_size - 1) + 1);

                if (CheckBits(x + dx, y + dy, lx, ly, _size))
                    continue;

                edges[count++] = (xx + yy * _grid.Width, 1);
            }

            // Diagonal edges
            for (var i = 0; i < 4; i++)
            {
                var (e1, e2) = (i, (i + 1) % 4);
                var (m1, lx1, ly1) = s_edges[e1];
                var (m2, lx2, ly2) = s_edges[e2];
                var (dx1, dy1) = s_steps[e1];
                var (dx2, dy2) = s_steps[e2];
                var (xx, yy) = (x + dx1 + dx2, y + dy1 + dy2);

                dx1 *= (m1 * (_size - 1) + 1);
                dy1 *= (m1 * (_size - 1) + 1);
                dx2 *= (m2 * (_size - 1) + 1);
                dy2 *= (m2 * (_size - 1) + 1);

                if (CheckBit(x + dx1 + dx2, y + dy1 + dy2) ||
                    CheckBits(x + dx1, y + dy1, lx1, ly1, _size) ||
                    CheckBits(x + dx2, y + dy2, lx2, ly2, _size))
                    continue;

                edges[count++] = (xx + yy * _grid.Width, DCost);
            }

            return count;
        }

        private bool CheckBit(int x, int y)
        {
            if (x < 0 || x >= _grid.Width || y < 0 || y >= _grid.Height)
            {
                return true;
            }

            var index = x + y * _grid.Width;
            return _grid.Bits[index];
        }

        private bool CheckBits(int x, int y, int dx, int dy, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (CheckBit(x, y))
                    return true;
                x += dx;
                y += dy;
            }
            return false;
        }
    }
}
