// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public enum FlowFieldDirection : byte { N, NE, E, SE, S, SW, W, NW }

public record FlowField(int Width, int Height, float Step, FlowFieldDirection[] Directions)
{
    private const float H = 0.707106781186548f;

    public Vector2 GetDirection(Vector2 position)
    {
        var x = (int)(position.X / Step);
        var y = (int)(position.Y / Step);

        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return default;

        switch (Directions[(x + y * Width)])
        {
            case FlowFieldDirection.N: return new(0, -1);
            case FlowFieldDirection.NE: return new(H, -H);
            case FlowFieldDirection.E: return new(1, 0);
            case FlowFieldDirection.SE: return new(H, H);
            case FlowFieldDirection.S: return new(0, 1);
            case FlowFieldDirection.SW: return new(-H, H);
            case FlowFieldDirection.W: return new(-1, 0);
            case FlowFieldDirection.NW: return new(-H, -H);
            default: return default;
        }
    }
}

public class FlowFieldSearch
{
    private static readonly (int dx, int dy)[] s_edges4 = new[]
    {
        (1, 0), (0, 1), (-1, 0), (0, -1),
    };

    private static readonly (int dx, int dy)[] s_edges8 = new[]
    {
        (0, -1), (1, -1), (1, 0), (1, 1),
        (0, 1), (-1, 1), (-1, 0), (-1, -1),
    };

    private readonly Queue<int> _queue = new();
    private readonly Dictionary<int, FlowField?> _flowFields = new();

    public FlowField? GetFlowField(PathGrid grid, Vector2 target)
    {
        var x = Math.Max(0, Math.Min(grid.Width - 1, (int)(target.X / grid.Step)));
        var y = Math.Max(0, Math.Min(grid.Height - 1, (int)(target.Y / grid.Step)));
        var index = x + y * grid.Width;

        if (!_flowFields.TryGetValue(index, out var result))
            result = _flowFields[index] = CreateFlowField(grid, index);

        return result;
    }

    private FlowField? CreateFlowField(PathGrid grid, int index)
    {
        if (grid.Bits[index])
            return null;

        var costs = ArrayPool<ushort>.Shared.Rent(grid.Width * grid.Height);
        Array.Fill(costs, ushort.MaxValue, 0, grid.Width * grid.Height);

        costs[index] = 0;
        _queue.Enqueue(index);

        while (_queue.TryDequeue(out var node))
        {
            var nodeCost = costs[node];
            var cy = Math.DivRem(node, grid.Width, out var cx);

            foreach (var (dx, dy) in s_edges4)
            {
                var (tx, ty) = (cx + dx, cy + dy);
                if (tx >= 0 && tx < grid.Width && ty >= 0 && ty < grid.Height)
                {
                    var targetIndex = tx + ty * grid.Width;
                    var cost = grid.Bits[targetIndex] ? ushort.MaxValue : (ushort)(nodeCost + 1);
                    if (cost < costs[targetIndex])
                        costs[targetIndex] = cost;
                    if (cost != ushort.MaxValue)
                        _queue.Enqueue(targetIndex);
                }
            }
        }

        ArrayPool<ushort>.Shared.Return(costs);

        var directions = new FlowFieldDirection[grid.Width * grid.Height];

        for (var i = 0; i < directions.Length; i++)
        {
            var minCost = ushort.MaxValue;
            var y = Math.DivRem(i, grid.Width, out var x);

            for (var d = 0; d < s_edges8.Length; d++)
            {
                var (tx, ty) = (x + s_edges8[d].dx, y + s_edges8[d].dy);
                if (tx >= 0 && tx < grid.Width && ty >= 0 && ty < grid.Height)
                {
                    var cost = costs[tx + ty * grid.Width];
                    if (cost < minCost)
                    {
                        minCost = cost;
                        directions[i] = (FlowFieldDirection)d;
                    }
                }
            }
        }

        return new(grid.Width, grid.Height, grid.Step, directions);
    }
}
