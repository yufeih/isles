// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Isles;

public record PathGrid(int Width, int Height, float Step, BitArray Bits);

public class PathFinder
{
    private readonly Dictionary<(Vector2, int), FlowField> _flowFields = new();

    public PathGridFlowField GetFlowField(PathGrid grid, float pathWidth, Vector2 target)
    {
        var size = (int)MathF.Ceiling(pathWidth / grid.Step);
        if (!_flowFields.TryGetValue((target, size), out var flowField))
            flowField = _flowFields[(target, size)] = FlowField.Create(
                new PathGridGraph(grid, size), target);

        return new() { Target = target, Grid = grid, FlowField = flowField };
    }
}

public struct PathGridFlowField
{
    public Vector2 Target;
    public PathGrid Grid;
    public FlowField FlowField;

    public bool IsValid => Grid != null;

    public Vector2 GetDirection(Vector2 position)
    {
        var x = position.X / Grid.Step - 0.5f;
        var y = position.Y / Grid.Step - 0.5f;
        if (x < 0 || x >= Grid.Width || y < 0 || y >= Grid.Height)
            return default;

        var (fx, fy) = (x % 1, y % 1);
        var (minx, miny) = ((int)x, (int)y);
        var (maxx, maxy) = (Math.Min(minx + 1, Grid.Width - 1), Math.Min(miny + 1, Grid.Height - 1));

        return Vector2.Lerp(
            Vector2.Lerp(GetDirection(minx, miny), GetDirection(maxx, miny), fx),
            Vector2.Lerp(GetDirection(minx, maxy), GetDirection(maxx, maxy), fx),
            fy);
    }

    private Vector2 GetDirection(int x, int y)
    {
        return FlowField.GetDirection(x + y * Grid.Width);
    }
}

public struct PathGridGraph : IPathGraph2
{
    private const float WallCost = 999999f;
    private const float DiagonalCost = 1.414213562373095f;

    private static readonly (int dx, int dy)[] s_directions = new[]
    {
        (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, 1),
    };

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

    public Vector2 GetPosition(int nodeIndex)
    {
        var y = Math.DivRem(nodeIndex, _grid.Width, out var x);
        return new((x + 0.5f) * _grid.Step, (y + 0.5f) * _grid.Step);
    }

    public int GetNodeIndex(Vector2 position)
    {
        var x = Math.Min(_grid.Width - 1, Math.Max(0, (int)(position.X / _grid.Step)));
        var y = Math.Min(_grid.Height - 1, Math.Max(0, (int)(position.Y / _grid.Step)));

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

            if (IsOutOfBounds(xx, yy))
                continue;

            var isWall = IsWallNoBoundsCheck(xx, yy);

            edges[count++] = (xx + yy * _grid.Width, isWall ? WallCost : 1);
        }

        // Diagonal edges
        for (var i = 0; i < 4; i++)
        {
            var (e1, e2) = (i, (i + 1) % 4);
            var (dx1, dy1) = s_steps[e1];
            var (dx2, dy2) = s_steps[e2];
            var (xx, yy) = (x + dx1 + dx2, y + dy1 + dy2);

            if (IsOutOfBounds(xx, yy))
                continue;

            var isWall = IsWallNoBoundsCheck(xx, yy) || IsWall(x + dx1, y + dy1) || IsWall(x + dx2, y + dy2);

            edges[count++] = (xx + yy * _grid.Width, isWall ? WallCost : DiagonalCost);
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

            if (IsOutOfBounds(xx, yy))
                continue;

            dx *= (m * (_size - 1) + 1);
            dy *= (m * (_size - 1) + 1);

            var isWall = IsWall(x + dx, y + dy, lx, ly, _size);

            edges[count++] = (xx + yy * _grid.Width, isWall ? WallCost : 1);
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

            if (IsOutOfBounds(xx, yy))
                continue;

            dx1 *= (m1 * (_size - 1) + 1);
            dy1 *= (m1 * (_size - 1) + 1);
            dx2 *= (m2 * (_size - 1) + 1);
            dy2 *= (m2 * (_size - 1) + 1);

            var isWall = IsWall(x + dx1 + dx2, y + dy1 + dy2) ||
                IsWall(x + dx1, y + dy1, lx1, ly1, _size) ||
                IsWall(x + dx2, y + dy2, lx2, ly2, _size);

            edges[count++] = (xx + yy * _grid.Width, isWall ? WallCost : DiagonalCost);
        }

        return count;
    }

    public bool CanLineTo(int nodeIndex, Vector2 target)
    {
        var y = Math.DivRem(nodeIndex, _grid.Width, out var x);
        foreach (var (dx, dy) in s_directions)
            if (IsWall(x + dx, y + dy))
                return false;
        return true;
    }

    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x >= _grid.Width || y < 0 || y >= _grid.Height;
    }

    private bool IsWallNoBoundsCheck(int x, int y)
    {
        return _grid.Bits[x + y * _grid.Width];
    }

    private bool IsWall(int x, int y)
    {
        if (IsOutOfBounds(x, y))
            return true;

        return _grid.Bits[x + y * _grid.Width];
    }

    private bool IsWall(int x, int y, int dx, int dy, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (IsWall(x, y))
                return true;
            x += dx;
            y += dy;
        }
        return false;
    }
}
