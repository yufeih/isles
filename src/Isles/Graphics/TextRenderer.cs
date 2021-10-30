// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace Isles.Graphics
{
    public class TextRenderer
    {
        private readonly GraphicsDevice _graphics;
        private readonly SKPaint _paint;
        private readonly LruCache<string, Texture2D> _textures = new(capacity: 100);

        public TextRenderer(GraphicsDevice graphics, string fontPath, float size)
        {
            _graphics = graphics;
            _paint = new(new(SKTypeface.FromFile(fontPath), size))
            {
                IsAntialias = true,
                IsStroke = false,
                Color = SKColors.White,
            };
        }

        public Vector2 MeasureString(string text)
        {
            return new(_paint.MeasureText(text), _paint.FontSpacing);
        }

        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1)
        {
            var texture = _textures.GetOrAdd(text, GetTextureCore);
            spriteBatch.Draw(texture, position, null, color, 0, Vector2.Zero, scale, default, 0);
        }

        private Texture2D GetTextureCore(string text)
        {
            var bounds = new SKRect();
            var width = (int)Math.Ceiling(_paint.MeasureText(text, ref bounds));
            var height = (int)_paint.TextSize;
            var texture = new Texture2D(_graphics, width, height);

            var info = new SKImageInfo(width, height);
            var bytes = new byte[info.BytesSize];
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            using var surface = SKSurface.Create(info, handle.AddrOfPinnedObject(), info.RowBytes);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.DrawText(text, -bounds.Left, -bounds.Top, _paint);

            canvas.Flush();
            texture.SetData(bytes);
            handle.Free();

            return texture;
        }
    }
}
