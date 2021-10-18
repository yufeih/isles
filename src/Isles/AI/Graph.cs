//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Isles.Engine
{
    #region GraphEdge & IGraph
    /// <summary>
    /// Interface for a graph edge
    /// </summary>
    public struct GraphEdge
    {
        /// <summary>
        /// Gets an index representing where the edge is from
        /// </summary>
        public int From;

        /// <summary>
        /// Gets an index representing where the edge leads to
        /// </summary>
        public int To;

        /// <summary>
        /// Gets a non-negtive cost associated to the edge
        /// </summary>
        public float Cost;

        /// <summary>
        /// Creates a graph edge
        /// </summary>
        public GraphEdge(int from, int to, float cost)
        {
            this.From = from;
            this.To = to;
            this.Cost = cost;
        }
    }


    /// <summary>
    /// Interface for a directed graph
    /// </summary>
    public interface IGraph
    {
        /// <summary>
        /// Gets the total number of nodes in the graph
        /// </summary>
        int NodeCount { get; }
        
        /// <summary>
        /// Gets all the out-going edges of a given node
        /// </summary>
        IEnumerable<GraphEdge> GetEdges(int nodeIndex);

        /// <summary>
        /// Gets the heuristic value between two nodes used in A* algorithm
        /// </summary>
        /// <param name="currentIndex">Index to the current node</param>
        /// <param name="endIndex">Index to the end/target node</param>
        /// <returns>A heuristic value between the two nodes</returns>
        float GetHeuristicValue(int currentIndex, int endIndex);
    }
    #endregion
    
    #region SparseGraph
    /// <summary>
    /// Sparse graph using adjacency list representation
    /// </summary>
    public class SparseGraph<TNode> : IGraph
    {
        #region Fields
        /// <summary>
        /// All graph nodes
        /// </summary>
        List<TNode> nodes;

        /// <summary>
        /// Graph edge adjacency list
        /// </summary>
        List<LinkedList<GraphEdge>> edges;
        #endregion

        #region IGraph
        /// <summary>
        /// Gets the total number of nodes in the graph
        /// </summary>
        public int NodeCount
        {
            get { return nodes.Count; } 
        }

        /// <summary>
        /// Gets all nodes
        /// </summary>
        public IEnumerable<TNode> Nodes
        {
            get { return nodes; }
        }

        /// <summary>
        /// Gets a graph node from a given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TNode GetNode(int index)
        {
            return nodes[index];
        }

        /// <summary>
        /// Gets the heuristic value between two nodes
        /// </summary>
        /// <param name="currentIndex">Index to the current node</param>
        /// <param name="endIndex">Index to the end/target node</param>
        /// <returns>A heuristic value between the two nodes</returns>
        public float GetHeuristicValue(int currentIndex, int endIndex)
        {
            return 0;
        }

        /// <summary>
        /// Gets all the out-going edges of a given node
        /// </summary>
        public IEnumerable<GraphEdge> GetEdges(int nodeIndex)
        {
            return edges[nodeIndex];
        }
        #endregion

        #region Construction
        /// <summary>
        /// Creates new directed sparse graph
        /// </summary>
        public SparseGraph()
        {
            nodes = new List<TNode>();
            edges = new List<LinkedList<GraphEdge>>();
        }

        /// <summary>
        /// Creates a new sparse graph with a initial node count
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <param name="directed"></param>
        public SparseGraph(int nodeCount)
        {
            nodes = new List<TNode>(nodeCount);
            edges = new List<LinkedList<GraphEdge>>(nodeCount);

            for (int i = 0; i < nodeCount; i++)
            {
                edges[i] = new LinkedList<GraphEdge>();
            }
        }

        /// <summary>
        /// Adds a new node to the graph
        /// </summary>
        /// <param name="node"></param>
        /// <returns>The index to the new node</returns>
        public int AddNode(TNode node)
        {
            nodes.Add(node);
            edges.Add(new LinkedList<GraphEdge>());

            return nodes.Count - 1;
        }

        /// <summary>
        /// Adds a new edge to the graph
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
        /// Removes a graph node
        /// </summary>
        /// <param name="nodeIndex"></param>
        public void RemoveNode(int nodeIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes a graph edge
        /// </summary>
        public void RemoveEdge(int from, int to)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Test
        /// <summary>
        /// Test cases for graph
        /// </summary>
        public static void Test()
        {
            SparseGraph<int> graph = new SparseGraph<int>();
            GraphSearchAStar search = new GraphSearchAStar();

            graph.AddNode(0);
            graph.AddNode(1);
            graph.AddNode(2);
            graph.AddNode(3);
            graph.AddNode(4);

            graph.AddEdge(new GraphEdge(0, 1, 198));
            graph.AddEdge(new GraphEdge(1, 0, 198));
            graph.AddEdge(new GraphEdge(1, 2, 220));
            graph.AddEdge(new GraphEdge(0, 4, 190));
            graph.AddEdge(new GraphEdge(4, 1, 265));
            graph.AddEdge(new GraphEdge(0, 3, 280));
            graph.AddEdge(new GraphEdge(4, 3, 149));
            graph.AddEdge(new GraphEdge(3, 2, 181));

            search.Search(graph, 0, 2);

            List<int> path = new List<int>();
            path.AddRange(search.Path);
        }
        #endregion
    }
    #endregion

    #region GraphSearch
    /// <summary>
    /// Interface for graph search algorithm
    /// </summary>
    public interface IGraphSearch
    {
        /// <summary>
        /// Perform a graph search on a graph, find a best path from start to end.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>Whether a path has been found</returns>
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
    /// Performs an A* graph search on a given graph
    /// </summary>
    public class GraphSearchAStar : IGraphSearch
    {
        /// <summary>
        /// Whether a search has finished
        /// </summary>
        bool finished = true;

        /// <summary>
        /// Start, end node of the search
        /// </summary>
        int start, end;

        /// <summary>
        /// The graph we're currently searching
        /// </summary>
        IGraph graph;

        /// <summary>
        /// A list holding the path information.
        /// For a given node index, the value at that index is the parent
        /// (or the previous step) index.
        /// </summary>
        int[] path;

        /// <summary>
        /// Contains the real accumulative cost to that node
        /// </summary>
        float[] costs;

        /// <summary>
        /// Current length of path or costs (Node count)
        /// </summary>
        int length;

        /// <summary>
        /// Create an priority queue to store node indices.
        /// </summary>
        IndexedPriorityQueue queue;

        /// <summary>
        /// Gets whether a search query has finished
        /// </summary>
        public bool Finished
        {
            get { return finished; }
        }

        /// <summary>
        /// Creates a graph searcher using Dijkstra's algorithm
        /// </summary>
        public GraphSearchAStar()
        {

        }

        /// <summary>
        /// Reset GraphSearch state
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
                for (int i = 0; i < length; i++)
                    path[i] = -1;
            }

            // Clear costs (path don't need to be cleared)
            for (int i = 0; i < length; i++)
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
        /// <returns>Whether a path has been found</returns>
        public bool Search(IGraph graph, int start, int end)
        {
            int steps;
            // Simple call our step search using infinite steps
            return Search(graph, start, end, int.MaxValue, out steps).Value;
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
            int nodeCount = graph.NodeCount;

            // Start a new search
            if (finished || this.start != start || this.end != end || this.graph != graph)
            {
                this.finished = false;
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
                int top = queue.Pop();

                // If we reached the end, everything is done
                if (end == top)
                {
                    finished = true;
                    return true;
                }

                // Otherwise test all node adjacent to this one
                foreach (GraphEdge edge in graph.GetEdges(top))
                {
                    // Calculate the heuristic cost from this node to the target (H)                       
                    float HCost = graph.GetHeuristicValue(edge.To, end);

                    // Calculate the 'real' cost to this node from the source (G)
                    float GCost = costs[top] + edge.Cost;

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
                    finished = false;
                    return null;
                }
            }

            // Finish the search
            finished = true;
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
                int i = end;
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
    #endregion
}
