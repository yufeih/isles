using System.Xml;

namespace isles.tests;

public class SvgBuilder
{
    private static readonly string s_baseDirectory = Path.Combine(FindRepositoryRoot(), "tests/svg");

    private readonly List<(float x, float y, float r, List<(float x, float y)> animations)> _circles = new();

    public void AddCircle(float x, float y, float radius)
    {
        _circles.Add((x, y, radius, new()));
    }

    public void AnimateCircle(int index, float x, float y)
    {
        _circles[index].animations.Add((x, y));
    }

    public void Save(string path, float duration)
    {
        path = Path.Combine(s_baseDirectory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

        using var xml = XmlWriter.Create(path, new() { Indent = true, OmitXmlDeclaration = true });
        xml.WriteStartElement("svg", ns: "http://www.w3.org/2000/svg");

        var random = new Random(999);
        var viewBox = ComputeViewBox();
        xml.WriteAttributeString("viewBox", $"{viewBox.x} {viewBox.y} {viewBox.w} {viewBox.h}");

        foreach (var (x, y, r, animations) in _circles)
        {
            xml.WriteStartElement("circle");
            xml.WriteAttributeString("r", r.ToString());
            xml.WriteAttributeString("cx", x.ToString());
            xml.WriteAttributeString("cy", y.ToString());
            xml.WriteAttributeString("fill", $"#{Convert.ToHexString(BitConverter.GetBytes(random.Next()), 0, 3)}");

            if (animations.Count > 0)
            {
                xml.WriteStartElement("animate");
                xml.WriteAttributeString("attributeName", "cx");
                xml.WriteAttributeString("dur", $"{duration}s");
                xml.WriteAttributeString("repeatCount", "indefinite");
                xml.WriteAttributeString("values", string.Join(';', animations.Select(a => a.x)));
                xml.WriteEndElement();

                xml.WriteStartElement("animate");
                xml.WriteAttributeString("attributeName", "cy");
                xml.WriteAttributeString("dur", $"{duration}s");
                xml.WriteAttributeString("repeatCount", "indefinite");
                xml.WriteAttributeString("values", string.Join(';', animations.Select(a => a.y)));
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }

        xml.WriteEndElement();
    }

    private (float x, float y, float w, float h) ComputeViewBox()
    {
        var x = _circles.Select(c => c.x).Concat(_circles.SelectMany(c => c.animations.Select(a => a.x)));
        var y = _circles.Select(c => c.y).Concat(_circles.SelectMany(c => c.animations.Select(a => a.y)));
        var r = _circles.Select(c => c.r).Max();

        var minX = x.Min() - r;
        var minY = y.Min() - r;
        var maxX = x.Max() + r;
        var maxY = y.Max() + r;

        return (minX, minY, (maxX - minX), (maxY - minY));
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !Directory.Exists(Path.Combine(directory, ".git")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory;
    }
}