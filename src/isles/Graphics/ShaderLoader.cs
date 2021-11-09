// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Isles.Graphics;

public class ShaderLoader
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ConcurrentDictionary<string, Effect> _shaders = new();

    public ShaderLoader(GraphicsDevice graphicsDevice) => _graphicsDevice = graphicsDevice;

    public Effect LoadShader(string path)
    {
        return _shaders.GetOrAdd(path, path => new(_graphicsDevice, File.ReadAllBytes(path)));
    }
}
