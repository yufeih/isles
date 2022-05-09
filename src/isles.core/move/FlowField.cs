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

    bool CanLineTo(int nodeIndex, Vector2 target);
}

public struct FlowField
{
    public (Half, Half)[] Vectors;

    public Vector2 GetDirection(int nodeIndex)
    {
        var (x, y) = Vectors[nodeIndex];
        return new((float)x, (float)y);
    }

    public static FlowField Create<T>(T graph, Vector2 target) where T: IPathGraph2
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

        return new() { Vectors = vectors };

        void EmitNode(int from)
        {
            var to = from;
            while (prev[to] != ushort.MaxValue)
                to = prev[to];

            var toPosition = to == targetIndex ? target : graph.GetPosition(to);
            if (!graph.CanLineTo(from, toPosition))
            {
                toPosition = graph.GetPosition(prev[from]);
                prev[from] = ushort.MaxValue;
            }

            var flow = toPosition - graph.GetPosition(from);
            vectors[from] = ((Half)flow.X, (Half)flow.Y);
        }
    }
}
