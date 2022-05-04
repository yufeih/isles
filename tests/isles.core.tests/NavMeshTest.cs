using Xunit;

namespace Isles;

public class NavMeshTests
{
    [Fact]
    public void NavMeshTest()
    {
        var polygon = new List<Vector2[]>
        {
            new Vector2[] { new(0,0), new(100,0), new(100,100), new(0,100) },
        };
        
        var svg = new SvgBuilder();
        foreach (var polylines in polygon)
        {
            svg.AddPolygon(polylines);
        }

        var navMesh = new NavMesh(polygon);
        var tris = navMesh.Triangulate().ToArray();
        for (var i = 0; i < tris.Length; i += 3)
        {
            svg.AddPolygon(new[]{ polygon[0][tris[i]], polygon[0][tris[i + 1]], polygon[0][tris[i + 2]]});
        }

        Snapshot.Save($"move/navmesh-trangulate.svg", svg.ToString());
    }
}
