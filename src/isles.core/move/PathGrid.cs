// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Isles;

public record PathGrid(int Width, int Height, float Step, BitArray Bits)
{
    public int GetNodeIndex(Vector2 position)
    {
        var x = Math.Min(Width - 1, Math.Max(0, (int)(position.X / Step)));
        var y = Math.Min(Height - 1, Math.Max(0, (int)(position.Y / Step)));

        return y * Width + x;
    }
}

public record PathGridFlowField(Vector2 Target, PathGrid Grid, FlowField FlowField)
{
    internal int TargetIslandId;

    public Vector2 GetVector(Vector2 position)
    {
        var x = position.X / Grid.Step - 0.5f;
        var y = position.Y / Grid.Step - 0.5f;
        x = MathHelper.Clamp(x, 0, Grid.Width);
        y = MathHelper.Clamp(y, 0, Grid.Height);

        var (fx, fy) = (x % 1, y % 1);
        var (minx, miny) = ((int)x, (int)y);
        var (maxx, maxy) = (Math.Min(minx + 1, Grid.Width - 1), Math.Min(miny + 1, Grid.Height - 1));

        var a = Get(minx, miny);
        var b = Get(maxx, miny);
        var c = Get(minx, maxy);
        var d = Get(maxx, maxy);

        return Vector2.Lerp(Vector2.Lerp(a, b, fx), Vector2.Lerp(c, d, fx), fy);

        Vector2 Get(int x, int y)
        {
            var i = x + y * Grid.Width;
            ref readonly var v = ref FlowField.Vectors[i];
            return new(v.X, v.Y);
        }
    }
}

public struct PathGridGraph : IPathGraph2
{
    private const float DiagonalCost = 1.414213562373095f;

    private static readonly (int dx, int dy)[] Steps = new[]
    {
        (0, -1), (1, 0), (0, 1), (-1, 0)
    };

    private static readonly (int multiplier, int lineX, int lineY)[] Edges = new[]
    {
        (0, 1, 0), (1, 0, 1), (1, 1, 0), (0, 0, 1),
    };

    private static readonly (int dx, int dy, float min, float max)[] TurnPoints = new[]
    {
        (1, 1, MathF.PI / 2, 0),
        (1, -1, 0, -MathF.PI / 2),
        (-1, -1, -MathF.PI / 2, MathF.PI),
        (-1, 1, MathF.PI, MathF.PI / 2),
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
            var (dx, dy) = Steps[i];
            var (xx, yy) = (x + dx, y + dy);

            if (IsWall(xx, yy))
                continue;

            edges[count++] = (xx + yy * _grid.Width, 1);
        }

        // Diagonal edges
        for (var i = 0; i < 4; i++)
        {
            var (e1, e2) = (i, (i + 1) % 4);
            var (dx1, dy1) = Steps[e1];
            var (dx2, dy2) = Steps[e2];
            var (xx, yy) = (x + dx1 + dx2, y + dy1 + dy2);

            if (IsWall(xx, yy) || IsWall(x + dx1, y + dy1) || IsWall(x + dx2, y + dy2))
                continue;

            edges[count++] = (xx + yy * _grid.Width, DiagonalCost);
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
            var (m, lx, ly) = Edges[i];
            var (dx, dy) = Steps[i];
            var (xx, yy) = (x + dx, y + dy);

            dx *= (m * (_size - 1) + 1);
            dy *= (m * (_size - 1) + 1);

            if (IsWall(x + dx, y + dy, lx, ly, _size))
                continue;

            edges[count++] = (xx + yy * _grid.Width, 1);
        }

        // Diagonal edges
        for (var i = 0; i < 4; i++)
        {
            var (e1, e2) = (i, (i + 1) % 4);
            var (m1, lx1, ly1) = Edges[e1];
            var (m2, lx2, ly2) = Edges[e2];
            var (dx1, dy1) = Steps[e1];
            var (dx2, dy2) = Steps[e2];
            var (xx, yy) = (x + dx1 + dx2, y + dy1 + dy2);

            if (IsWall(xx, yy))
                continue;

            dx1 *= (m1 * (_size - 1) + 1);
            dy1 *= (m1 * (_size - 1) + 1);
            dx2 *= (m2 * (_size - 1) + 1);
            dy2 *= (m2 * (_size - 1) + 1);

            if (IsWall(x + dx1 + dx2, y + dy1 + dy2) ||
                IsWall(x + dx1, y + dy1, lx1, ly1, _size) ||
                IsWall(x + dx2, y + dy2, lx2, ly2, _size))
                continue;

            edges[count++] = (xx + yy * _grid.Width, DiagonalCost);
        }

        return count;
    }

    public bool IsTurnPoint(int nodeIndex, Vector2 target)
    {
        var r = float.NaN;
        var y = Math.DivRem(nodeIndex, _grid.Width, out var x);

        foreach (var (dx, dy, min, max) in TurnPoints)
        {
            if (!IsWall(x + dx, y + dy) || IsWall(x + dx, y) || IsWall(x, y + dy))
                continue;

            if (float.IsNaN(r))
            {
                var v = target - GetPosition(nodeIndex);
                r = MathF.Atan2(v.Y, v.X);
            }

            if (MathFHelper.NormalizeRotation(r - min) < 0 || MathFHelper.NormalizeRotation(r - max) > 0)
                return true;
        }
        return false;
    }

    private bool IsWall(int x, int y)
    {
        if (x < 0 || x >= _grid.Width || y < 0 || y >= _grid.Height)
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
