using Xunit;

namespace Isles;

public class PolyMapTest
{
    private const string polygon = "M45.73883056640625 138.95689392089844 L0 78.06947326660156 L32.2603759765625 23.063064575195312 L139.53857421875 0 L285.9595947265625 48.86119079589844 L282.64471435546875 125.54225158691406 L241.6610107421875 125.54225158691406 L255.92633056640625 52.98771667480469 L138.12432861328125 29.569625854492188 L59.495361328125 31.008102416992188 L26.062255859375 79.04753112792969 L66.36138916015625 128.5947723388672 L308.52471923828125 155.73069763183594 L301.2427978515625 270.01731872558594 L69.04193115234375 263.0497283935547 L29.75628662109375 224.87522888183594 L54.42364501953125 208.7491912841797 L79.8546142578125 232.8672332763672 L266.08453369140625 235.75486755371094 L269.6763916015625 178.45310974121094 L104.68902587890625 156.4392547607422";

    //private const string polygon = "M0 33.98651123046875 L17.28155517578125 0 L141.84063720703125 2.33221435546875 L173.46612548828125 27.56231689453125";

    [Fact]
    public static void BuildVisibilityMap()
    {
        var points = polygon.Split(' ').Select(s => float.Parse(s.Trim('M').Trim('L'))).ToArray();
        var vertices = Enumerable.Range(0, points.Length / 2).Select(i => new Vector2(points[i * 2], points[i * 2 + 1])).ToArray();
        var eye = new Vector2(200, 400);
        var map = PolyMap.BuildVisibilityMap(vertices, eye);

        var svg = new SvgBuilder();
        svg.AddPolygon(vertices);

        svg.AddCircle(eye.X, eye.Y, 2, "green");
        foreach (var i in map)
        {
            svg.AddCircle(vertices[i].X, vertices[i].Y, 2, "red");
        }

        Snapshot.Save($"move/polymap.svg", svg.ToString());
    }
}
