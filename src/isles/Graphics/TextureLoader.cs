// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using SkiaSharp;

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

    private unsafe Texture2D LoadTextureCore(string path)
    {
        var pixels = ReadAllPixels(path, out var width, out var height);
        var mipmap = IsPowerOfTwo(width) && IsPowerOfTwo(height);
        var texture = new Texture2D(_graphicsDevice, width, height, mipmap, SurfaceFormat.Color);

        fixed (void* ptr = pixels)
        {
            texture.SetDataPointerEXT(0, null, (IntPtr)ptr, pixels.Length);
        }

        if (mipmap)
        {
            CreateMipMaps(texture, pixels);
        }

        return texture;

        static bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;
    }

    public static Span<Color> ReadAllPixels(string path, out int width, out int height)
    {
        using var stream = File.OpenRead(path);
        using var bitmap = SKBitmap.Decode(stream);
        width = bitmap.Width;
        height = bitmap.Height;
        return SKColorToColor(bitmap.Pixels);
    }

    private static unsafe Texture2D CreateMipMaps(Texture2D texture, ReadOnlySpan<Color> pixels)
    {
        var level = 1;
        var width = texture.Width / 2;
        var height = texture.Height / 2;

        var source = ArrayPool<Color>.Shared.Rent(width * height / 4);
        var target = ArrayPool<Color>.Shared.Rent(width * height);

        while (width > 1 || height > 1)
        {
            var ww = width * 2;
            var src = level == 1 ? pixels : source;
            for (var y = 0; y < height; y++)
            {
                var yy = y * 2;
                for (var x = 0; x < width; x++)
                {
                    var xx = x * 2;
                    var a = src[yy * ww + xx];
                    var b = src[(yy + 1) * ww + xx];
                    var c = src[yy * ww + xx + 1];
                    var d = src[(yy + 1) * ww + xx + 1];

                    target[y * width + x] = new(
                        (a.R + b.R + c.R + d.R) / 4,
                        (a.G + b.G + c.G + d.G) / 4,
                        (a.B + b.B + c.B + d.B) / 4,
                        (a.A + b.A + c.A + d.A) / 4);
                }
            }

            fixed (void* ptr = target)
            {
                texture.SetDataPointerEXT(level, null, (IntPtr)ptr, width * height);
            }

            level++;
            width /= 2;
            height /= 2;

            (source, target) = (target, source);
        }

        ArrayPool<Color>.Shared.Return(source);
        ArrayPool<Color>.Shared.Return(target);

        return texture;
    }

    private static Span<Color> SKColorToColor(Span<SKColor> pixels)
    {
        // ARGB --> ABGR
        foreach (ref var pixel in MemoryMarshal.Cast<SKColor, uint>(pixels))
        {
            pixel = ((pixel >> 16) & 0x000000FF) |
                    ((pixel << 16) & 0x00FF0000) |
                    (pixel & 0xFF00FF00);
        }

        return MemoryMarshal.Cast<SKColor, Color>(pixels);
    }
}
