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
            new Vector2[] { new(40,40), new(50,40), new(50,50), new(40,50) },
        };
        
        var svg = new SvgBuilder();
        foreach (var polylines in polygon)
        {
            svg.AddPolygon(polylines);
        }

        var navMesh = new NavMesh();
        var vertices = polygon.SelectMany(p => p).ToArray();
        var tris = navMesh.Triangulate(vertices, polygon.Select(p => p.Length).ToArray()).ToArray();
        for (var i = 0; i < tris.Length; i += 3)
        {
            svg.AddPolygon(new[]{ vertices[tris[i]], vertices[tris[i + 1]], vertices[tris[i + 2]]});
        }

        Snapshot.Save($"move/navmesh-trangulate.svg", svg.ToString());
    }
}
