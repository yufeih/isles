using Xunit;

namespace Isles;

public class NavMeshTests
{
    [Fact]
    public void NavMeshTest()
    {
        var random = new Random(1);
        var grid = TestHelper.CreateRandomGrid(random, 10, 10);
        var navMesh = new NavMesh(grid);
        var svg = new SvgBuilder();

        svg.AddGrid(grid);

        var tris = navMesh.Triangles;
        var vertices = navMesh.Vertices;
        for (var i = 0; i < tris.Length; i += 3)
        {
            svg.AddPolygon(new[]{ vertices[tris[i]], vertices[tris[i + 1]], vertices[tris[i + 2]]});
        }

        Snapshot.Save($"move/navmesh-trangulate.svg", svg.ToString());
    }
}
