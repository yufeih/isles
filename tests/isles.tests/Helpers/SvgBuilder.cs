using System.Text;
using System.Xml;
using Xunit;

namespace isles.tests;

public class SvgBuilder
{
    private static readonly bool s_isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
    private static readonly string s_baseDirectory = FindRepositoryRoot();

    private readonly List<(float x, float y, float r, List<(float x, float y)> animations)> _circles = new();
    private readonly List<(float x, float y, float w, float h)> _rectangles = new();

    public void AddCircle(float x, float y, float radius)
    {
        _circles.Add((x, y, radius, new()));
    }

    public void AnimateCircle(int index, float x, float y)
    {
        _circles[index].animations.Add((x, y));
    }

    public void AddRectangle(float x, float y, float w, float h)
    {
        _rectangles.Add((x, y, w, h));
    }

    public void Snapshot(string name, float duration)
    {
        var path = Path.Combine(s_baseDirectory, "tests", "snapshots", name);
        var expected = File.Exists(path) ? File.ReadAllText(path) : null;
        var actual = Build(duration);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, actual);

        if (expected is null && s_isCI)
        {
            throw new InvalidOperationException($"Cannot find snapshot file '{path}'");
        }

        if (expected is null)
        {
            return;
        }

        try
        {
            Assert.Equal(expected, actual, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }
        catch when (s_isCI)
        {
            var failedPath = Path.Combine(s_baseDirectory, "tests", "failed", name);
            Directory.CreateDirectory(Path.GetDirectoryName(failedPath)!);
            File.WriteAllText(failedPath, actual);
            throw;
        }
    }

    private string Build(float duration)
    {
        var sb = new StringBuilder();
        using var xml = XmlWriter.Create(new StringWriter(sb), new() { Indent = true, OmitXmlDeclaration = true });
        xml.WriteStartElement("svg", ns: "http://www.w3.org/2000/svg");

        var random = new Random(0);
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
                xml.WriteAttributeString("calcMode", "discrete");
                xml.WriteAttributeString("values", string.Join(';', animations.Select(a => a.x)));
                xml.WriteEndElement();

                xml.WriteStartElement("animate");
                xml.WriteAttributeString("attributeName", "cy");
                xml.WriteAttributeString("dur", $"{duration}s");
                xml.WriteAttributeString("repeatCount", "indefinite");
                xml.WriteAttributeString("calcMode", "discrete");
                xml.WriteAttributeString("values", string.Join(';', animations.Select(a => a.y)));
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }

        foreach (var (x, y, w, h) in _rectangles)
        {
            xml.WriteStartElement("rect");
            xml.WriteAttributeString("x", x.ToString());
            xml.WriteAttributeString("y", y.ToString());
            xml.WriteAttributeString("width", w.ToString());
            xml.WriteAttributeString("height", h.ToString());
            xml.WriteAttributeString("fill", $"#{Convert.ToHexString(BitConverter.GetBytes(random.Next()), 0, 3)}");
            xml.WriteEndElement();
        }

        xml.WriteEndElement();
        xml.Flush();

        return sb.ToString();
    }

    private (float x, float y, float w, float h) ComputeViewBox()
    {
        var x = _circles.Select(c => c.x).Concat(
            _circles.SelectMany(c => c.animations.Select(a => a.x))).Concat(
            _rectangles.SelectMany(r => new[] { r.x, r.x + r.w }));

        var y = _circles.Select(c => c.y).Concat(
            _circles.SelectMany(c => c.animations.Select(a => a.y))).Concat(
            _rectangles.SelectMany(r => new[] { r.y, r.y + r.h }));

        var r = _circles.Select(c => c.r).DefaultIfEmpty().Max();

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
        return directory!;
    }
}
