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
/// Sparse graph using adjacency list representation.
/// </summary>
public class SparseGraph<TNode> : IGraph
{
    /// <summary>
    /// All graph nodes.
    /// </summary>
    private readonly List<TNode> nodes;

    /// <summary>
    /// Graph edge adjacency list.
    /// </summary>
    private readonly List<LinkedList<GraphEdge>> edges;

    /// <summary>
    /// Gets the total number of nodes in the graph.
    /// </summary>
    public int NodeCount => nodes.Count;

    /// <summary>
    /// Gets all nodes.
    /// </summary>
    public IEnumerable<TNode> Nodes => nodes;

    /// <summary>
    /// Gets a graph node from a given index.
    /// </summary>
    /// <param name="index"></param>
    public TNode GetNode(int index)
    {
        return nodes[index];
    }

    /// <summary>
    /// Gets the heuristic value between two nodes.
    /// </summary>
    /// <param name="currentIndex">Index to the current node.</param>
    /// <param name="endIndex">Index to the end/target node.</param>
    /// <returns>A heuristic value between the two nodes.</returns>
    public float GetHeuristicValue(int currentIndex, int endIndex)
    {
        return 0;
    }

    /// <summary>
    /// Gets all the out-going edges of a given node.
    /// </summary>
    public IEnumerable<GraphEdge> GetEdges(int nodeIndex)
    {
        return edges[nodeIndex];
    }

    /// <summary>
    /// Creates new directed sparse graph.
    /// </summary>
    public SparseGraph()
    {
        nodes = new List<TNode>();
        edges = new List<LinkedList<GraphEdge>>();
    }

    /// <summary>
    /// Creates a new sparse graph with a initial node count.
    /// </summary>
    /// <param name="nodeCount"></param>
    /// <param name="directed"></param>
    public SparseGraph(int nodeCount)
    {
        nodes = new List<TNode>(nodeCount);
        edges = new List<LinkedList<GraphEdge>>(nodeCount);

        for (var i = 0; i < nodeCount; i++)
        {
            edges[i] = new LinkedList<GraphEdge>();
        }
    }

    /// <summary>
    /// Adds a new node to the graph.
    /// </summary>
    /// <param name="node"></param>
    /// <returns>The index to the new node.</returns>
    public int AddNode(TNode node)
    {
        nodes.Add(node);
        edges.Add(new LinkedList<GraphEdge>());

        return nodes.Count - 1;
    }

    /// <summary>
    /// Adds a new edge to the graph.
    /// </summary>
    /// <param name="edge"></param>
    public void AddEdge(GraphEdge edge)
    {
        // Validate edge nodes
        if (edge.From < 0 || edge.From >= nodes.Count ||
            edge.To < 0 || edge.To >= nodes.Count)
        {
            throw new ArgumentOutOfRangeException();
        }

        edges[edge.From].AddFirst(edge);
    }

    /// <summary>
    /// Removes a graph node.
    /// </summary>
    /// <param name="nodeIndex"></param>
    public void RemoveNode(int nodeIndex)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Removes a graph edge.
    /// </summary>
    public void RemoveEdge(int from, int to)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Interface for graph search algorithm.
/// </summary>
public interface IGraphSearch
{
    /// <summary>
    /// Perform a graph search on a graph, find a best path from start to end.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns>Whether a path has been found.</returns>
    bool Search(IGraph graph, int start, int end);

    /// <summary>
    /// Perform a graph search on a graph, find a best path from start to end.
    /// This algorithm complete the graph search in multiple calls instead of one,
    /// the maximum number of graph nodes traversed in each call is specified by
    /// the steps parameter. The algorithm aims at decomposing the time consuming
    /// graph search algorithm into several steps in order to average the time cost.
    /// When the search is incomplete, if a different graph, start or end point is
    /// specified, a new search will be triggered.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="steps">
    /// Max number of nodes to be searched in this call.
    /// </param>
    /// <param name="stepsUsed">
    /// Actrual number of nodes searched during this call.
    /// </param>
    /// <returns>
    /// True or false indicates whether a path has been found
    /// Null if the search isn't complete.
    /// </returns>
    bool? Search(IGraph graph, int start, int end, int steps, out int stepsUsed);

    /// <summary>
    /// Gets the path from search result. The path is an array of index
    /// to all the graph nodes on the path from start to end.
    /// The path is valid only after search is called and completed.
    /// </summary>
    IEnumerable<int> Path { get; }
}

/// <summary>
/// Performs an A* graph search on a given graph.
/// </summary>
public class GraphSearchAStar : IGraphSearch
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
    /// The graph we're currently searching.
    /// </summary>
    private IGraph graph;

    /// <summary>
    /// A list holding the path information.
    /// For a given node index, the value at that index is the parent
    /// (or the previous step) index.
    /// </summary>
    private int[] path;

    /// <summary>
    /// Contains the real accumulative cost to that node.
    /// </summary>
    private float[] costs;

    /// <summary>
    /// Current length of path or costs (Node count).
    /// </summary>
    private int length;

    /// <summary>
    /// Create an priority queue to store node indices.
    /// </summary>
    private IndexedPriorityQueue queue;

    /// <summary>
    /// Gets whether a search query has finished.
    /// </summary>
    public bool Finished { get; private set; } = true;

    /// <summary>
    /// Creates a graph searcher using Dijkstra's algorithm.
    /// </summary>
    public GraphSearchAStar()
    {
    }

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
        // Simple call our step search using infinite steps
        return Search(graph, start, end, int.MaxValue, out _).Value;
    }

    /// <summary>
    /// Perform a graph search on a graph, find a best path from start to end.
    /// This algorithm complete the graph search in multiple calls instead of one,
    /// the maximum number of graph nodes traversed in each call is specified by
    /// the steps parameter. The algorithm aims at decomposing the time consuming
    /// graph search algorithm into several steps in order to average the time cost.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="steps"></param>
    /// <returns>
    /// True or false indicates whether a path has been found
    /// Null if the search isn't complete.
    /// </returns>
    public bool? Search(IGraph graph, int start, int end, int steps, out int stepCount)
    {
        var nodeCount = graph.NodeCount;

        // Start a new search
        if (Finished || this.start != start || this.end != end || this.graph != graph)
        {
            Finished = false;
            this.graph = graph;
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
        }

        // Step count
        stepCount = 0;

        // While the queue is not empty
        while (!queue.Empty)
        {
            // Get the next node with the lowest cost
            // and removes it from the queue
            var top = queue.Pop();

            // If we reached the end, everything is done
            if (end == top)
            {
                Finished = true;
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

            // If we have processed enough nodes but still failed to reach the target,
            // pause the search and return null.
            if (++stepCount >= steps)
            {
                Finished = false;
                return null;
            }
        }

        // Finish the search
        Finished = true;
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
