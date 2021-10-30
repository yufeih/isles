// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Isles.Graphics
{
    public class ModelPicker<T> : IDisposable
    {
        private const int ObjectMapSize = 512;

        private readonly GraphicsDevice _graphics;
        private readonly ModelRenderer _renderer;
        private readonly DepthStencilBuffer _depthStencil;
        private readonly RenderTarget2D _renderTarget;

        private readonly Dictionary<Color, T> _objectMap = new();
        private readonly Color[] _colors = new Color[ObjectMapSize * ObjectMapSize];

        public ModelPicker(GraphicsDevice graphics, ModelRenderer renderer)
        {
            _graphics = graphics;
            _renderer = renderer;
            _depthStencil = new DepthStencilBuffer(_graphics, ObjectMapSize, ObjectMapSize, _graphics.DepthStencilBuffer.Format);
            _renderTarget = new RenderTarget2D(_graphics, ObjectMapSize, ObjectMapSize, 1, SurfaceFormat.Color, RenderTargetUsage.PreserveContents);
        }

        public ObjectMap DrawObjectMap(Matrix viewProjection, IEnumerable<T> items, Func<T, GameModel> select)
        {
            _graphics.PushRenderTarget(_renderTarget, _depthStencil);
            _graphics.Clear(Color.Black);

            _renderer.Clear();
            _objectMap.Clear();

            uint packedValue = 0;

            foreach (var item in items)
            {
                var color = new Color { PackedValue = packedValue += 10, A = 255 };
                _objectMap.Add(color, item);
                select(item).Draw(color.ToVector4());
            }

            _renderer.DrawObjectMap(viewProjection);
            _graphics.PopRenderTarget();

            _renderTarget.GetTexture().GetData(_colors, 0, _colors.Length);

            return new(_graphics, _objectMap, _colors);
        }

        public void Dispose()
        {
            _depthStencil.Dispose();
            _renderTarget.Dispose();
        }

        public readonly ref struct ObjectMap
        {
            private readonly GraphicsDevice _graphics;
            private readonly Dictionary<Color, T> _objectMap;
            private readonly Color[] _colors;

            public ObjectMap(GraphicsDevice graphics, Dictionary<Color, T> objectMap, Color[] colors)
            {
                _graphics = graphics;
                _objectMap = objectMap;
                _colors = colors;
            }

            public T Pick()
            {
                var pp = _graphics.PresentationParameters;
                var mouseState = Mouse.GetState();
                var mapX = (int)((ObjectMapSize - 1) * MathHelper.Clamp(1.0f * mouseState.X / pp.BackBufferWidth, 0, 1));
                var mapY = (int)((ObjectMapSize - 1) * MathHelper.Clamp(1.0f * mouseState.Y / pp.BackBufferHeight, 0, 1));
                var color = _colors[mapY * ObjectMapSize + mapX];
                return _objectMap.TryGetValue(color, out var result) ? result : default;
            }
        }
    }
}