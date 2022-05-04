// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Isles.NativeMethods;

namespace Isles;

public class NavMesh
{
    private List<Vector2[]> _polygon;

    private ArrayBuilder<int> _triangles;

    public NavMesh(List<Vector2[]> polygons)
    {
        _polygon = polygons;
    }

    public unsafe ReadOnlySpan<int> Triangulate()
    {
        var polygon = navmesh_new_polygon();

        foreach (var polylines in _polygon)
        {
            fixed (Vector2* vertices = polylines)
            {
                navmesh_polygon_add_polylines(polygon, vertices, polylines.Length);
            }
        }

        _triangles.Clear();
        var length = navmesh_polygon_triangulate(polygon, out var indices);
        for (var i = 0; i < length; i++)
        {
            _triangles.Add(indices[i]);
        }

        navmesh_delete_polygon(polygon);

        return _triangles;
    }
}