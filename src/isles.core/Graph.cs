// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public interface IGraph
{
    /// <summary>
    /// Gets the total number of nodes in the graph.
    /// </summary>
    int NodeCount { get; }

    /// <summary>
    /// Gets all the out-going edges of a given node.
    /// </summary>
    IEnumerable<(int to, float cost)> GetEdges(int from);

    /// <summary>
    /// Gets the heuristic value between two nodes used in A* algorithm.
    /// </summary>
    /// <returns>A heuristic value between the two nodes.</returns>
    float GetHeuristicValue(int from, int to);
}

/// <summary>
/// Performs an A* graph search on a given graph.
/// </summary>
public class GraphSearchAStar
{
    private int[] _path = default!;
    private float[] _costs = default!;
    private MinHeap _heap = default!;

    private readonly ArrayBuilder<int> _result = new();

    /// <summary>
    /// Returns a list of path index from end to start.
    /// </summary>
    public ReadOnlySpan<int> Search(IGraph graph, int start, int end)
    {
        var nodeCount = graph.NodeCount;
        if (_path is null || nodeCount > _path.Length)
        {
            EnsureCapacity(nodeCount);
        }

        // Clear costs (path don't need to be cleared)
        Array.Clear(_costs);

        // Add the start node on the queue
        _heap.Clear();
        _heap.Add(start, 0);

        // While the queue is not empty
        while (!_heap.Empty)
        {
            // Get the next node with the lowest cost
            // and removes it from the queue
            var top = _heap.Pop();

            // If we reached the end, everything is done
            if (end == top)
            {
                _result.Clear();
                var i = end;
                while (i != start && i >= 0)
                {
                    _result.Add(i);
                    i = _path[i];
                }
                _result.Add(start);
                return _result.AsSpan();
            }

            // Otherwise test all node adjacent to this one
            foreach (var (to, cost) in graph.GetEdges(top))
            {
                // Calculate the heuristic cost from this node to the target (H)
                var HCost = graph.GetHeuristicValue(to, end);

                // Calculate the 'real' cost to this node from the source (G)
                var GCost = _costs[top] + cost;

                // If the node is discoverted for the first time,
                // Setup it's cost then add it to the priority queue.
                if (_heap.Index[to] < 0)
                {
                    _path[to] = top;
                    _costs[to] = GCost;

                    _heap.Add(to, GCost + HCost);
                }

                // If the node has already been visited, but we have found a
                // new path with a lower cost, then replace the existing path
                // and update the cost.
                else if (_heap.Index[to] > 0 && GCost < _costs[to])
                {
                    _path[to] = top;
                    _costs[to] = GCost;

                    // Reset node cost
                    _heap.IncreasePriority(to, GCost + HCost);
                }
            }
        }

        // Finish the search
        return Array.Empty<int>();
    }

    private void EnsureCapacity(int newLength)
    {
        _path = new int[newLength];
        _costs = new float[newLength];
        _heap = new MinHeap(newLength);

        // Reset path to -1
        for (var i = 0; i < newLength; i++)
        {
            _path[i] = -1;
        }
    }
}
