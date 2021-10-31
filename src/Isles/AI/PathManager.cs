// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    //-------------------------------------------------------------------------
    //
    //  8 Directions Layout:
    //
    //       6  7  0
    //         \|/
    //       5 - - 1
    //         /|\
    //       4  3  2
    //
    //-------------------------------------------------------------------------

    /// <summary>
    /// Represents a sequence of nodes to follow.
    /// </summary>
    public struct PathEdge
    {
        public Vector2 Position;

        public PathEdge(Vector2 position)
        {
            Position = position;
        }
    }

    /// <summary>
    /// Represents a sequence of path edge to follow.
    /// </summary>
    public class Path : IEnumerable<PathEdge>
    {
        public LinkedList<PathEdge> Edges = new();

        public IEnumerator<PathEdge> GetEnumerator()
        {
            return Edges.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Edges.GetEnumerator();
        }
    }

    /// <summary>
    /// A brush describing the shape of the obstacle.
    /// </summary>
    public class PathBrush
    {
        /// <summary>
        /// X, Y determines the relative xy position of each marked point.
        /// </summary>
        public int[] X;
        public int[] Y;

        /// <summary>
        /// Size of the brush.
        /// </summary>
        public int SizeX;
        public int SizeY;

        /// <summary>
        /// Internal data generated to help with graph search.
        /// </summary>
        public int[][] DX = new int[8][];
        public int[][] DY = new int[8][];

        public PathBrush(IEnumerable<Point> grids)
        {
            var x = new List<int>();
            var y = new List<int>();

            foreach (Point p in grids)
            {
                x.Add(p.X);
                y.Add(p.Y);
            }

            CreateFromPoints(x, y);
        }

        public PathBrush(int[] x, int[] y)
        {
            CreateFromPoints(new List<int>(x), new List<int>(y));
        }

        private void CreateFromPoints(List<int> x, List<int> y)
        {
            if (x.Count != y.Count || x.Count <= 0)
            {
                throw new ArgumentException();
            }

            X = new int[x.Count];
            Y = new int[y.Count];

            x.CopyTo(X, 0);
            y.CopyTo(Y, 0);

            // Compute size
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            for (var i = 0; i < x.Count; i++)
            {
                if (x[i] < minX)
                {
                    minX = x[i];
                }

                if (x[i] > maxX)
                {
                    maxX = x[i];
                }

                if (y[i] < minY)
                {
                    minY = y[i];
                }

                if (y[i] > maxY)
                {
                    maxY = y[i];
                }
            }

            SizeX = 1 + maxX - minX;
            SizeY = 1 + maxY - minY;

            // Complain about about it if the given path brush is too large
            if (SizeX <= 0 || SizeX > 128 || SizeY <= 0 || SizeY > 128)
            {
                throw new ArgumentException();
            }

            // Min values of the brush should start from zero
            for (var i = 0; i < x.Count; i++)
            {
                X[i] -= minX;
                x[i] -= minX;
                Y[i] -= minY;
                y[i] -= minY;
            }

            // Compute the mask value for 8 directions.
            // Which node we should check for connectivity if we move one step
            // ahead along that direction.
            var dx = new List<int>();
            var dy = new List<int>();

            //// Append a border
            // x.Add(SizeX); y.Add(SizeY);
            // x.Add(SizeX); y.Add(-1);
            // x.Add(-1); y.Add(SizeX);
            // x.Add(-1); y.Add(-1);
            for (var k = 0; k < 8; k++)
            {
                dx.Clear();
                dy.Clear();

                for (var i = 0; i < x.Count; i++)
                {
                    var xx = x[i] + PathGraph.DX8[k];
                    var yy = y[i] + PathGraph.DY8[k];

                    // Checks if the new point excced the boundary
                    if (xx < 0 || xx >= SizeX || yy < 0 || yy >= SizeY)
                    {
                        dx.Add(xx);
                        dy.Add(yy);
                        continue;
                    }

                    // Checks if the new point exists in the old brush
                    var exist = false;

                    for (var n = 0; n < x.Count; n++)
                    {
                        if (x[n] == xx && y[n] == yy)
                        {
                            exist = true;
                            break;
                        }
                    }

                    if (!exist)
                    {
                        dx.Add(xx);
                        dy.Add(yy);
                    }
                }

                DX[k] = dx.ToArray();
                DY[k] = dy.ToArray();
            }
        }
    }

    /// <summary>
    /// Graph representing the path graph of Isles landscape.
    /// </summary>
    public class PathGraph : IGraph
    {
        /// <summary>
        /// Reference to the landscape.
        /// </summary>
        public ILandscape Landscape
        {
            get => landscape;
            set => landscape = value;
        }

        private ILandscape landscape;

        /// <summary>
        /// Graph data.
        /// </summary>
        /// <remarks>
        /// Each entry in the array represent the number of obstacles overlapping it.
        /// Note that we do not consider height in our cost calculation since the landcape
        /// is currently very flat.
        /// </remarks>
        private byte[,] data;
        private byte[,] dynamicData;

        /// <summary>
        /// Size of the grid bitmap.
        /// </summary>
        private readonly int entryWidth;

        /// <summary>
        /// Size of the grid bitmap.
        /// </summary>
        private readonly int entryHeight;

        /// <summary>
        /// Size of the grid bitmap.
        /// </summary>
        private readonly int nodeCount;

        public int EntryWidth => entryWidth;

        public int EntryHeight => entryHeight;

        /// <summary>
        /// Size of each entry cell.
        /// </summary>
        private readonly float cellSize;

        /// <summary>
        /// Size of each entry cell.
        /// </summary>
        private readonly float halfCellSize;

        /// <summary>
        /// Size of the map.
        /// </summary>
        private readonly float mapWidth;

        /// <summary>
        /// Size of the map.
        /// </summary>
        private readonly float mapHeight;

        /// <summary>
        /// Gets the size of each cell.
        /// </summary>
        public float CellSize => cellSize;

        public float HalfCellSize => halfCellSize;

        /// <summary>
        /// Gets or sets the brush of the path finder.
        /// </summary>
        /// <remarks>
        /// By setting the brush of the path graph, we're able to find
        /// a way that let an object with the shape of the path brush move through.
        /// </remarks>
        public PathBrush Brush
        {
            get => brush;
            set => brush = value;
        }

        private PathBrush brush;

        /// <summary>
        /// Gets or sets the boundary of the path graph.
        /// </summary>
        public Rectangle? Boundary
        {
            get => boundary;
            set => boundary = value;
        }

        private Rectangle? boundary;

        /// <summary>
        /// Gets or sets whether dynamic obstacles are ignored when searching.
        /// </summary>
        public bool IgnoreDynamicObstacles
        {
            get => ignoreDynamicObstacles;
            set => ignoreDynamicObstacles = value;
        }

        private bool ignoreDynamicObstacles;

        /// <summary>
        /// Construct a landscape graph.
        /// </summary>
        public PathGraph(ILandscape landscape, float resolution, IEnumerable<Point> occluders)
        {
            this.landscape = landscape ?? throw new ArgumentNullException();

            // Note landscape grid include the edges.
            // Currently the maximum grid resolution is 256 * 256, which generates
            // a path graph with a resolution of 2048 * 2048. Pretty large, ha :)
            if (landscape.GridCount.X > 257 || landscape.GridCount.Y > 257)
            {
                throw new ArgumentException();
            }

            entryWidth = landscape.GridCount.X - 1;
            entryHeight = landscape.GridCount.Y - 1;

            if ((entryWidth != 32 && entryWidth != 64 && entryWidth != 128 && entryWidth != 256) ||
                (entryHeight != 32 && entryHeight != 64 && entryHeight != 128 && entryHeight != 256))
            {
                throw new InvalidOperationException();
            }

            // Give the path graph more detail
            entryWidth = (int)(entryWidth * resolution);
            entryHeight = (int)(entryHeight * resolution);

            nodeCount = entryHeight * entryWidth;

            mapWidth = landscape.Size.X;
            mapHeight = landscape.Size.Y;

            if (!Math2D.FloatEquals(mapWidth / entryWidth, mapHeight / entryHeight))
            {
                throw new InvalidOperationException();
            }

            cellSize = mapWidth / entryWidth;
            halfCellSize = cellSize / 2;

            // Initialize grid data
            InitializeEntry(resolution, occluders);

            this.landscape = landscape;
        }

        public static readonly int[] DX8 = new int[8] { 1, 1, 1, 0, -1, -1, -1, 0 };
        public static readonly int[] DY8 = new int[8] { 1, 0, -1, -1, -1, 0, 1, 1 };

        public static readonly int[] DX4 = new int[4] { 1, 0, -1, 0 };
        public static readonly int[] DY4 = new int[4] { 0, -1, 0, 1 };

        /// <summary>
        /// Initialize grid entries.
        /// </summary>
        private void InitializeEntry(float resolution, IEnumerable<Point> occluders)
        {
            data = new byte[entryWidth, entryHeight];
            dynamicData = new byte[entryWidth, entryHeight];

            for (var y = 0; y < entryHeight; y++)
            {
                for (var x = 0; x < entryWidth; x++)
                {
                    data[x, y] = dynamicData[x, y] = 0;

                    if (resolution >= 2)
                    {
                        // If our resolution is big enough, we only test mid point
                        Vector2 position = GridToPosition(x, y);
                        if (landscape.IsPointOccluded(position.X, position.Y))
                        {
                            Mark(x, y);
                        }
                    }
                    else
                    {
                        // Otherwise, count the number of obstacles
                        const int SamplesPerGrid = 8;

                        var size = (int)(SamplesPerGrid / resolution);
                        var overlappedPoints = 0;
                        var totalPoints = size * size;
                        var step = cellSize / size;

                        for (var xx = x * cellSize; xx < (x + 1) * cellSize; xx += step)
                        {
                            for (var yy = y * cellSize; yy < (y + 1) * cellSize; yy += step)
                            {
                                if (landscape.IsPointOccluded(xx, yy))
                                {
                                    overlappedPoints++;
                                }
                            }
                        }

                        if (1.0f * overlappedPoints / totalPoints >= 0.5f)
                        {
                            Mark(x, y);
                        }
                    }
                }
            }

            // Mark custom occluders
            if (occluders != null)
            {
                foreach (Point p in occluders)
                {
                    Mark(p.X, p.Y);
                }
            }
        }

        /// <summary>
        /// Mark one grid as obstacle. Input boundary is not checked.
        /// </summary>
        public void Mark(int x, int y)
        {
            // System.Diagnostics.Debug.Assert(data[x, y] == 0);
            data[x, y]++;
        }

        /// <summary>
        /// Unmark one grid. Input boundary is not checked.
        /// </summary>
        public void Unmark(int x, int y)
        {
            // System.Diagnostics.Debug.Assert(data[x, y] > 0);
            data[x, y]--;
        }

        /// <summary>
        /// Mark one grid as dynamic obstacle. Input boundary is not checked.
        /// </summary>
        public void MarkDynamic(int x, int y)
        {
            // Note agents can ignore dynamic obstacles when they're
            // moving, so this assertion does not hold.

            // System.Diagnostics.Debug.Assert(dynamicData[x, y] == 0);
            dynamicData[x, y]++;
        }

        /// <summary>
        /// Unmark one grid. Input boundary is not checked.
        /// </summary>
        public void UnmarkDynamic(int x, int y)
        {
            // System.Diagnostics.Debug.Assert(dynamicData[x, y] > 0);
            dynamicData[x, y]--;
        }

        /// <summary>
        /// Calculate the cost to move from a to b.
        /// </summary>
        private float CalculateCost(int a, int b)
        {
            Vector2 positionA = IndexToPosition(a);
            Vector2 positionB = IndexToPosition(b);

            return Vector2.Subtract(positionA, positionB).Length();
        }

        public int GridToIndex(int x, int y)
        {
            return y * entryWidth + x;
        }

        public Point IndexToGrid(int i)
        {
            return new Point(i % entryWidth, i / entryWidth);
        }

        public Vector2 GridToPosition(int x, int y)
        {
            return new Vector2(x * cellSize + halfCellSize, y * cellSize + halfCellSize);
        }

        public Vector2 GridToPosition(int x, int y, PathBrush brush)
        {
            Vector2 position = GridToPosition(x, y);

            if (brush == null)
            {
                return position;
            }

            position.X += brush.SizeX * halfCellSize - halfCellSize;
            position.Y += brush.SizeY * halfCellSize - halfCellSize;

            return position;
        }

        public Point PositionToGrid(float x, float y)
        {
            return new Point((int)(x / cellSize), (int)(y / cellSize));
        }

        public Point PositionToGrid(float x, float y, PathBrush brush)
        {
            if (brush == null)
            {
                return PositionToGrid(x, y);
            }

            int xGrid, yGrid;

            xGrid = brush.SizeX % 2 == 0 ? (int)Math.Round(x / cellSize) - brush.SizeX / 2 : (int)(x / cellSize) - brush.SizeX / 2;

            yGrid = brush.SizeY % 2 == 0 ? (int)Math.Round(y / cellSize) - brush.SizeY / 2 : (int)(y / cellSize) - brush.SizeY / 2;

            return new Point(xGrid, yGrid);
        }

        /// <summary>
        /// Get graph index from position.
        /// </summary>
        public int PositionToIndex(Vector2 position)
        {
            return (int)(position.X / cellSize) + (int)(position.Y / cellSize) * entryWidth;
        }

        public int PositionToIndex(Vector2 position, PathBrush brush)
        {
            Point p = PositionToGrid(position.X, position.Y, brush);
            return GridToIndex(p.X, p.Y);
        }

        /// <summary>
        /// Get position from graph index.
        /// </summary>
        public Vector2 IndexToPosition(int index)
        {
            // Return grid center
            var x = index % entryWidth;
            var y = index / entryWidth;

            return new Vector2(x * cellSize + halfCellSize, y * cellSize + halfCellSize);
        }

        public int NodeCount => nodeCount;

        /// <summary>
        /// Checks whether a position is obstructed on the path graph.
        /// </summary>
        public bool IsPositionObstructed(float x, float y, bool includeDynamic)
        {
            Point p = PositionToGrid(x, y);

            if (p.X >= 0 && p.X < entryWidth &&
                p.Y >= 0 && p.Y < entryHeight)
            {
                return includeDynamic ? data[p.X, p.Y] > 0 || dynamicData[p.X, p.Y] > 0 : data[p.X, p.Y] > 0;
            }

            return false;
        }

        /// <summary>
        /// Gets whether a brush is been obstruected on the path graph.
        /// </summary>
        public bool IsBrushObstructed(float x, float y, PathBrush brush, bool includeDynamic)
        {
            if (brush == null)
            {
                return IsPositionObstructed(x, y, includeDynamic);
            }

            // Check each individual grid
            foreach (Point p in EnumerateGridsInBrush(new Vector2(x, y), brush))
            {
                if (IsGridObstructed(p.X, p.Y, includeDynamic))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets whether a grid is been obstructed on the path graph.
        /// </summary>
        public bool IsGridObstructed(int x, int y, bool includeDynamic)
        {
            var withinBounds = boundary == null
                ? x >= 0 && x < entryWidth && y >= 0 && y < entryHeight
                : x >= boundary.Value.X && x < boundary.Value.Right &&
                                y >= boundary.Value.Y && y < boundary.Value.Bottom;
            if (!withinBounds)
            {
                return true;
            }

            return includeDynamic ? data[x, y] > 0 || dynamicData[x, y] > 0 : data[x, y] > 0;
        }

        public IEnumerable<Point> EnumerateGridsInBrush(Vector2 position, PathBrush brush)
        {
            if (brush != null)
            {
                Point p = PositionToGrid(position.X, position.Y, brush);

                for (var i = 0; i < brush.X.Length; i++)
                {
                    yield return new Point(brush.X[i] + p.X, brush.Y[i] + p.Y);
                }
            }
        }

        /// <summary>
        /// Gets all the out-coming edges of a given node.
        /// </summary>
        public IEnumerable<GraphEdge> GetEdges(int nodeIndex)
        {
            GraphEdge edge;
            bool connected;
            var x = nodeIndex % entryWidth;
            var y = nodeIndex / entryWidth;

            for (var k = 0; k < 8; k++)
            {
                connected = true;

                if (brush == null)
                {
                    // If no path brush is specified, test 8 adjacent nodes.
                    connected = !IsGridObstructed(x + DX8[k], y + DY8[k], !ignoreDynamicObstacles);
                }
                else
                {
                    for (var i = 0; i < brush.DX[k].Length; i++)
                    {
                        if (IsGridObstructed(x + brush.DX[k][i], y + brush.DY[k][i], !ignoreDynamicObstacles))
                        {
                            connected = false;
                            break;
                        }
                    }
                }

                if (connected)
                {
                    edge.From = nodeIndex;
                    edge.To = GridToIndex(x + DX8[k], y + DY8[k]);
                    edge.Cost = (DX8[k] == 0 || DY8[k] == 0) ? 10 : 14;

                    yield return edge;
                }
            }
        }

        /// <summary>
        /// Gets the heuristic value for A* search, simply return the cost from current
        /// node to the destination node.
        /// </summary>
        public float GetHeuristicValue(int currentIndex, int endIndex)
        {
            return CalculateCost(currentIndex, endIndex);
        }
    }

    /// <summary>
    /// Path manager to provide path finding service
    /// for all game agents.
    /// </summary>
    public class PathManager
    {
        /// <summary>
        /// Resolution for small graph.
        /// </summary>
        public const float Resolution = 4;

        /// <summary>
        /// Current landscape.
        /// </summary>
        public ILandscape Landscape
        {
            get => landscape;
            set
            {
                landscape = value;
                graph.Landscape = value;
                largeGraph.Landscape = value;
            }
        }

        private ILandscape landscape;

        /// <summary>
        /// Graph search.
        /// </summary>
        private readonly GraphSearchAStar search;
        private readonly GraphSearchAStar largeSearch;

        /// <summary>
        /// Gets the detailed path graph used for searching.
        /// </summary>
        public PathGraph Graph => graph;

        /// <summary>
        /// Gets the large path graph for path searching on the landscape.
        /// </summary>
        public PathGraph LargeGraph => largeGraph;

        private readonly PathGraph graph;
        private readonly PathGraph largeGraph;

        /// <summary>
        /// Dynamic entities.
        /// </summary>
        private readonly List<IMovable> dynamicObstacles = new();

        /// <summary>
        /// Gets or sets the maximun number of search steps per update.
        /// </summary>
        public int MaxSearchStepsPerUpdate
        {
            get => maxSearchStepsPerUpdate;

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                maxSearchStepsPerUpdate = value;
            }
        }

        private int maxSearchStepsPerUpdate = 2000;

        /// <summary>
        /// Gets whether the path manager is busy at the moment.
        /// </summary>
        public bool Busy => pendingRequestsCount > 0;

        /// <summary>
        /// Pending query requests.
        /// </summary>
        private readonly List<PathQuery> pendingRequests = new(8);

        /// <summary>
        /// Number of pending path finding requests.
        /// Note that the length of the array pendingRequests isn't the real request count.
        /// </summary>
        private int pendingRequestsCount;

        /// <summary>
        /// The query current active.
        /// </summary>
        private PathQuery currentQuery;

        /// <summary>
        /// Internal representation for a given path query.
        /// </summary>
        private class PathQuery : IComparable<PathQuery>
        {
            /// <summary>
            /// Start, end node.
            /// </summary>
            public int Start;

            /// <summary>
            /// Start, end node.
            /// </summary>
            public int End;

            /// <summary>
            /// Destination position.
            /// </summary>
            public Vector2 Destination;

            /// <summary>
            /// Priority of this query.
            /// </summary>
            public float Priority;

            /// <summary>
            /// Handler for this query.
            /// </summary>
            public IEventListener Subscriber;

            /// <summary>
            /// The grids owned by the dynamic obstacle are removed before path finding.
            /// </summary>
            public IMovable Obstacle;

            /// <summary>
            /// Bounds of the path graph.
            /// </summary>
            public Rectangle Boundary;

            /// <summary>
            /// Compare function, used in the priority queue.
            /// </summary>
            public int CompareTo(PathQuery query)
            {
                return (int)(Priority - query.Priority);
            }

            /// <summary>
            /// For the ease of constructing a path query.
            /// </summary>
            public PathQuery(int start, int end, Vector2 destination, float priority,
                             Rectangle boundary, IEventListener subscriber, IMovable obstacle)
            {
                Start = start;
                End = end;
                Destination = destination;
                Priority = priority;
                Subscriber = subscriber ?? throw new ArgumentException();
                Obstacle = obstacle;
                Boundary = boundary;
            }
        }

        /// <summary>
        /// Info attached to each dynamic obstacle.
        /// </summary>
        private class Tag
        {
            /// <summary>
            /// List of marks that the obstacle currently overlaps.
            /// </summary>
            public List<Point> Marks;

            /// <summary>
            /// Stored obstacle position.
            /// </summary>
            public Vector3 Position = Vector3.Zero;
        }

        /// <summary>
        /// Create a circular path brush.
        /// </summary>
        public PathBrush CreateBrush(float radius)
        {
            if (radius < 0)
            {
                throw new ArgumentException();
            }

            var size = (int)(2 * radius / graph.CellSize);
            if (size < 1)
            {
                size = 1;
            }

            var halfSize = size / 2;
            if (halfSize < 0)
            {
                halfSize = 0;
            }

            radius = halfSize * graph.CellSize;

            var xPoints = new List<int>();
            var yPoints = new List<int>();

            if (size % 2 == 0)
            {
                // If we have even number of grids
                for (var x = -halfSize; x < halfSize; x++)
                {
                    for (var y = -halfSize; y < halfSize; y++)
                    {
                        var xx = x * graph.CellSize + graph.HalfCellSize;
                        var yy = y * graph.CellSize + graph.HalfCellSize;

                        if (xx * xx + yy * yy <= radius * radius)
                        {
                            xPoints.Add(x);
                            yPoints.Add(y);
                        }
                    }
                }
            }
            else
            {
                // If we have odd number of grids
                for (var x = -halfSize; x <= halfSize; x++)
                {
                    for (var y = -halfSize; y <= halfSize; y++)
                    {
                        var xx = x * graph.CellSize;
                        var yy = y * graph.CellSize;

                        if (xx * xx + yy * yy <= radius * radius)
                        {
                            xPoints.Add(x);
                            yPoints.Add(y);
                        }
                    }
                }
            }

            return new PathBrush(xPoints.ToArray(), yPoints.ToArray());
        }

        /// <summary>
        /// Adds an entity as a dynamic obstacle.
        /// </summary>
        public void AddMovable(IMovable obstacle)
        {
            if (obstacle == null || obstacle.MovementTag != null)
            {
                throw new ArgumentException();
            }

            System.Diagnostics.Debug.Assert(!dynamicObstacles.Contains(obstacle));

            // Add the entity to internal list
            dynamicObstacles.Add(obstacle);

            // Store marked grids in the tag of the obstacle
            var tag = new Tag
            {
                Position = obstacle.Position,
                Marks = new List<Point>(),
            };
            tag.Marks.AddRange(graph.EnumerateGridsInBrush(new Vector2(obstacle.Position.X,
                                                                       obstacle.Position.Y),
                                                                       obstacle.Brush));
            obstacle.MovementTag = tag;

            // Change graph structure
            foreach (Point p in tag.Marks)
            {
                graph.MarkDynamic(p.X, p.Y);
            }
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        public void RemoveMovable(IMovable obstacle)
        {
            if (obstacle == null)
            {
                throw new ArgumentException();
            }

            // Removes the entity from internal list
            for (var i = 0; i < dynamicObstacles.Count; i++)
            {
                if (dynamicObstacles[i] == obstacle)
                {
                    var tag = obstacle.MovementTag as Tag;

                    // Change path graph structure
                    foreach (Point p in tag.Marks)
                    {
                        graph.UnmarkDynamic(p.X, p.Y);
                    }

                    obstacle.MovementTag = null;
                    dynamicObstacles.RemoveAt(i);
                    return;
                }
            }
        }

        public IEnumerable<Point> EnumerateGridsInAxisAlignedRectangle(Vector2 position, int grids)
        {
            var x = position.X / graph.CellSize;
            var y = position.Y / graph.CellSize;

            if (grids % 2 == 1)
            {
                var xx = (int)x;
                var yy = (int)y;

                for (var i = -grids / 2; i <= grids / 2; i++)
                {
                    yield return new Point(xx + i, yy + i);
                }
            }
            else
            {
                var xx = (x - (int)x) > 0.5f ? (int)x + 1 : (int)x;
                var yy = (y - (int)y) > 0.5f ? (int)y + 1 : (int)y;

                for (var i = -grids / 2; i < grids / 2; i++)
                {
                    yield return new Point(xx + i, yy + i);
                }
            }
        }

        public IEnumerable<Point> EnumerateGridsInOutline(Outline outline)
        {
            if (outline.Type == OutlineType.Circle)
            {
                return EnumerateGridsInCircle(outline.Position, outline.Radius);
            }

            return outline.Type == OutlineType.Rectangle
                ? EnumerateGridsInRectangle(outline.Min, outline.Max,
                                                 outline.Position, outline.Rotation)
                : null;
        }

        public IEnumerable<Point> EnumerateGridsInRectangle(
            Vector2 min, Vector2 max, Vector2 translation, float rotation)
        {
            Vector2 mid = Math2D.LocalToWorld((min + max) / 2, translation, rotation);

            Point midGrid = graph.PositionToGrid(mid.X, mid.Y);
            var radius = 1 + (int)(Math.Max(max.X - min.X, max.Y - min.Y) / graph.CellSize / 2);

            Vector2 p;

            for (var x = midGrid.X - radius; x <= midGrid.X + radius; x++)
            {
                for (var y = midGrid.Y - radius; y < midGrid.Y + radius; y++)
                {
                    p = graph.GridToPosition(x, y);
                    if (Math2D.PointInRectangle(p, min, max, translation, rotation))
                    {
                        yield return new Point(x, y);
                    }
                }
            }
        }

        public IEnumerable<Point> EnumerateGridsInCircle(Vector2 position, float radius)
        {
            Vector2 min = position - new Vector2(radius, radius);
            Vector2 max = position + new Vector2(radius, radius);

            var xMin = (int)(min.X / graph.CellSize);
            var xMax = (int)(max.X / graph.CellSize);
            var yMin = (int)(min.Y / graph.CellSize);
            var yMax = (int)(max.Y / graph.CellSize);

            var radiusSquared = radius * radius;

            var counter = 0;
            Vector2 center;
            for (var y = yMin; y <= yMax; y++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    center.X = x * graph.CellSize + graph.CellSize / 2;
                    center.Y = y * graph.CellSize + graph.CellSize / 2;

                    if (radiusSquared >=
                        Vector2.Subtract(center, position).LengthSquared())
                    {
                        counter++;
                        yield return new Point(x, y);
                    }
                }
            }

            if (counter == 0)
            {
                // At least one grid must be returned
                yield return new Point(xMin, yMin);
            }
        }

        public IEnumerable<Point> EnumerateGridsInnerOut(int x, int y, int maxRadius)
        {
            for (var r = 1; r < maxRadius; r++)
            {
                for (var i = -r; i < r; i++)
                {
                    yield return new Point(x + r, y + i);
                    yield return new Point(x - r, y + i + 1);
                    yield return new Point(x + i + 1, y + r);
                    yield return new Point(x + i, y - r);
                }
            }
        }

        /// <summary>
        /// Create a new path manager on a given landscape.
        /// </summary>
        /// <param name="landscape"></param>
        public PathManager(ILandscape landscape, IEnumerable<Point> occluders)
        {
            this.landscape = landscape;

            // Create a new graph
            graph = new PathGraph(landscape, Resolution, occluders);
            largeGraph = new PathGraph(landscape, 0.25f, null);

            // Create a new graph searcher
            search = new GraphSearchAStar();
            largeSearch = new GraphSearchAStar();

            // Read in settings value
            MaxSearchStepsPerUpdate = BaseGame.Singleton.Settings.MaxPathSearchStepsPerUpdate;
        }

        public void Mark(IMovable agent)
        {
            if (agent != null && agent.MovementTag is Tag)
            {
                var tag = agent.MovementTag as Tag;

                foreach (Point p in tag.Marks)
                {
                    graph.MarkDynamic(p.X, p.Y);
                }
            }
        }

        public void Unmark(IMovable agent)
        {
            if (agent != null && agent.MovementTag is Tag)
            {
                var tag = agent.MovementTag as Tag;

                foreach (Point p in tag.Marks)
                {
                    graph.UnmarkDynamic(p.X, p.Y);
                }
            }
        }

        public void Mark(IEnumerable<Point> staticMarks)
        {
            foreach (Point p in staticMarks)
            {
                graph.Mark(p.X, p.Y);
            }
        }

        public void Unmark(IEnumerable<Point> staticMarks)
        {
            foreach (Point p in staticMarks)
            {
                graph.Unmark(p.X, p.Y);
            }
        }

        /// <summary>
        /// Gets the unobstructed grid that are nearest to the specified point.
        /// </summary>
        public Vector2 FindValidPosition(Vector2 position, PathBrush brush)
        {
            var x = (int)(position.X / graph.CellSize);
            var y = (int)(position.Y / graph.CellSize);

            // First check the input grid
            if (!graph.IsBrushObstructed(position.X, position.Y, brush, true))
            {
                return position;
            }

            // look up its adjancent grids
            foreach (Point p in EnumerateGridsInnerOut(x, y, 512))
            {
                if (p.X >= 0 && p.X < graph.EntryWidth &&
                    p.Y >= 0 && p.Y < graph.EntryHeight)
                {
                    position = graph.GridToPosition(p.X, p.Y);
                    if (!graph.IsBrushObstructed(position.X, position.Y, brush, true))
                    {
                        return position;
                    }
                }
            }

            return position;
        }

        /// <summary>
        /// Find the next valid position that the agent can be placed at.
        /// </summary>
        public Vector2 FindNextValidPosition(Vector2 target, Vector2? start, Vector2? lastPosition, IMovable agent)
        {
            Unmark(agent);
            if (CanBePlacedAt(target.X, target.Y, agent))
            {
                Mark(agent);
                return target;
            }

            Vector2 lastPositionValue;
            Vector2 startValue;
            if (lastPosition.HasValue && lastPosition.Value == target)
            {
                lastPosition = null;
            }

            startValue = start ?? Vector2.Zero;

            if (target == startValue)
            {
                Mark(agent);
                return target + new Vector2(0.2f, 0.2f);
            }

            if (!lastPosition.HasValue)
            {
                lastPositionValue = target - startValue;
                lastPositionValue.Normalize();
                lastPositionValue *= 1.2f;
                lastPositionValue = target - lastPositionValue;
                if (CanBePlacedAt(lastPositionValue.X, lastPositionValue.Y, agent))
                {
                    Mark(agent);
                    return lastPositionValue;
                }
            }
            else
            {
                lastPositionValue = lastPosition.Value;
            }

            Vector2 newDirection, newTarget, lastDirection, originalDirection, prep;
            float distance = 0;
            float angle;
            var count = 0;

            originalDirection = target - startValue;
            while (true)
            {
                if (count > 300)
                {
                    Mark(agent);
                    return target + new Vector2(0.2f, 0.2f);
                }

                lastDirection = target - lastPositionValue;
                if (lastDirection == Vector2.Zero)
                {
                    lastDirection = originalDirection;
                    lastDirection.Normalize();
                    lastDirection *= distance + 1.2f * graph.CellSize;
                }

                distance = lastDirection.Length();
                prep = new Vector2(-originalDirection.Y, originalDirection.X);

                var factor = Vector2.Dot(originalDirection, lastDirection)
                        / originalDirection.Length() / lastDirection.Length();
                if (factor > 1)
                {
                    factor = 1;
                }

                if (factor < -1)
                {
                    factor = -1;
                }

                angle = (float)Math.Acos(factor);
                angle += (float)Math.PI / 15;
                if (angle > Math.PI)
                {
                    distance += graph.CellSize * 1.2f;
                    newDirection = originalDirection;
                }
                else
                {
                    newDirection = Vector2.Dot(lastDirection, prep) > 0
                        ? Math2D.LocalToWorld(originalDirection, Vector2.Zero, -angle)
                        : Math2D.LocalToWorld(originalDirection, Vector2.Zero, angle);
                }

                newDirection.Normalize();
                newDirection *= distance;
                newTarget = target - newDirection;
                if (CanBePlacedAt(newTarget.X, newTarget.Y, agent))
                {
                    Mark(agent);
                    return newTarget;
                }

                lastPositionValue = newTarget;
                count++;
            }
        }

        /// <summary>
        /// Gets the unobstructed grid that are nearest to the specified point.
        /// </summary>
        public Vector2 FindValidPosition(Vector2 position, Vector2? start, IMovable agent)
        {
            return FindNextValidPosition(position, start, start, agent);
        }

        /// <summary>
        /// Tests to see if the specified grids can be placed at a given location.
        /// </summary>
        public bool CanBePlacedAt(IEnumerable<Point> grids, bool includingDynamics)
        {
            foreach (Point p in grids)
            {
                if (graph.IsGridObstructed(p.X, p.Y, includingDynamics))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tests to see if the specified brush can be placed at a given location.
        /// </summary>
        public bool CanBePlacedAt(float x, float y, PathBrush brush, bool includingDynamics)
        {
            if (brush == null)
            {
                throw new ArgumentNullException();
            }

            foreach (Point p in graph.EnumerateGridsInBrush(new Vector2(x, y), brush))
            {
                if (graph.IsGridObstructed(p.X, p.Y, includingDynamics))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tests to see if the agent can be positioned to the target.
        /// </summary>
        public bool CanBePlacedAt(float x, float y, IMovable agent)
        {
            if (agent == null || !(agent.MovementTag is Tag))
            {
                return true;
            }

            Unmark(agent);

            foreach (Point p in graph.EnumerateGridsInBrush(new Vector2(x, y), agent.Brush))
            {
                if (graph.IsGridObstructed(p.X, p.Y, !agent.IgnoreDynamicObstacles))
                {
                    Mark(agent);
                    return false;
                }
            }

            Mark(agent);
            return true;
        }

        /// <summary>
        /// Tests to see if the agent can move between two positions.
        /// </summary>
        public bool CanMoveBetween(Vector2 start, Vector2 end, IMovable agent, bool includeDynamic)
        {
            if (agent == null || !(agent.MovementTag is Tag))
            {
                return true;
            }

            Unmark(agent);

            var step = Vector2.Subtract(end, start);
            step.Normalize();
            step *= graph.CellSize * 0.5f;

            var firstGrid = true;
            var steps = (int)(Vector2.Subtract(end, start).Length() / step.Length());

            for (var i = 0; i < steps; i++)
            {
                // Make it more precise on the first grid
                if (firstGrid)
                {
                    for (var k = 0; k < 5; k++)
                    {
                        if (graph.IsBrushObstructed(start.X, start.Y, agent.Brush, includeDynamic))
                        {
                            Mark(agent);
                            return false;
                        }

                        start += step / 5;
                    }

                    firstGrid = false;
                }
                else
                {
                    if (graph.IsBrushObstructed(start.X, start.Y, agent.Brush, includeDynamic))
                    {
                        Mark(agent);
                        return false;
                    }

                    start += step;
                }
            }

            Mark(agent);
            return true;
        }

        /// <summary>
        /// Querys a path on the large graph.
        /// </summary>
        public Path QueryPath(Vector2 start, Vector2 end)
        {
            Point endGrid = largeGraph.PositionToGrid(end.X, end.Y);

            // Adjust end position to avoid searching on the whole graph
            if (largeGraph.IsGridObstructed(endGrid.X, endGrid.Y, true))
            {
                var counter = 0;
                foreach (Point p in EnumerateGridsInnerOut(endGrid.X, endGrid.Y, 512))
                {
                    if (counter++ > 512)
                    {
                        return new Path();
                    }

                    if (p.X >= 0 && p.X < largeGraph.EntryWidth &&
                        p.Y >= 0 && p.Y < largeGraph.EntryHeight &&
                        !largeGraph.IsGridObstructed(p.X, p.Y, true))
                    {
                        endGrid = p;
                        break;
                    }
                }
            }

            // Perform the search, this should be pretty fast
            if (largeSearch.Search(largeGraph, largeGraph.PositionToIndex(start),
                                   largeGraph.GridToIndex(endGrid.X, endGrid.Y)))
            {
                // Build standard path
                Path path = BuildPath(largeSearch.Path, largeGraph, null, false);

                // If we moved the end position, don't forget to append it to the end
                if (path.Edges.Count > 0)
                {
                    path.Edges.AddLast(new PathEdge(end));
                }

                return path;
            }

            return null;
        }

        public const int AreaSize = (int)(16 * Resolution);
        private const float FactorToStart = 0.55f;

        /// <summary>
        /// Query a path asychroniously from start node to end node.
        ///
        /// FIXME: Can we change the structure of the graph when the search is still
        ///        been carry on? We are not perform searching using multi-threading,
        ///        but we limit the maximum search steps each frame, and the priority
        ///        queue in the AStar algorithm can recognize current search state.
        /// </summary>
        /// <param name="pathWidth">Width of the path.</param>
        /// <param name="priority">Higher number means lower priority.</param>
        /// <param name="marks">
        /// Some additional obstacles you may wish to add to the graph.
        /// </param>
        public void QueryPath(IEventListener subscriber, Vector2 start, Vector2 end,
                              float priority, IMovable obstacle)
        {
            // PositionToIndex will return the bottom left cornor of the brush
            PathBrush brush = obstacle.Brush;
            Point startGrid = graph.PositionToGrid(start.X, start.Y, brush);
            Point endGrid = graph.PositionToGrid(end.X, end.Y, brush);

            // Compute the boundary
            Rectangle bounds;
            bounds.Width = AreaSize;
            bounds.Height = AreaSize;

            bounds.X = (endGrid.X + startGrid.X) / 2 - (int)(bounds.Width * FactorToStart);
            bounds.Y = (endGrid.Y + startGrid.Y) / 2 - (int)(bounds.Height * FactorToStart);

            // Creates a new path query
            var query = new PathQuery(graph.GridToIndex(startGrid.X, startGrid.Y),
                                            graph.GridToIndex(endGrid.X, endGrid.Y),
                                            end, priority, bounds, subscriber, obstacle);

            // One subscriber can only query one path at a time,
            // the new path query will replace the old one.
            for (var i = 0; i < pendingRequests.Count; i++)
            {
                if (pendingRequests[i] != null &&
                    pendingRequests[i].Subscriber == subscriber)
                {
                    pendingRequests[i] = query;
                    return;
                }
            }

            // Check if there's any space for we to insert into the  list
            for (var i = 0; i < pendingRequests.Count; i++)
            {
                if (pendingRequests[i] == null)
                {
                    pendingRequests[i] = query;
                    pendingRequestsCount++;
                    return;
                }
            }

            // Add a new query to the priority queue
            pendingRequests.Add(query);
            pendingRequestsCount++;
        }

        /// <summary>
        /// Cancel a path query.
        /// </summary>
        public void CancelQuery(IEventListener subscriber)
        {
            for (var i = 0; i < pendingRequests.Count; i++)
            {
                if (pendingRequests[i] != null &&
                    pendingRequests[i].Subscriber == subscriber)
                {
                    pendingRequests.RemoveAt(i);
                    pendingRequestsCount--;
                    break;
                }
            }
        }

        /// <summary>
        /// Update path manager.
        /// </summary>
        public void Update()
        {
            // Reflect dynamic object movement on the path graph
            UpdatePathGraph();

            // Update the graph search
            UpdateSearch();
        }

        private void UpdatePathGraph()
        {
            // Change the structure of the graph if any dynamic obstacle moves
            for (var i = 0; i < dynamicObstacles.Count; i++)
            {
                IMovable obstacle = dynamicObstacles[i];

                // Make sure this is called before GameWorld.UpdateSceneManager,
                // because that method will reset entity.IsDirty to false...
                if (obstacle != null && obstacle.MovementTag is Tag)
                {
                    var tag = obstacle.MovementTag as Tag;

                    if (obstacle.Position != tag.Position)
                    {
                        // Unmark previous grids
                        foreach (Point p in tag.Marks)
                        {
                            graph.UnmarkDynamic(p.X, p.Y);
                        }

                        // Gets grids from brush
                        tag.Marks.Clear();
                        tag.Marks.AddRange(graph.EnumerateGridsInBrush(
                            new Vector2(obstacle.Position.X, obstacle.Position.Y),
                                                             obstacle.Brush));
                        // Mark new grids
                        foreach (Point p in tag.Marks)
                        {
                            graph.MarkDynamic(p.X, p.Y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Manually update the grid info after you changed its position.
        /// </summary>
        public void UpdateMovable(IMovable obstacle)
        {
            if (obstacle != null && obstacle.MovementTag is Tag)
            {
                var tag = obstacle.MovementTag as Tag;

                // Unmark previous grids
                foreach (Point p in tag.Marks)
                {
                    graph.UnmarkDynamic(p.X, p.Y);
                }

                // Gets grids from brush
                tag.Marks.Clear();
                tag.Marks.AddRange(graph.EnumerateGridsInBrush(
                    new Vector2(obstacle.Position.X, obstacle.Position.Y),
                                                     obstacle.Brush));
                // Mark new grids
                foreach (Point p in tag.Marks)
                {
                    graph.MarkDynamic(p.X, p.Y);
                }
            }
        }

        private void UpdateSearch()
        {
            var totalSteps = 0;

            // Do nothing if there's no pending requests
            if (pendingRequestsCount <= 0)
            {
                return;
            }

            while (totalSteps < maxSearchStepsPerUpdate && pendingRequestsCount > 0)
            {
                var min = -1;
                var priority = float.MaxValue;

                // Gets the query with the highest priority
                for (var i = 0; i < pendingRequests.Count; i++)
                {
                    if (pendingRequests[i] != null &&
                        pendingRequests[i].Priority < priority)
                    {
                        priority = pendingRequests[i].Priority;
                        min = i;
                    }
                }

                if (min < 0)
                {
                    break;
                }

                PathQuery query = pendingRequests[min];

                // Adjust graph parameters
                graph.Boundary = query.Boundary;
                graph.IgnoreDynamicObstacles = query.Obstacle.IgnoreDynamicObstacles;

                // Checks if current query is interrupted
                if (query != currentQuery)
                {
                    // Adjust graph brush
                    graph.Brush = query.Obstacle.Brush;

                    // Refresh current query
                    currentQuery = query;
                }

                Unmark(query.Obstacle);

                // Handle current query
                var result = search.Search(
                    graph, query.Start, query.End, maxSearchStepsPerUpdate, out var steps);

                Mark(query.Obstacle);

                // If the search completed, we can stop dealing with this
                // query and notify the PathFound event
                if (result.HasValue)
                {
                    // No query now
                    currentQuery = null;

                    if (result.Value)
                    {
                        // Path found
                        // It turns out that entities gets stucked due to
                        // path simplification and smoothing :(
                        Path path = BuildPath(search.Path, graph, query.Obstacle, true);

                        // Append the destination
                        path.Edges.AddLast(new PathEdge(query.Destination));

                        // Smooth path to eliminate artifical edges
                        SmoothPath(ref path, query.Obstacle);

                        // Notify the path found event
                        Event.SendMessage(EventType.PathFound, query.Subscriber, this, path);
                    }
                    else
                    {
                        // Path not found
                        Event.SendMessage(EventType.PathNotFound,
                                          query.Subscriber, this, null);
                    }

                    // Remove it from the pending requests
                    pendingRequests[min] = null;
                    pendingRequestsCount--;

                    System.Diagnostics.Debug.Assert(pendingRequestsCount >= 0);

                    // BaseGame game = BaseGame.Singleton;
                    // Vector2 s = graph.IndexToPosition(query.Start);
                    // Vector2 e = graph.IndexToPosition(query.End);
                    // game.Graphics2D.DrawLine(game.Project(new Vector3(s, landscape.GetHeight(s.X, s.Y))),
                    //                         game.Project(new Vector3(e, landscape.GetHeight(e.X, e.Y))),
                    //                         result.Value ? Color.White : Color.Black);
                }

                totalSteps += steps;
            }

            // Set graph bounds to null whether we're not searching
            graph.Boundary = null;
        }

        /// <summary>
        /// Converts from graph path (Accessed by index) to realworld path (Accessed by position).
        /// </summary>
        private Path BuildPath(IEnumerable<int> path, PathGraph graph, IMovable agent, bool simplifyPath)
        {
            var previous = new Point();
            var resultPath = new Path();
            PathBrush brush = agent != null ? agent.Brush : null;

            // Ignore first edge
            var firstEdge = true;

            var direction = -1;
            foreach (var i in path)
            {
                // Note how we invert the path.
                // Since the path from graph search are actually from the end to start.
                Point p = graph.IndexToGrid(i);

                var edge = new PathEdge(graph.GridToPosition(p.X, p.Y, brush));

                // Simplify path, leaving only corners.
                if (!firstEdge && simplifyPath && resultPath.Edges.Count > 0)
                {
                    var newDirection = GetDirection(previous, p);
                    previous = p;

                    // Add a new node only if it's in the corner
                    if (newDirection == direction)
                    {
                        resultPath.Edges.First.Value = edge;
                        continue;
                    }

                    direction = newDirection;
                    firstEdge = false;
                }

                resultPath.Edges.AddFirst(edge);
            }

            return resultPath;
        }

        public void SmoothPath(ref Path path, IMovable agent)
        {
            LinkedListNode<PathEdge> next;
            LinkedListNode<PathEdge> remove;
            LinkedListNode<PathEdge> current = path.Edges.First;

            // Ignore the first edge
            if (current != null)
            {
                current = current.Next;
            }

            while (current != null)
            {
                // Traverse the path nodes, remove as much nodes as we can
                if (current.Next == null)
                {
                    return;
                }

                // Advance two steps ahead and see if we can remove some nodes
                next = current.Next.Next;

                while (next != null)
                {
                    if (CanMoveBetween(current.Value.Position, next.Value.Position,
                                       agent, !agent.IgnoreDynamicObstacles))
                    {
                        // Remove the node if we can directly move through
                        remove = current.Next;
                        while (remove != next)
                        {
                            remove = remove.Next;
                            path.Edges.Remove(remove.Previous);
                        }
                    }

                    next = next.Next;
                }

                // Step to the next node
                current = current.Next;
            }
        }

        private int GetDirection(Point from, Point to)
        {
            if (Math2D.FloatEquals(to.X, from.X))
            {
                return to.Y > from.Y ? 7 : 3;
            }

            if (Math2D.FloatEquals(to.Y, from.Y))
            {
                return to.X > from.X ? 1 : 5;
            }

            return to.X > from.X ? to.Y > from.Y ? 0 : 2 : to.Y > from.Y ? 6 : 4;
        }

        public void DrawPath(Path path)
        {
            if (path == null)
            {
                return;
            }

            BaseGame game = BaseGame.Singleton;

            LinkedListNode<PathEdge> p = path.Edges.First;

            while (p != null && p.Next != null)
            {
                var a = new Vector3(p.Value.Position, 0);
                var b = new Vector3(p.Next.Value.Position, 0);

                a.Z = Landscape.GetHeight(a.X, a.Y);
                b.Z = Landscape.GetHeight(b.X, b.Y);

                game.Graphics2D.DrawLine(game.Project(a), game.Project(b), Color.White);

                p = p.Next;
            }
        }
    }
}
