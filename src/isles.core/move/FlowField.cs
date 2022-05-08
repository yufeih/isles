// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public interface IFlowField
{
    IPathGraph2 Graph { get; }

    Vector2 GetDirection(int nodeIndex);

    Vector2 GetDirection(Vector2 position);
}

public interface IPathGraph2
{
    int NodeCount { get; }

    int MaxEdgeCount { get; }

    int GetEdges(int from, Span<(int to, float cost)> edges);

    Vector2 GetPosition(int i);

    int GetNodeIndex(Vector2 position);
}

public readonly struct FlowField<T> : IFlowField where T : IPathGraph2
{
    private readonly T _graph;
    private readonly (Half, Half)[] _vectors;

    public IPathGraph2 Graph => _graph;

    public FlowField(T graph, (Half, Half)[] vectors)
    {
        _graph = graph;
        _vectors = vectors;
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

    public static FlowField<T> Create(T graph, int nodeIndex)
    {
        var nodeCount = graph.NodeCount;
        var distance = ArrayPool<float>.Shared.Rent(nodeCount);
        var prev = ArrayPool<ushort>.Shared.Rent(nodeCount);
        var vectors = new (Half, Half)[nodeCount];

        Array.Fill(prev, ushort.MaxValue, 0, nodeCount);
        Array.Fill(distance, float.MaxValue, 0, nodeCount);

        Span<(int, float)> edges = stackalloc (int, float)[graph.MaxEdgeCount];

        var heap = new PriorityQueue();
        heap.Fill(nodeCount, int.MaxValue);

        distance[nodeIndex] = 0;
        heap.UpdatePriority(nodeIndex, 0);

        while (heap.TryDequeue(out var from, out var cost))
        {
            var edgeCount = graph.GetEdges(from, edges);
            foreach (var (to, dcost) in edges.Slice(0, edgeCount))
            {
                var newCost = cost + dcost;
                if (newCost < distance[to])
                {
                    distance[to] = newCost;
                    prev[to] = (ushort)from;
                    heap.UpdatePriority(to, newCost);

                    var v = Vector2.Normalize(graph.GetPosition(from) - graph.GetPosition(to));
                    vectors[to] = ((Half)v.X, (Half)v.Y);
                }
            }
        }

        ArrayPool<float>.Shared.Return(distance);
        ArrayPool<ushort>.Shared.Return(prev);

        return new(graph, vectors);
    }
}
