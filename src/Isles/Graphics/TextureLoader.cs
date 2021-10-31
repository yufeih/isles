// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace Isles.Graphics
{
    public class TextureLoader
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly ConcurrentDictionary<string, Texture2D> _textures = new();

        public TextureLoader(GraphicsDevice graphicsDevice) => _graphicsDevice = graphicsDevice;

        public Texture2D LoadTexture(string path)
        {
            return _textures.GetOrAdd(path, path =>
            {
                using var stream = File.OpenRead(path);
                return Texture2D.FromStream(_graphicsDevice, stream);
            });
        }

        public static (Color[], int width, int height) ReadAllPixels(string path)
        {
            using var stream = File.OpenRead(path);
            using var bitmap = SKBitmap.Decode(stream);
            var pixels = MemoryMarshal.Cast<SKColor, Color>(bitmap.Pixels).ToArray();
            return (pixels, bitmap.Width, bitmap.Height);
        }
    }
}
