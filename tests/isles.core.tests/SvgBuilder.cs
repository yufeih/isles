using System.Text;
using System.Xml;

namespace Isles;

public class SvgData
{
    public float? Opacity { get; init; }

    public Dictionary<string, string>? Attributes { get; init; }

    public float Duration { get; init; }

    public Vector2[]? Animations { get; init; }
}

public class SvgBuilder
{
    private readonly Random _random = new(0);
    private readonly StringBuilder _body = new();
    private readonly XmlWriter _xml;

    private (float minX, float minY, float maxX, float maxY) _viewBox;

    public SvgBuilder()
    {
        _xml = XmlWriter.Create(new StringWriter(_body), new() { Indent = true, ConformanceLevel = ConformanceLevel.Fragment });
    }

    public void AddCircle(float x, float y, float radius, string? color = null, SvgData? data = null)
    {
        _xml.WriteStartElement("circle");
        _xml.WriteAttributeString("r", radius.ToString());
        _xml.WriteAttributeString("cx", x.ToString());
        _xml.WriteAttributeString("cy", y.ToString());
        _xml.WriteAttributeString("fill", color ?? NextColor());
        WriteData(data);
        _xml.WriteEndElement();

        UpdateViewBox(x - radius, y - radius, x + radius, y + radius, data);
    }

    public void AddRectangle(float x, float y, float w, float h, string? color = null, SvgData? data = null)
    {
        _xml.WriteStartElement("rect");
        _xml.WriteAttributeString("x", x.ToString());
        _xml.WriteAttributeString("y", y.ToString());
        _xml.WriteAttributeString("width", w.ToString());
        _xml.WriteAttributeString("height", h.ToString());
        _xml.WriteAttributeString("fill", color ?? NextColor());
        WriteData(data);
        _xml.WriteEndElement();

        UpdateViewBox(x, y, x + w, y + h, data);
    }

    public void AddPolygon(Vector2[] vertices, float width = 1, string? color = null, SvgData? data = null)
    {
        AddLines(vertices, width, color, loop: true, data);
    }

    public void AddLines(Vector2[] points, float width = 1, string? color = null, SvgData? data = null)
    {
        AddLines(points, width, color, loop: false, data);
    }

    private void AddLines(Vector2[] points, float width, string? color, bool loop, SvgData? data)
    {
        if (points.Length <= 1)
        {
            return;
        }

        var value = string.Join(" ", points.Skip(1).Select(i => $"L {i.X},{i.Y}"));
        if (loop)
        {
            value += $" L {points[0].X},{points[0].Y}";
        }

        _xml.WriteStartElement("path");
        _xml.WriteAttributeString("fill", "none");
        _xml.WriteAttributeString("stroke", color ?? NextColor());
        _xml.WriteAttributeString("stroke-width", width.ToString());
        _xml.WriteAttributeString("d", $"M {points[0].X},{points[0].Y} {value}");
        WriteData(data);
        _xml.WriteEndElement();

        UpdateViewBox(
            points.Min(p => p.X),
            points.Min(p => p.Y),
            points.Max(p => p.X),
            points.Max(p => p.Y),
            data);
    }

    public void AddGrid(PathGrid grid)
    {
        _xml.WriteStartElement("path");
        _xml.WriteAttributeString("stroke", "#ccc");
        _xml.WriteAttributeString("stroke-width", $"{grid.Step / 100f}");
        var hLines = string.Join(" ", Enumerable.Range(0, grid.Height + 1).Select(i => $"M {0},{grid.Step * i} h {grid.Step * grid.Width}"));
        var vLines = string.Join(" ", Enumerable.Range(0, grid.Width + 1).Select(i => $"M {grid.Step * i},{0} v {grid.Step * grid.Height}"));
        _xml.WriteAttributeString("d", $"{hLines} {vLines}");
        _xml.WriteEndElement();

        for (var i = 0; i < grid.Bits.Length; i++)
        {
            if (grid.Bits[i])
            {
                var x = i % grid.Width;
                var y = i / grid.Width;
                AddRectangle(x * grid.Step, y * grid.Step, grid.Step, grid.Step, "#333");
            }
        }

        UpdateViewBox(0, 0, grid.Width * grid.Step, grid.Height * grid.Step);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        using var xml = XmlWriter.Create(new StringWriter(sb), new() { Indent = true, OmitXmlDeclaration = true });
        xml.WriteStartElement("svg", ns: "http://www.w3.org/2000/svg");

        var viewBox = $"{_viewBox.minX} {_viewBox.minY} {_viewBox.maxX - _viewBox.minX} {_viewBox.maxY - _viewBox.minY}";
        xml.WriteAttributeString("viewBox", viewBox);

        _xml.Flush();
        xml.WriteRaw(Environment.NewLine);
        xml.WriteRaw(_body.ToString());
        xml.WriteRaw(Environment.NewLine);
        xml.WriteEndElement();
        xml.Flush();

        return sb.ToString();
    }

    private void WriteData(SvgData? data)
    {
        if (data is null)
        {
            return;
        }

        if (data.Opacity != null)
        {
            _xml.WriteAttributeString("opacity", data.Opacity.ToString());
        }

        if (data.Attributes != null)
        {
            foreach (var (key, value) in data.Attributes)
            {
                _xml.WriteAttributeString($"data-{key}", value);
            }
        }

        if (data.Animations != null && data.Animations.Length > 0)
        {
            _xml.WriteStartElement("animate");
            _xml.WriteAttributeString("attributeName", "cx");
            _xml.WriteAttributeString("dur", $"{data.Duration}s");
            _xml.WriteAttributeString("repeatCount", "indefinite");
            _xml.WriteAttributeString("calcMode", "discrete");
            _xml.WriteAttributeString("values", string.Join(';', data.Animations.Select(a => a.X)));
            _xml.WriteEndElement();

            _xml.WriteStartElement("animate");
            _xml.WriteAttributeString("attributeName", "cy");
            _xml.WriteAttributeString("dur", $"{data.Duration}s");
            _xml.WriteAttributeString("repeatCount", "indefinite");
            _xml.WriteAttributeString("calcMode", "discrete");
            _xml.WriteAttributeString("values", string.Join(';', data.Animations.Select(a => a.Y)));
            _xml.WriteEndElement();
        }
    }

    private void UpdateViewBox(float minX, float minY, float maxX, float maxY, SvgData? data = null)
    {
        if (data != null && data.Animations != null && data.Animations.Length > 0)
        {
            var w = maxX - minX;
            var h = maxY - minY;
            minX = Math.Min(minX, data.Animations.Min(a => a.X) - w);
            maxX = Math.Max(maxX, data.Animations.Max(a => a.X) + w);
            minY = Math.Min(minY, data.Animations.Min(a => a.Y) - h);
            maxY = Math.Max(maxY, data.Animations.Max(a => a.Y) + h);
        }

        _viewBox = (Math.Min(_viewBox.minX, minX), Math.Min(_viewBox.minY, minY), Math.Max(_viewBox.maxX, maxX), Math.Max(_viewBox.maxY, maxY));
    }

    private string NextColor()
    {
        return $"#{Convert.ToHexString(BitConverter.GetBytes(_random.Next()), 0, 3)}";
    }
}
