// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public record FlowField(int Width, int Height, float Step, ushort[] Next)
{
    private const float H = 0.707106781186548f;

    public Vector2 GetDirection(int i)
    {
        var y = Math.DivRem(i, Width, out var x);
        if (Next[i] == ushort.MaxValue)
            return default;

        var yy = Math.DivRem(Next[i], Width, out var xx);
        return Vector2.Normalize(new(xx - x, yy - y));
    }

    public Vector2 GetDirection(Vector2 position)
    {
        var x = (int)(position.X / Step);
        var y = (int)(position.Y / Step);

        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return default;

        return GetDirection(x + y * Width);
    }
}

public class FlowFieldSearch
{
    private const float D = 1.414213562373095f;

    private static readonly (int dx, int dy, float cost)[] s_edges = new[]
    {
        (0, -1, 1), (1, -1, D), (1, 0, 1), (1, 1, D),
        (0, 1, 1), (-1, 1, D), (-1, 0, 1), (-1, -1, D),
    };

    private readonly PriorityQueue _heap = new();
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

        var nodeCount = grid.Width * grid.Height;
        var distance = ArrayPool<float>.Shared.Rent(nodeCount);
        var next = new ushort[nodeCount];
        Array.Fill(next, ushort.MaxValue, 0, nodeCount);
        Array.Fill(distance, float.MaxValue, 0, nodeCount);
        _heap.Fill(nodeCount, int.MaxValue);

        distance[index] = 0;
        _heap.UpdatePriority(index, 0);

        while (_heap.TryDequeue(out var top, out var cost))
        {
            var cy = Math.DivRem(top, grid.Width, out var cx);

            foreach (var (dx, dy, dcost) in s_edges)
            {
                var (tx, ty) = (cx + dx, cy + dy);
                if (tx >= 0 && tx < grid.Width && ty >= 0 && ty < grid.Height)
                {
                    var targetIndex = tx + ty * grid.Width;
                    if (!grid.Bits[targetIndex])
                    {
                        var newCost = cost + dcost;
                        if (newCost < distance[targetIndex])
                        {
                            distance[targetIndex] = newCost;
                            next[targetIndex] = (ushort)top;
                            _heap.UpdatePriority(targetIndex, newCost);
                        }
                    }
                }
            }
        }

        ArrayPool<float>.Shared.Return(distance);

        return new(grid.Width, grid.Height, grid.Step, next);
    }
}
