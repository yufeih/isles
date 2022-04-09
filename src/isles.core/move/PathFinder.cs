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
    private readonly AStarSearch _search = new();
    private ArrayBuilder<Vector2> _findPath;
    private ArrayBuilder<int> _cleanupStraightLine;
    private ArrayBuilder<int> _cleanupLineOfSight;

    public ReadOnlySpan<Vector2> FindPath(PathGrid grid, float pathWidth, Vector2 start, Vector2 end)
    {
        var size = (int)MathF.Ceiling(pathWidth / grid.Step);
        var path = _search.Search(new PathGridGraph(grid, size), grid.GetIndex(start), grid.GetIndex(end));
        if (path.Length <= 1)
        {
            return Array.Empty<Vector2>();
        }

        path = CleanupStraightLine(path);
        path = CleanupLineOfSight(grid, path, size);

        return _findPath.ConvertAll<int>(path, i => grid.GetPosition(i));
    }

    private ReadOnlySpan<int> CleanupStraightLine(ReadOnlySpan<int> path)
    {
        _cleanupStraightLine.Clear();
        _cleanupStraightLine.Add(path[0]);
        var direction = Math.Abs(path[1] - path[0]) == 1;
        for (var i = 1; i < path.Length - 1; i++)
        {
            var currentDirection = Math.Abs(path[i + 1] - path[i]) == 1;
            if (direction != currentDirection)
            {
                direction = currentDirection;
                _cleanupStraightLine.Add(path[i]);
            }
        }
        _cleanupStraightLine.Add(path[^1]);
        return _cleanupStraightLine;
    }

    private ReadOnlySpan<int> CleanupLineOfSight(PathGrid grid, ReadOnlySpan<int> path, int size)
    {
        _cleanupLineOfSight.Clear();
        _cleanupLineOfSight.Add(path[0]);

        var i = 0;
        while (i < path.Length - 1)
        {
            // Binary merge waypoints that has line of sight
            var begin = i + 1;
            var end = path.Length;

            while (true)
            {
                var mid = (begin + end) / 2;
                if (mid == begin)
                {
                    _cleanupLineOfSight.Add(path[i = begin]);
                    break;
                }

                var y0 = Math.DivRem(path[i], grid.Width, out var x0);
                var y1 = Math.DivRem(path[mid], grid.Width, out var x1);

                if (LineOfSightTest(grid, size, x0, y0, x1, y1))
                {
                    begin = mid;
                }
                else
                {
                    end = mid;
                }
            }
        }

        return _cleanupLineOfSight;
    }

    public static bool LineOfSightTest(PathGrid grid, float pathWidth, Vector2 start, Vector2 end)
    {
        var size = (int)MathF.Ceiling(pathWidth / grid.Step);
        var x0 = (int)(start.X / grid.Step);
        var y0 = (int)(start.Y / grid.Step);
        var x1 = (int)(end.X / grid.Step);
        var y1 = (int)(end.Y / grid.Step);

        return LineOfSightTest(grid, size, x0, y0, x1, y1);
    }

    private static bool LineOfSightTest(PathGrid grid, int size, int x0, int y0, int x1, int y1)
    {
        return size == 1 ? LineOfSightTest1(grid, x0, y0, x1, y1) : LineOfSightTestN(grid, size, x0, y0, x1, y1);
    }

    private static bool LineOfSightTest1(PathGrid grid, int x0, int y0, int x1, int y1)
    {
        return !BresenhamLine(x0, y0, x1, y1, GridHitTest, grid);
    }

    private static bool LineOfSightTestN(PathGrid grid, int size, int x0, int y0, int x1, int y1)
    {
        var half = (size - 1) / 2;
        var w = Math.Abs(x1 - x0);
        var h = Math.Abs(y1 - y0);

        if (w > h)
        {
            y0 -= half;
            y1 -= half;
            for (var k = 0; k < size; k++)
            {
                if (y0 < 0 || y0 >= grid.Height || y1 < 0 || y1 > grid.Height)
                {
                    return false;
                }

                if (BresenhamLine(x0, y0, x1, y1, GridHitTest, grid))
                {
                    return false;
                }

                y0++;
                y1++;
            }
        }
        else
        {
            x0 -= half;
            y0 -= half;
            for (var k = 0; k < size; k++)
            {
                if (x0 < 0 || x0 >= grid.Width || x1 < 0 || x1 >= grid.Width)
                {
                    return false;
                }

                if (BresenhamLine(x0, y0, x1, y1, GridHitTest, grid))
                {
                    return false;
                }

                x0++;
                x1++;
            }
        }

        return true;
    }

    private static bool GridHitTest(int x, int y, PathGrid grid)
    {
        return grid.Bits[x + y * grid.Width];
    }

    private static bool BresenhamLine<T>(
        int x0, int y0, int x1, int y1, Func<int, int, T, bool> setPixel, T state)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = (dx > dy ? dx : -dy) / 2;

        while (true)
        {
            if (setPixel(x0, y0, state))
            {
                return true;
            }

            if (x0 == x1 && y0 == y1)
            {
                return false;
            }

            var err = error;
            if (err > -dx)
            {
                error -= dy;
                x0 += sx;
            }
            if (err < dy)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    record PathGridGraph(PathGrid grid, int size) : IPathGraph
    {
        private static readonly (int dx, int dy)[] s_edges = new[]
        {
            (1, 0), (0, 1), (-1, 0), (0, -1),
        };

        private ArrayBuilder<(int to, float cost)> _edges;

        public int NodeCount => grid.Width * grid.Height;

        public ReadOnlySpan<(int to, float cost)> GetEdges(int from)
        {
            return size == 1 ? GetEdges1(from) : GetEdgesN(from);
        }

        public ReadOnlySpan<(int to, float cost)> GetEdges1(int from)
        {
            _edges.Clear();

            var y = Math.DivRem(from, grid.Width, out var x);

            foreach (var (dx, dy) in s_edges)
            {
                var xx = x + dx;
                var yy = y + dy;

                if (xx < 0 || xx >= grid.Width || yy < 0 || yy >= grid.Height)
                {
                    continue;
                }

                var i = xx + yy * grid.Width;
                if (!grid.Bits[i])
                {
                    _edges.Add((i, grid.Step));
                }
            }

            return _edges;
        }

        public ReadOnlySpan<(int to, float cost)> GetEdgesN(int from)
        {
            _edges.Clear();

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
                    _edges.Add((i, grid.Step));
                }
            }

            return _edges;
        }

        public float GetHeuristicValue(int from, int to)
        {
            return (grid.GetPosition(to) - grid.GetPosition(from)).Length();
        }
    }
}
