// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

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
public struct GraphPathEdge
{
    public Vector2 Position;

    public GraphPathEdge(Vector2 position)
    {
        Position = position;
    }
}

/// <summary>
/// Represents a sequence of path edge to follow.
/// </summary>
public class GraphPath : IEnumerable<GraphPathEdge>
{
    public LinkedList<GraphPathEdge> Edges = new();

    public IEnumerator<GraphPathEdge> GetEnumerator()
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
public class PathGraph : IPathGraph
{
    /// <summary>
    /// Reference to the landscape.
    /// </summary>
    public ILandscape Landscape { get; set; }

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

    public int EntryWidth { get; }

    public int EntryHeight { get; }

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
    public float CellSize { get; }

    public float HalfCellSize { get; }

    /// <summary>
    /// Gets or sets the brush of the path finder.
    /// </summary>
    /// <remarks>
    /// By setting the brush of the path graph, we're able to find
    /// a way that let an object with the shape of the path brush move through.
    /// </remarks>
    public PathBrush Brush { get; set; }

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
    public bool IgnoreDynamicObstacles { get; set; }

    private ArrayBuilder<(int to, float cost)> _edges;

    /// <summary>
    /// Construct a landscape graph.
    /// </summary>
    public PathGraph(ILandscape landscape, float resolution, IEnumerable<Point> occluders)
    {
        Landscape = landscape ?? throw new ArgumentNullException();

        // Note landscape grid include the edges.
        // Currently the maximum grid resolution is 256 * 256, which generates
        // a path graph with a resolution of 2048 * 2048. Pretty large, ha :)
        if (landscape.GridCount.X > 257 || landscape.GridCount.Y > 257)
        {
            throw new ArgumentException();
        }

        EntryWidth = landscape.GridCount.X - 1;
        EntryHeight = landscape.GridCount.Y - 1;

        if ((EntryWidth != 32 && EntryWidth != 64 && EntryWidth != 128 && EntryWidth != 256) ||
            (EntryHeight != 32 && EntryHeight != 64 && EntryHeight != 128 && EntryHeight != 256))
        {
            throw new InvalidOperationException();
        }

        // Give the path graph more detail
        EntryWidth = (int)(EntryWidth * resolution);
        EntryHeight = (int)(EntryHeight * resolution);

        NodeCount = EntryHeight * EntryWidth;

        mapWidth = landscape.Size.X;
        mapHeight = landscape.Size.Y;

        if (!Math2D.FloatEquals(mapWidth / EntryWidth, mapHeight / EntryHeight))
        {
            throw new InvalidOperationException();
        }

        CellSize = mapWidth / EntryWidth;
        HalfCellSize = CellSize / 2;

        // Initialize grid data
        InitializeEntry(resolution, occluders);

        Landscape = landscape;
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
        data = new byte[EntryWidth, EntryHeight];
        dynamicData = new byte[EntryWidth, EntryHeight];

        for (var y = 0; y < EntryHeight; y++)
        {
            for (var x = 0; x < EntryWidth; x++)
            {
                data[x, y] = dynamicData[x, y] = 0;

                if (resolution >= 2)
                {
                    // If our resolution is big enough, we only test mid point
                    Vector2 position = GridToPosition(x, y);
                    if (Landscape.IsPointOccluded(position.X, position.Y))
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
                    var step = CellSize / size;

                    for (var xx = x * CellSize; xx < (x + 1) * CellSize; xx += step)
                    {
                        for (var yy = y * CellSize; yy < (y + 1) * CellSize; yy += step)
                        {
                            if (Landscape.IsPointOccluded(xx, yy))
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
        data[x, y]++;
    }

    /// <summary>
    /// Unmark one grid. Input boundary is not checked.
    /// </summary>
    public void Unmark(int x, int y)
    {
        data[x, y]--;
    }

    /// <summary>
    /// Mark one grid as dynamic obstacle. Input boundary is not checked.
    /// </summary>
    public void MarkDynamic(int x, int y)
    {
        dynamicData[x, y]++;
    }

    /// <summary>
    /// Unmark one grid. Input boundary is not checked.
    /// </summary>
    public void UnmarkDynamic(int x, int y)
    {
        dynamicData[x, y]--;
    }

    public void ClearDynamic()
    {
        Array.Clear(dynamicData, 0, dynamicData.Length);
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
        return y * EntryWidth + x;
    }

    public Point IndexToGrid(int i)
    {
        return new Point(i % EntryWidth, i / EntryWidth);
    }

    public Vector2 GridToPosition(int x, int y)
    {
        return new Vector2(x * CellSize + HalfCellSize, y * CellSize + HalfCellSize);
    }

    public Vector2 GridToPosition(int x, int y, PathBrush brush)
    {
        Vector2 position = GridToPosition(x, y);

        if (brush == null)
        {
            return position;
        }

        position.X += brush.SizeX * HalfCellSize - HalfCellSize;
        position.Y += brush.SizeY * HalfCellSize - HalfCellSize;

        return position;
    }

    public Point PositionToGrid(float x, float y)
    {
        return new Point((int)(x / CellSize), (int)(y / CellSize));
    }

    public Point PositionToGrid(float x, float y, PathBrush brush)
    {
        if (brush == null)
        {
            return PositionToGrid(x, y);
        }

        int xGrid, yGrid;

        xGrid = brush.SizeX % 2 == 0 ? (int)Math.Round(x / CellSize) - brush.SizeX / 2 : (int)(x / CellSize) - brush.SizeX / 2;

        yGrid = brush.SizeY % 2 == 0 ? (int)Math.Round(y / CellSize) - brush.SizeY / 2 : (int)(y / CellSize) - brush.SizeY / 2;

        return new Point(xGrid, yGrid);
    }

    /// <summary>
    /// Get graph index from position.
    /// </summary>
    public int PositionToIndex(Vector2 position)
    {
        return (int)(position.X / CellSize) + (int)(position.Y / CellSize) * EntryWidth;
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
        var x = index % EntryWidth;
        var y = index / EntryWidth;

        return new Vector2(x * CellSize + HalfCellSize, y * CellSize + HalfCellSize);
    }

    public int NodeCount { get; }

    /// <summary>
    /// Checks whether a position is obstructed on the path graph.
    /// </summary>
    public bool IsPositionObstructed(float x, float y, bool includeDynamic)
    {
        Point p = PositionToGrid(x, y);

        if (p.X >= 0 && p.X < EntryWidth &&
            p.Y >= 0 && p.Y < EntryHeight)
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
            ? x >= 0 && x < EntryWidth && y >= 0 && y < EntryHeight
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
    public ReadOnlySpan<(int to, float cost)> GetEdges(int nodeIndex)
    {
        _edges.Clear();

        bool connected;
        var x = nodeIndex % EntryWidth;
        var y = nodeIndex / EntryWidth;

        for (var k = 0; k < 8; k++)
        {
            connected = true;

            if (Brush == null)
            {
                // If no path brush is specified, test 8 adjacent nodes.
                connected = !IsGridObstructed(x + DX8[k], y + DY8[k], !IgnoreDynamicObstacles);
            }
            else
            {
                for (var i = 0; i < Brush.DX[k].Length; i++)
                {
                    if (IsGridObstructed(x + Brush.DX[k][i], y + Brush.DY[k][i], !IgnoreDynamicObstacles))
                    {
                        connected = false;
                        break;
                    }
                }
            }

            if (connected)
            {
                var to = GridToIndex(x + DX8[k], y + DY8[k]);
                var cost = (DX8[k] == 0 || DY8[k] == 0) ? 10 : 14;

                _edges.Add((to, cost));
            }
        }

        return _edges;
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
            Graph.Landscape = value;
        }
    }

    private ILandscape landscape;

    /// <summary>
    /// Graph search.
    /// </summary>
    private readonly AStarSearch search = new();

    /// <summary>
    /// Gets the detailed path graph used for searching.
    /// </summary>
    public PathGraph Graph { get; }

    /// <summary>
    /// Dynamic entities.
    /// </summary>
    private readonly List<IMovable> dynamicObstacles = new();

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
    /// Create a circular path brush.
    /// </summary>
    public PathBrush CreateBrush(float radius)
    {
        if (radius < 0)
        {
            throw new ArgumentException();
        }

        var size = (int)(2 * radius / Graph.CellSize);
        if (size < 1)
        {
            size = 1;
        }

        var halfSize = size / 2;
        if (halfSize < 0)
        {
            halfSize = 0;
        }

        radius = halfSize * Graph.CellSize;

        var xPoints = new List<int>();
        var yPoints = new List<int>();

        if (size % 2 == 0)
        {
            // If we have even number of grids
            for (var x = -halfSize; x < halfSize; x++)
            {
                for (var y = -halfSize; y < halfSize; y++)
                {
                    var xx = x * Graph.CellSize + Graph.HalfCellSize;
                    var yy = y * Graph.CellSize + Graph.HalfCellSize;

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
                    var xx = x * Graph.CellSize;
                    var yy = y * Graph.CellSize;

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
        // Add the entity to internal list
        dynamicObstacles.Add(obstacle);

        // Store marked grids in the tag of the obstacle
        obstacle.PathMarks = Graph.EnumerateGridsInBrush(new(obstacle.Position.X, obstacle.Position.Y), obstacle.Brush).ToList();

        // Change graph structure
        foreach (var p in obstacle.PathMarks)
        {
            Graph.MarkDynamic(p.X, p.Y);
        }
    }

    /// <summary>
    /// Removes an entity.
    /// </summary>
    public void RemoveMovable(IMovable obstacle)
    {
        // Removes the entity from internal list
        for (var i = 0; i < dynamicObstacles.Count; i++)
        {
            if (dynamicObstacles[i] == obstacle)
            {
                // Change path graph structure
                foreach (Point p in obstacle.PathMarks)
                {
                    Graph.UnmarkDynamic(p.X, p.Y);
                }

                obstacle.PathMarks = null;
                dynamicObstacles.RemoveAt(i);
                return;
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

        Point midGrid = Graph.PositionToGrid(mid.X, mid.Y);
        var radius = 1 + (int)(Math.Max(max.X - min.X, max.Y - min.Y) / Graph.CellSize / 2);

        Vector2 p;

        for (var x = midGrid.X - radius; x <= midGrid.X + radius; x++)
        {
            for (var y = midGrid.Y - radius; y < midGrid.Y + radius; y++)
            {
                p = Graph.GridToPosition(x, y);
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

        var xMin = (int)(min.X / Graph.CellSize);
        var xMax = (int)(max.X / Graph.CellSize);
        var yMin = (int)(min.Y / Graph.CellSize);
        var yMax = (int)(max.Y / Graph.CellSize);

        var radiusSquared = radius * radius;

        var counter = 0;
        Vector2 center;
        for (var y = yMin; y <= yMax; y++)
        {
            for (var x = xMin; x <= xMax; x++)
            {
                center.X = x * Graph.CellSize + Graph.CellSize / 2;
                center.Y = y * Graph.CellSize + Graph.CellSize / 2;

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
        Graph = new PathGraph(landscape, Resolution, occluders);
    }

    public void Mark(IMovable agent)
    {
        foreach (var p in agent.PathMarks)
        {
            Graph.MarkDynamic(p.X, p.Y);
        }
    }

    public void Unmark(IMovable agent)
    {
        foreach (var p in agent.PathMarks)
        {
            Graph.UnmarkDynamic(p.X, p.Y);
        }
    }

    public void Mark(IEnumerable<Point> staticMarks)
    {
        foreach (var p in staticMarks)
        {
            Graph.Mark(p.X, p.Y);
        }
    }

    public void Unmark(IEnumerable<Point> staticMarks)
    {
        foreach (var p in staticMarks)
        {
            Graph.Unmark(p.X, p.Y);
        }
    }

    /// <summary>
    /// Gets the unobstructed grid that are nearest to the specified point.
    /// </summary>
    public Vector2 FindValidPosition(Vector2 position, PathBrush brush)
    {
        var x = (int)(position.X / Graph.CellSize);
        var y = (int)(position.Y / Graph.CellSize);

        // First check the input grid
        if (!Graph.IsBrushObstructed(position.X, position.Y, brush, true))
        {
            return position;
        }

        // look up its adjancent grids
        foreach (var p in EnumerateGridsInnerOut(x, y, 512))
        {
            if (p.X >= 0 && p.X < Graph.EntryWidth &&
                p.Y >= 0 && p.Y < Graph.EntryHeight)
            {
                position = Graph.GridToPosition(p.X, p.Y);
                if (!Graph.IsBrushObstructed(position.X, position.Y, brush, true))
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
                lastDirection *= distance + 1.2f * Graph.CellSize;
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
                distance += Graph.CellSize * 1.2f;
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
    /// Tests to see if the specified grids can be placed at a given location.
    /// </summary>
    public bool CanBePlacedAt(IEnumerable<Point> grids, bool includingDynamics)
    {
        foreach (var p in grids)
        {
            if (Graph.IsGridObstructed(p.X, p.Y, includingDynamics))
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
        Unmark(agent);

        foreach (Point p in Graph.EnumerateGridsInBrush(new Vector2(x, y), agent.Brush))
        {
            if (Graph.IsGridObstructed(p.X, p.Y, !agent.IgnoreDynamicObstacles))
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
        Unmark(agent);

        var step = Vector2.Subtract(end, start);
        step.Normalize();
        step *= Graph.CellSize * 0.5f;

        var firstGrid = true;
        var steps = (int)(Vector2.Subtract(end, start).Length() / step.Length());

        for (var i = 0; i < steps; i++)
        {
            // Make it more precise on the first grid
            if (firstGrid)
            {
                for (var k = 0; k < 5; k++)
                {
                    if (Graph.IsBrushObstructed(start.X, start.Y, agent.Brush, includeDynamic))
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
                if (Graph.IsBrushObstructed(start.X, start.Y, agent.Brush, includeDynamic))
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
        Point startGrid = Graph.PositionToGrid(start.X, start.Y, brush);
        Point endGrid = Graph.PositionToGrid(end.X, end.Y, brush);

        // Compute the boundary
        Rectangle bounds;
        bounds.Width = AreaSize;
        bounds.Height = AreaSize;

        bounds.X = (endGrid.X + startGrid.X) / 2 - (int)(bounds.Width * FactorToStart);
        bounds.Y = (endGrid.Y + startGrid.Y) / 2 - (int)(bounds.Height * FactorToStart);

        // Creates a new path query
        var query = new PathQuery(Graph.GridToIndex(startGrid.X, startGrid.Y),
                                        Graph.GridToIndex(endGrid.X, endGrid.Y),
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
        Graph.ClearDynamic();

        // Change the structure of the graph if any dynamic obstacle moves
        foreach (var obstacle in dynamicObstacles)
        {
            // Gets grids from brush
            obstacle.PathMarks.Clear();
            obstacle.PathMarks.AddRange(Graph.EnumerateGridsInBrush(new(obstacle.Position.X, obstacle.Position.Y), obstacle.Brush));
            // Mark new grids
            foreach (Point p in obstacle.PathMarks)
            {
                Graph.MarkDynamic(p.X, p.Y);
            }
        }
    }

    /// <summary>
    /// Manually update the grid info after you changed its position.
    /// </summary>
    public void UpdateMovable(IMovable obstacle)
    {
        // Unmark previous grids
        foreach (Point p in obstacle.PathMarks)
        {
            Graph.UnmarkDynamic(p.X, p.Y);
        }

        // Gets grids from brush
        obstacle.PathMarks.Clear();
        obstacle.PathMarks.AddRange(Graph.EnumerateGridsInBrush(new(obstacle.Position.X, obstacle.Position.Y), obstacle.Brush));

        // Mark new grids
        foreach (Point p in obstacle.PathMarks)
        {
            Graph.MarkDynamic(p.X, p.Y);
        }
    }

    private void UpdateSearch()
    {
        // Do nothing if there's no pending requests
        if (pendingRequestsCount <= 0)
        {
            return;
        }

        while (pendingRequestsCount > 0)
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
            Graph.Boundary = query.Boundary;
            Graph.IgnoreDynamicObstacles = query.Obstacle.IgnoreDynamicObstacles;

            // Checks if current query is interrupted
            if (query != currentQuery)
            {
                // Adjust graph brush
                Graph.Brush = query.Obstacle.Brush;

                // Refresh current query
                currentQuery = query;
            }

            Unmark(query.Obstacle);

            // Handle current query
            var result = search.Search(Graph, query.Start, query.End);

            Mark(query.Obstacle);

            // If the search completed, we can stop dealing with this
            // query and notify the PathFound event
            // No query now
            currentQuery = null;

            if (result.Length > 0)
            {
                // Path found
                // It turns out that entities gets stucked due to
                // path simplification and smoothing :(
                GraphPath path = BuildPath(result, Graph, query.Obstacle, true);

                // Append the destination
                path.Edges.AddLast(new GraphPathEdge(query.Destination));

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
        }

        // Set graph bounds to null whether we're not searching
        Graph.Boundary = null;
    }

    /// <summary>
    /// Converts from graph path (Accessed by index) to realworld path (Accessed by position).
    /// </summary>
    private GraphPath BuildPath(ReadOnlySpan<int> path, PathGraph graph, IMovable agent, bool simplifyPath)
    {
        var previous = new Point();
        var resultPath = new GraphPath();
        var brush = agent?.Brush;

        // Ignore first edge
        var firstEdge = true;

        var direction = -1;
        foreach (var i in path)
        {
            // Note how we invert the path.
            // Since the path from graph search are actually from the end to start.
            Point p = graph.IndexToGrid(i);

            var edge = new GraphPathEdge(graph.GridToPosition(p.X, p.Y, brush));

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

    public void SmoothPath(ref GraphPath path, IMovable agent)
    {
        LinkedListNode<GraphPathEdge> next;
        LinkedListNode<GraphPathEdge> remove;
        LinkedListNode<GraphPathEdge> current = path.Edges.First;

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
}
