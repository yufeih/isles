// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using SkiaSharp;
using Svg.Skia;

namespace Isles.Graphics;

public class TextureLoader
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ConcurrentDictionary<string, Texture2D> _textures = new();

    public TextureLoader(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public Texture2D LoadTexture(string path)
    {
        return _textures.GetOrAdd(path, LoadTextureCore);
    }

    private Texture2D LoadTextureCore(string path)
    {
        using var stream = File.OpenRead(path);
        using var bitmap = path.EndsWith(".svg") ? LoadSvg(stream) : SKBitmap.Decode(stream);

        var pixels = bitmap.Pixels;
        SKColorToColor(pixels);

        var mipmap = IsPowerOfTwo(bitmap.Width) && IsPowerOfTwo(bitmap.Height);
        var texture = new Texture2D(_graphicsDevice, bitmap.Width, bitmap.Height, mipmap, SurfaceFormat.Color);
        texture.SetData(pixels);

        if (mipmap)
        {
            CreateMipMaps(texture, pixels);
        }

        return texture;

        static bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;

        static SKBitmap LoadSvg(Stream stream)
        {
            using var svg = new SKSvg();
            return svg.Load(stream)!.ToBitmap(SKColors.Transparent, 1, 1, SKColorType.Rgba8888, SKAlphaType.Premul, null);
        }
    }

    public static (Color[], int width, int height) ReadAllPixels(string path)
    {
        using var stream = File.OpenRead(path);
        using var bitmap = SKBitmap.Decode(stream);
        var pixels = MemoryMarshal.Cast<SKColor, Color>(bitmap.Pixels).ToArray();
        return (pixels, bitmap.Width, bitmap.Height);
    }

    private static Texture2D CreateMipMaps(Texture2D texture, SKColor[] pixels)
    {
        var level = 1;
        var width = texture.Width / 2;
        var height = texture.Height / 2;
        var bufferLarge = pixels;
        var bufferSmall = ArrayPool<SKColor>.Shared.Rent(width * height);

        while (width > 1 || height > 1)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var a = bufferLarge[(y * 2) * width * 2 + x * 2];
                    var b = bufferLarge[(y * 2 + 1) * width * 2 + x * 2];
                    var c = bufferLarge[(y * 2) * width * 2 + x * 2 + 1];
                    var d = bufferLarge[(y * 2 + 1) * width * 2 + x * 2 + 1];

                    bufferSmall[y * width + x] = new(
                        (byte)((a.Red + b.Red + c.Red + d.Red) / 4),
                        (byte)((a.Green + b.Green + c.Green + d.Green) / 4),
                        (byte)((a.Blue + b.Blue + c.Blue + d.Blue) / 4),
                        (byte)((a.Alpha + b.Alpha + c.Alpha + d.Alpha) / 4));
                }
            }
            texture.SetData(level, null, bufferSmall, 0, width * height);

            level++;
            width /= 2;
            height /= 2;
            (bufferSmall, bufferLarge) = (bufferLarge, bufferSmall);
        }

        ArrayPool<SKColor>.Shared.Return(pixels == bufferSmall ? bufferLarge : bufferSmall);

        return texture;
    }

    private static Span<Color> SKColorToColor(SKColor[] pixels)
    {
        // ARGB --> ABGR
        foreach (ref var pixel in MemoryMarshal.Cast<SKColor, uint>(pixels))
        {
            pixel = ((pixel >> 16) & 0x000000ff) |
                    ((pixel << 16) & 0x00ff0000) |
                    (pixel & 0xff00ff00);
        }

        return MemoryMarshal.Cast<SKColor, Color>(pixels);
    }
}
