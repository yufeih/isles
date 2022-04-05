// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public struct GraphEdge
{
    /// <summary>
    /// Gets an index representing where the edge is from.
    /// </summary>
    public int From;

    /// <summary>
    /// Gets an index representing where the edge leads to.
    /// </summary>
    public int To;

    /// <summary>
    /// Gets a non-negtive cost associated to the edge.
    /// </summary>
    public float Cost;
}

public interface IGraph
{
    /// <summary>
    /// Gets the total number of nodes in the graph.
    /// </summary>
    int NodeCount { get; }

    /// <summary>
    /// Gets all the out-going edges of a given node.
    /// </summary>
    IEnumerable<GraphEdge> GetEdges(int nodeIndex);

    /// <summary>
    /// Gets the heuristic value between two nodes used in A* algorithm.
    /// </summary>
    /// <param name="currentIndex">Index to the current node.</param>
    /// <param name="endIndex">Index to the end/target node.</param>
    /// <returns>A heuristic value between the two nodes.</returns>
    float GetHeuristicValue(int currentIndex, int endIndex);
}

/// <summary>
/// Performs an A* graph search on a given graph.
/// </summary>
public class GraphSearchAStar
{
    /// <summary>
    /// Start, end node of the search.
    /// </summary>
    private int start;

    /// <summary>
    /// Start, end node of the search.
    /// </summary>
    private int end;

    /// <summary>
    /// A list holding the path information.
    /// For a given node index, the value at that index is the parent
    /// (or the previous step) index.
    /// </summary>
    private int[] path = default!;

    /// <summary>
    /// Contains the real accumulative cost to that node.
    /// </summary>
    private float[] costs = default!;

    /// <summary>
    /// Current length of path or costs (Node count).
    /// </summary>
    private int length;

    /// <summary>
    /// Create an priority queue to store node indices.
    /// </summary>
    private IndexedPriorityQueue queue = default!;

    /// <summary>
    /// Reset GraphSearch state.
    /// </summary>
    private void Reset(int newLength)
    {
        if (newLength > length)
        {
            length = newLength;

            path = new int[length];
            costs = new float[length];
            queue = new IndexedPriorityQueue(length);

            // Reset path to -1
            for (var i = 0; i < length; i++)
            {
                path[i] = -1;
            }
        }

        // Clear costs (path don't need to be cleared)
        for (var i = 0; i < length; i++)
        {
            costs[i] = 0;
        }

        // Reset the queue
        queue.Clear();
    }

    /// <summary>
    /// Perform a graph search on a graph, find a best path from start to end.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns>Whether a path has been found.</returns>
    public bool Search(IGraph graph, int start, int end)
    {
        var nodeCount = graph.NodeCount;

        this.start = start;
        this.end = end;

        // Validate input
        if (nodeCount <= 0 || start < 0 || start >= nodeCount ||
            end < 0 || end >= nodeCount)
        {
            throw new ArgumentOutOfRangeException();
        }

        // Reset everything
        Reset(nodeCount);

        // Add the start node on the queue
        queue.Add(start, 0);

        // While the queue is not empty
        while (!queue.Empty)
        {
            // Get the next node with the lowest cost
            // and removes it from the queue
            var top = queue.Pop();

            // If we reached the end, everything is done
            if (end == top)
            {
                return true;
            }

            // Otherwise test all node adjacent to this one
            foreach (GraphEdge edge in graph.GetEdges(top))
            {
                // Calculate the heuristic cost from this node to the target (H)
                var HCost = graph.GetHeuristicValue(edge.To, end);

                // Calculate the 'real' cost to this node from the source (G)
                var GCost = costs[top] + edge.Cost;

                // If the node is discoverted for the first time,
                // Setup it's cost then add it to the priority queue.
                if (queue.Index[edge.To] < 0)
                {
                    path[edge.To] = top;
                    costs[edge.To] = GCost;

                    queue.Add(edge.To, GCost + HCost);
                }

                // If the node has already been visited, but we have found a
                // new path with a lower cost, then replace the existing path
                // and update the cost.
                else if (queue.Index[edge.To] > 0 && GCost < costs[edge.To])
                {
                    path[edge.To] = top;
                    costs[edge.To] = GCost;

                    // Reset node cost
                    queue.IncreasePriority(edge.To, GCost + HCost);
                }
            }
        }

        // Finish the search
        return false;
    }

    /// <summary>
    /// Gets the path from search result. The path is an array of index
    /// to all the graph nodes on the path FROM END TO START!!!.
    /// The path is only valid after search is called and completed.
    /// </summary>
    public IEnumerable<int> Path
    {
        get
        {
            var i = end;
            while (i != start && i >= 0)
            {
                yield return i;

                i = path[i];
            }

            // Do not forget to return start
            yield return start;
        }
    }
}
