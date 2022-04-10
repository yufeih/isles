using SkiaSharp;
using Xunit;

namespace Isles;

public class PlotLineTest
{
    [Fact]
    public static void WuLine()
    {
        DrawLine("wu", (bitmap, x0, y0, x1, y1) =>
        {
            PlotLine.WuLine(x0, y0, x1, y1, (x, y, a, _) => {
                bitmap.SetPixel(x, y, SKColors.Black.WithAlpha((byte)(255 * a))); 
                return false;
            }, 0);
        });
    }

    [Fact]
    public static void BresenhamLine()
    {
        DrawLine("bresenham", (bitmap, x0, y0, x1, y1) =>
        {
            PlotLine.BresenhamLine(x0, y0, x1, y1, (x, y, _) => {
                bitmap.SetPixel(x, y, SKColors.Black);
                return false;
            }, 0);
        });
    }

    private static void DrawLine(string name, Action<SKBitmap, int, int, int, int> draw)
    {
        var random = new Random(1);
        var size = 128;
        var bitmap = new SKBitmap(size, size);

        draw(bitmap, 0, 0, 10, 10);
        draw(bitmap, 10, 0, 8, 0);
        draw(bitmap, size - 1, 0, size - 1, 10);

        draw(bitmap, 70, 70, 100, 80);
        draw(bitmap, 70, 50, 120, 40);

        for (var i = 0; i < 10; i++)
        {
            draw(bitmap, random.Next(size), random.Next(size), random.Next(size), random.Next(size));
        }

        File.WriteAllBytes(Path.Combine(Snapshot.SnapshotDirectory, $"move/plotline-{name}.png"), bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray());
    }
}
