// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        return _textures.GetOrAdd(path, path =>
        {
            using var stream = File.OpenRead(path);
            var texture = Texture2D.FromStream(_graphicsDevice, stream);
            return CreateMipMaps(texture);
        });
    }

    public static (Color[], int width, int height) ReadAllPixels(string path)
    {
        using var stream = File.OpenRead(path);
        using var bitmap = SKBitmap.Decode(stream);
        var pixels = MemoryMarshal.Cast<SKColor, Color>(bitmap.Pixels).ToArray();
        return (pixels, bitmap.Width, bitmap.Height);
    }

    private Texture2D CreateMipMaps(Texture2D texture)
    {
        var width = texture.Width;
        var height = texture.Height;

        if (!IsPowerOfTwo(width) || !IsPowerOfTwo(height))
        {
            return texture;
        }

        var level = 0;
        var startIndex = 0;
        var buffer1 = new Color[width * height];
        var buffer2 = new Color[width * height / 4];
        var result = new Texture2D(_graphicsDevice, width, height, true, SurfaceFormat.Color);

        while (width > 1 || height > 1)
        {
            if (level == 0)
            {
                texture.GetData(buffer1, startIndex, width * height);
                result.SetData(level, null, buffer1, 0, width * height);
                texture.Dispose();
            }
            else
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var a = buffer2[(y * 2) * width * 2 + x * 2];
                        var b = buffer2[(y * 2 + 1) * width * 2 + x * 2];
                        var c = buffer2[(y * 2) * width * 2 + x * 2 + 1];
                        var d = buffer2[(y * 2 + 1) * width * 2 + x * 2 + 1];

                        buffer1[y * width + x] = new(
                            (a.R + b.R + c.R + d.R) / 4,
                            (a.G + b.G + c.G + d.G) / 4,
                            (a.B + b.B + c.B + d.B) / 4,
                            (a.A + b.A + c.A + d.A) / 4);
                    }
                }
                result.SetData(level, null, buffer1, 0, width * height);
            }

            level++;
            width /= 2;
            height /= 2;
            (buffer1, buffer2) = (buffer2, buffer1);
        }

        return result;

        static bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;
    }
}
