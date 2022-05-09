// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public interface IFlowField
{
    Vector2 Target { get; }

    IPathGraph2 Graph { get; }

    Vector2 GetDirection(int nodeIndex);

    Vector2 GetDirection(Vector2 position);
}

public interface IPathGraph2
{
    int NodeCount { get; }

    int MaxEdgeCount { get; }

    int GetEdges(int from, Span<(int to, float cost)> edges);

    Vector2 GetPosition(int nodeInde);

    int GetNodeIndex(Vector2 position);

    bool CanLineTo(int nodeIndex, Vector2 target);
}

public readonly struct FlowField<T> : IFlowField where T : IPathGraph2
{
    private readonly T _graph;
    private readonly (Half, Half)[] _vectors;

    public Vector2 Target { get; }
    public IPathGraph2 Graph => _graph;

    public FlowField(T graph, Vector2 target, (Half, Half)[] vectors)
    {
        _graph = graph;
        _vectors = vectors;
        Target = target;
    }

    public Vector2 GetDirection(int nodeIndex)
    {
        var (x, y) = _vectors[nodeIndex];
        return new((float)x, (float)y);
    }

    public Vector2 GetDirection(Vector2 position)
    {
        return GetDirection(_graph.GetNodeIndex(position));
    }

    public static FlowField<T> Create(T graph, Vector2 target)
    {
        var nodeCount = graph.NodeCount;
        var targetIndex = graph.GetNodeIndex(target);
        var distance = ArrayPool<float>.Shared.Rent(nodeCount);
        var prev = ArrayPool<ushort>.Shared.Rent(nodeCount);
        var vectors = new (Half, Half)[nodeCount];

        Array.Fill(prev, ushort.MaxValue, 0, nodeCount);
        Array.Fill(distance, float.MaxValue, 0, nodeCount);

        Span<(int, float)> edges = stackalloc (int, float)[graph.MaxEdgeCount];

        var heap = new PriorityQueue();
        heap.Fill(nodeCount, float.PositiveInfinity);

        distance[targetIndex] = 0;
        heap.UpdatePriority(targetIndex, 0);

        while (heap.TryDequeue(out var from, out var cost))
        {
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
                    prev[to] = (ushort)from;
                    heap.UpdatePriority(to, newCost);
                }
            }
        }

        ArrayPool<float>.Shared.Return(distance);
        ArrayPool<ushort>.Shared.Return(prev);

        return new(graph, target, vectors);

        void EmitNode(int from)
        {
            var to = from;
            while (prev[to] != ushort.MaxValue)
                to = prev[to];

            if (from == to)
                return;

            var toPosition = to == targetIndex ? target : graph.GetPosition(to);
            if (!graph.CanLineTo(from, toPosition))
            {
                toPosition = graph.GetPosition(prev[from]);
                prev[from] = ushort.MaxValue;
            }

            var flow = Vector2.Normalize(toPosition - graph.GetPosition(from));
            vectors[from] = ((Half)flow.X, (Half)flow.Y);
        }
    }
}
