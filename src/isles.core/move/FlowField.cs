// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public interface IPathGraph2
{
    int NodeCount { get; }

    int MaxEdgeCount { get; }

    int GetEdges(int from, Span<(int to, float cost)> edges);

    Vector2 GetPosition(int nodeInde);

    int GetNodeIndex(Vector2 position);

    bool IsTurnPoint(int nodeIndex, Vector2 target);
}

public readonly struct FlowFieldVector
{
    private readonly Half _x;
    private readonly Half _y;
    private readonly short _next;

    public readonly float X => (float)_x;
    public readonly float Y => (float)_y;
    public readonly short Next => _next switch
    {
        -1 => -1,
        < -1 => (short)(-_next - 1),
        >= 0 => _next,
    };

    public readonly bool IsTurnPoint => _next < 0;

    public FlowFieldVector(float x, float y, short next, bool isTurnPoint)
    {
        _x = (Half)x;
        _y = (Half)y;
        _next = next == -1 ? (short)-1 : (isTurnPoint ? (short)-(next + 1) : next);
    }
}

public readonly struct FlowField
{
    public readonly FlowFieldVector[] Vectors { get; init; }

    public static FlowField Create<T>(T graph, Vector2 target) where T: IPathGraph2
    {
        var nodeCount = graph.NodeCount;
        var targetIndex = graph.GetNodeIndex(target);
        var distance = ArrayPool<float>.Shared.Rent(nodeCount);
        var prev = ArrayPool<short>.Shared.Rent(nodeCount);
        var vectors = new FlowFieldVector[nodeCount];

        Array.Fill(prev, (short)-1, 0, nodeCount);
        Array.Fill(distance, float.MaxValue, 0, nodeCount);

        Span<(int, float)> edges = stackalloc (int, float)[graph.MaxEdgeCount];

        var queue = new PriorityQueue<int, float>(nodeCount);
        distance[targetIndex] = 0;
        queue.Enqueue(targetIndex, 0);

        while (queue.TryDequeue(out var from, out var cost))
        {
            if (vectors[from].X != default || vectors[from].Y != default)
                continue;

            if (cost == float.PositiveInfinity)
                continue;

            EmitNode(from);

            var edgeCount = graph.GetEdges(from, edges);
            foreach (var (to, dcost) in edges.Slice(0, edgeCount))
            {
                var newCost = cost + dcost;
                if (newCost < distance[to])
                {
                    distance[to] = newCost;
                    prev[to] = (short)from;
                    queue.Enqueue(to, newCost);
                }
            }
        }

        ArrayPool<float>.Shared.Return(distance);
        ArrayPool<short>.Shared.Return(prev);

        return new() { Vectors = vectors };

        void EmitNode(int from)
        {
            var next = prev[from];
            var to = from;
            while (prev[to] >= 0)
                to = prev[to];

            var isTurnPoint = false;
            var toPosition = to == targetIndex ? target : graph.GetPosition(to);
            var flow = toPosition - graph.GetPosition(from);
            if (graph.IsTurnPoint(from, toPosition))
            {
                isTurnPoint = true;
                prev[from] = -1;
            }

            vectors[from] = new(flow.X, flow.Y, next, isTurnPoint);
        }
    }
}
