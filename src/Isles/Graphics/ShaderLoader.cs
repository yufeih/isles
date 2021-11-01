// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class ShaderLoader
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly ConcurrentDictionary<string, Effect> _shaders = new();
        private readonly EffectProcessor _effectProcessor = new();

        public ShaderLoader(GraphicsDevice graphicsDevice) => _graphicsDevice = graphicsDevice;

        public Effect LoadShader(string path)
        {
            return _shaders.GetOrAdd(path, path => new(_graphicsDevice, CompileEffect(path)));
        }

        private byte[] CompileEffect(string path)
        {
            return _effectProcessor.Process(
                new() { EffectCode = File.ReadAllText(path), Identity = new(path) },
                null)
                .GetEffectCode();
        }
    }
}
