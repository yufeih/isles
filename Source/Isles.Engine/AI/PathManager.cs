using System;
using System.Collections.Generic;
using System.Text;
using Isles.Graphics;
using Microsoft.Xna.Framework;

namespace Isles.AI
{
    /// <summary>
    /// Path manager to provide path finding service
    /// for all game agents.
    /// </summary>
    public class PathManager
    {
        /// <summary>
        /// Current landscape
        /// </summary>
        Landscape landscape;

        /// <summary>
        /// Graph to be search
        /// </summary>
        LandscapeGraph graph;

        /// <summary>
        /// Graph searcher
        /// </summary>
        GraphSearchAStar<GraphEdge> search;

        /// <summary>
        /// Graph searcher
        /// </summary>
        GraphSearchAStar<GraphEdge> searchImmediate;

        /// <summary>
        /// Max search steps per update.
        /// Used to limit the CPU usage for path finding service
        /// </summary>
        int maxSearchStepsPerUpdate = 2000;

        /// <summary>
        /// Pending query requests. Set initial capacity to 8.
        /// </summary>
        PriorityQueue<PathQuery> pendingRequests = new PriorityQueue<PathQuery>(8);

        /// <summary>
        /// Internal representation for a given path query
        /// </summary>
        private struct PathQuery : IComparable<PathQuery>
        {
            /// <summary>
            /// Start, end node
            /// </summary>
            public int Start, End;

            /// <summary>
            /// Priority of this query
            /// </summary>
            public float Priority;

            /// <summary>
            /// Handler for this query
            /// </summary>
            public OnPathFound PathFound;

            /// <summary>
            /// Compare function, used in the priority queue.
            /// </summary>
            /// <remarks>
            /// Priority queue is a MIN queue, but we want the query
            /// with the highest priority to be poped first.
            /// </remarks>
            public int CompareTo(PathQuery query)
            {
                return (int)(query.Priority - Priority);
            }

            /// <summary>
            /// For the ease of constructing a path query
            /// </summary>
            public PathQuery(int start, int end, float priority, OnPathFound pathFound)
            {
                this.Start = start;
                this.End = end;
                this.Priority = priority;
                this.PathFound = pathFound;
            }
        }

        /// <summary>
        /// Gets path graph
        /// </summary>
        public LandscapeGraph Graph
        {
            get { return graph; }
        }

        /// <summary>
        /// Gets or sets the maximun number of search steps per update
        /// </summary>
        public int MaxSearchStepsPerUpdate
        {
            get { return maxSearchStepsPerUpdate; }

            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException();

                maxSearchStepsPerUpdate = value;
            }
        }

        /// <summary>
        /// Create a new path manager on a given landscape
        /// </summary>
        /// <param name="landscape"></param>
        public PathManager(Landscape landscape)
        {
            this.landscape = landscape;

            // Create a new graph
            this.graph = new LandscapeGraph(landscape);

            // Create a new graph searcher
            this.search = new GraphSearchAStar<GraphEdge>();
            this.searchImmediate = new GraphSearchAStar<GraphEdge>();
        }

        /// <summary>
        /// Called when the path is found for a query
        /// </summary>
        /// <param name="path">The result path, of null if no path is found</param>
        public delegate void OnPathFound(IEnumerable<int> path);

        /// <summary>
        /// Query a path asychroniously from start node to end node.
        /// </summary>
        /// <param name="start">Start node index in the graph</param>
        /// <param name="end">End node index in the graph</param>
        /// <param name="priority">Priority for this query</param>
        /// <param name="onPathFound">Delegation to handle path found event</param>
        public void Query(int start, int end, float priority, OnPathFound onPathFound)
        {
            // Add a new query to the priority queue
            pendingRequests.Add(new PathQuery(start, end, priority, onPathFound));
        }

        /// <summary>
        /// Search for a path immediately.
        /// </summary>
        /// <remarks>
        /// This can be a very costly operation, so don't use it often!
        /// </remarks>
        /// <returns>The result path, of null if no path is found</returns>
        public IEnumerable<int> QueryImmediate(int start, int end)
        {
            return // Return immediate search result
                searchImmediate.Search(graph, start, end) ?
                searchImmediate.Path : null;
        }

        /// <summary>
        /// Update path manager
        /// </summary>
        public void Update()
        {
            // Do nothing if there's no pending requests
            if (pendingRequests.Count <= 0)
                return;

            int steps = 0;
            int totalSteps = 0;

            while (totalSteps < maxSearchStepsPerUpdate)
            {
                // Gets the query with the highest priority
                PathQuery query = pendingRequests.Top;

                // Handle current query
                bool? result = search.Search(
                    graph, query.Start, query.End, maxSearchStepsPerUpdate, out steps);

                // If the search completed, we can stop dealing with this
                // query and notify the PathFound event
                if (result.HasValue)
                {
                    // Pass null if no path is found
                    query.PathFound(result.Value ? search.Path : null);

                    // Remove it from the pending requests
                    pendingRequests.Pop();
                }

                totalSteps += steps;
            }
        }
    }
}
