// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Isles.NativeMethods;

namespace Isles;

public class NavMesh
{
    private ArrayBuilder<Vector2> _vertices;
    private ArrayBuilder<int> _polylines;
    private ArrayBuilder<ushort> _triangles;

    public ReadOnlySpan<Vector2> Vertices => _vertices;
    public ReadOnlySpan<int> Polylines => _polylines;
    public ReadOnlySpan<ushort> Triangles => _triangles;

    public NavMesh(PathGrid grid)
    {
        _vertices.Add(new(0, 0));
        _vertices.Add(new(grid.Width * grid.Step, 0));
        _vertices.Add(new(grid.Width * grid.Step, grid.Height * grid.Step));
        _vertices.Add(new(0, grid.Height * grid.Step));
        _polylines.Add(4);

        for (var y = 0; y < grid.Height; y++)
            for (var x = 0; x < grid.Width; x++)
                if (grid.Bits[x + grid.Width * y])
                {
                    _vertices.AddRange(stackalloc Vector2[]
                    {
                        new(x * grid.Step, y * grid.Step),
                        new((x + 1) * grid.Step, y * grid.Step),
                        new((x + 1) * grid.Step, (y + 1) * grid.Step),
                        new(x * grid.Step, (y + 1) * grid.Step),
                    });
                    _polylines.Add(4);
                }

        Triangulate(_vertices, _polylines);
    }

    private unsafe void Triangulate(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> polylines)
    {
        var sum = 0;
        foreach (var count in polylines)
        {
            if (count < 3)
                throw new ArgumentOutOfRangeException(nameof(polylines));
            sum += count;
        }

        if (sum > vertices.Length)
            throw new ArgumentOutOfRangeException(nameof(polylines));

        IntPtr polygon;

        fixed (Vector2* pVertices = vertices)
        fixed (int* pPolylines = polylines)
        {
            polygon = navmesh_polygon_new(pPolylines, polylines.Length, pVertices);
        }

        _triangles.Clear();
        var length = navmesh_polygon_triangulate(polygon, out var indices);
        for (var i = 0; i < length; i++)
        {
            _triangles.Add(indices[i]);
        }

        navmesh_polygon_delete(polygon);
    }
}