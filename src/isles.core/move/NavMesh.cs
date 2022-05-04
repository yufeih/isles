// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Isles.NativeMethods;

namespace Isles;

public class NavMesh
{
    private ArrayBuilder<ushort> _triangles;

    public unsafe ReadOnlySpan<ushort> Triangulate(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<int> polylines)
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

        return _triangles;
    }
}