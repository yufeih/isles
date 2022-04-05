using System.Collections;
using System.Text;
using System.Xml;
using Xunit;

namespace Isles;

public class SvgBuilder
{
    private static readonly bool s_isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
    private static readonly string s_baseDirectory = FindRepositoryRoot();

    private readonly Random _random = new(0);
    private readonly List<(float x, float y, float r, List<(float x, float y)> animations, Dictionary<string, string> data)> _circles = new();
    private readonly List<(float x, float y, float w, float h, string? color)> _rectangles = new();
    private readonly List<(Vector2[] lines, float w)> _lineSegments = new();
    private readonly List<(int w, int h, float step)> _grids = new();

    public void AddCircle(float x, float y, float radius)
    {
        _circles.Add((x, y, radius, new(), new()));
    }

    public void AnimateCircle(int index, float x, float y)
    {
        _circles[index].animations.Add((x, y));
    }

    public void SetCircleData(int index, string key, string value)
    {
        _circles[index].data[key] = value;
    }

    public void AddRectangle(float x, float y, float w, float h, string? color = null)
    {
        _rectangles.Add((x, y, w, h, color));
    }

    public void AddLineSegments(Vector2[] lineSegments, float width)
    {
        if (lineSegments.Length <= 1)
        {
            return;
        }
        _lineSegments.Add((lineSegments, width));
    }

    public void AddGrid(PathGrid grid)
    {
        _grids.Add((grid.Width, grid.Height, grid.Step));

        for (var i = 0; i < grid.Bits.Length; i++)
        {
            if (grid.Bits[i])
            {
                var x = i % grid.Width;
                var y = i / grid.Width;
                AddRectangle(x * grid.Step, y * grid.Step, grid.Step, grid.Step, "#aaa");
            }
        }
    }

    public void Snapshot(string name, float duration = default)
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

        var viewBox = ComputeViewBox();
        xml.WriteAttributeString("viewBox", $"{viewBox.x} {viewBox.y} {viewBox.w} {viewBox.h}");

        foreach (var (w, h, step) in _grids)
        {
            xml.WriteStartElement("path");
            xml.WriteAttributeString("stroke", "#ccc");
            xml.WriteAttributeString("stroke-width", $"{step / 100f}");
            var hLines = string.Join(" ", Enumerable.Range(0, h + 1).Select(i => $"M {0},{step * i} h {step * w}"));
            var vLines = string.Join(" ", Enumerable.Range(0, w + 1).Select(i => $"M {step * i},{0} v {step * h}"));
            xml.WriteAttributeString("d", $"{hLines} {vLines}");
            xml.WriteEndElement();
        }

        foreach (var (x, y, r, animations, data) in _circles)
        {
            xml.WriteStartElement("circle");
            xml.WriteAttributeString("r", r.ToString());
            xml.WriteAttributeString("cx", x.ToString());
            xml.WriteAttributeString("cy", y.ToString());
            xml.WriteAttributeString("fill", NextColor());

            foreach (var (key, value) in data)
            {
                xml.WriteAttributeString($"data-{key}", value);
            }

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

        foreach (var (x, y, w, h, color) in _rectangles)
        {
            xml.WriteStartElement("rect");
            xml.WriteAttributeString("x", x.ToString());
            xml.WriteAttributeString("y", y.ToString());
            xml.WriteAttributeString("width", w.ToString());
            xml.WriteAttributeString("height", h.ToString());
            xml.WriteAttributeString("fill", color ?? NextColor());
            xml.WriteEndElement();
        }

        foreach (var (lines, width) in _lineSegments)
        {
            xml.WriteStartElement("path");
            xml.WriteAttributeString("fill", "none");
            xml.WriteAttributeString("stroke", NextColor());
            xml.WriteAttributeString("stroke-width", width.ToString());
            var value = string.Join(" ", lines.Skip(1).Select(i => $"L {i.X},{i.Y}"));
            xml.WriteAttributeString("d", $"M {lines[0].X},{lines[0].Y} {value}");
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
            _rectangles.SelectMany(r => new[] { r.x, r.x + r.w })).Concat(
            _grids.SelectMany(g => new[]{ 0, g.w * g.step }));

        var y = _circles.Select(c => c.y).Concat(
            _circles.SelectMany(c => c.animations.Select(a => a.y))).Concat(
            _rectangles.SelectMany(r => new[] { r.y, r.y + r.h })).Concat(
            _grids.SelectMany(g => new[]{ 0, g.h * g.step }));

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

    private string NextColor()
    {
        return $"#{Convert.ToHexString(BitConverter.GetBytes(_random.Next()), 0, 3)}";
    }
}
