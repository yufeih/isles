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
                new EffectContentProcessorContext())
                .GetEffectCode();
        }

        private class EffectContentProcessorContext : ContentProcessorContext
        {
            public override ContentBuildLogger Logger => null;
            public override OpaqueDataDictionary Parameters { get; } = new();
            public override TargetPlatform TargetPlatform => TargetPlatform.Windows;
            public override GraphicsProfile TargetProfile => GraphicsProfile.Reach;
            public override string BuildConfiguration => "Release";
            public override string OutputFilename => "";
            public override string OutputDirectory => "";
            public override string IntermediateDirectory => "";

            public override void AddDependency(string filename) { }
            public override void AddOutputFile(string filename) { }
            public override TOutput BuildAndLoadAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName) => default;
            public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName, string assetName) => default;
            public override TOutput Convert<TInput, TOutput>(TInput input, string processorName, OpaqueDataDictionary processorParameters) => default;
        }
    }
}
