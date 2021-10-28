// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Isles.Graphics
{
    public class GltfModel
    {
        public Mesh[] Meshes { get; init; }

        public class Mesh
        {
            public Primitive[] Primitives { get; init; }
        }

        public class Primitive
        {
            public VertexDeclaration VertexDeclaration { get; init; }

            public VertexBuffer VertexBuffer { get; init; }

            public IndexBuffer IndexBuffer { get; init; }

            public Texture2D Texture { get; init; }

            public bool DoubleSided { get; init; }

            public int VertexStride { get; init; }

            public int NumVertices { get; init; }

            public int PrimitiveCount { get; init; }
        }
    }

    public class ModelLoader
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly TextureLoader _textureLoader;
        private readonly ConcurrentDictionary<string, GltfModel> _models = new();

        public ModelLoader(GraphicsDevice graphicsDevice, TextureLoader textureLoader)
        {
            _graphicsDevice = graphicsDevice;
            _textureLoader = textureLoader;
        }

        public GltfModel LoadModel(string path)
        {
            return _models.GetOrAdd(path, LoadModelCore);
        }

        private GltfModel LoadModelCore(string path)
        {
            var gltfSchema = new
            {
                buffers = new[] { new { uri = "", byteLength = 0 } },
                bufferViews = new[] { new { buffer = 0, byteOffset = 0, byteLength = 0, byteStride = 0, target = 0, } },
                accessors = new[] { new { bufferView = 0, byteOffset = 0, componentType = 0, type = "", count = 0, min = new[] { 0f }, max = new[] { 0f } } },
                meshes = new[] { new { primitives = new[] { new { attributes = new { POSITION = 0, NORMAL = 0, TEXCOORD_0 = 0, JOINTS_0 = (int?)0, WEIGHTS_0 = (int?)0 }, indices = 0, material = 0, mode = 0 } } } },
                images = new[] { new { uri = "" } },
                textures = new[] { new { source = 0 } },
                materials = new[] { new { doubleSided = false, pbrMetallicRoughness = new { baseColorTexture = new { index = 0 } } } },
            };

            var gltf = JsonHelper.DeserializeAnonymousType(File.ReadAllBytes(path), gltfSchema);
            var basedir = Path.GetDirectoryName(path);
            var buffers = gltf.buffers.Select(buffer => File.ReadAllBytes(Path.Combine(basedir, buffer.uri))).ToArray();
            var meshes = new List<GltfModel.Mesh>();

            foreach (var mesh in gltf.meshes)
            {
                var primitives = new List<GltfModel.Primitive>();

                foreach (var primitive in mesh.primitives)
                {
                    // Vertex Buffer
                    var positionAccessor = gltf.accessors[primitive.attributes.POSITION];
                    var positionBufferView = gltf.bufferViews[positionAccessor.bufferView];
                    var vertexStride = positionBufferView.byteStride;
                    var elements = new List<VertexElement>
                    {
                        new(0, (short)gltf.accessors[primitive.attributes.POSITION].byteOffset, VertexElementFormat.Vector3, default, VertexElementUsage.Position, 0),
                        new(0, (short)gltf.accessors[primitive.attributes.NORMAL].byteOffset, VertexElementFormat.Vector2, default, VertexElementUsage.Normal, 0),
                        new(0, (short)gltf.accessors[primitive.attributes.TEXCOORD_0].byteOffset, VertexElementFormat.Vector2, default, VertexElementUsage.TextureCoordinate, 0)
                    };

                    if (primitive.attributes.JOINTS_0 != null && primitive.attributes.WEIGHTS_0 != 0)
                    {
                        elements.Add(new(0, (short)gltf.accessors[primitive.attributes.JOINTS_0.Value].byteOffset, VertexElementFormat.Byte4, default, VertexElementUsage.BlendIndices, 0));
                        elements.Add(new(0, (short)gltf.accessors[primitive.attributes.WEIGHTS_0.Value].byteOffset, VertexElementFormat.Vector4, default, VertexElementUsage.BlendWeight, 0));
                    }

                    var vertexBuffer = new VertexBuffer(_graphicsDevice, positionBufferView.byteLength, BufferUsage.WriteOnly);
                    vertexBuffer.SetData(buffers[positionBufferView.buffer], positionBufferView.byteOffset, positionBufferView.byteLength);

                    // Index Buffer
                    var indicesAccessor = gltf.accessors[primitive.indices];
                    var indicesBufferView = gltf.bufferViews[indicesAccessor.bufferView];
                    var (indicesSizeInBytes, indexElementSize) = indicesAccessor.componentType == 5123
                        ? (indicesAccessor.count * 2, IndexElementSize.SixteenBits)
                        : (indicesAccessor.count * 4, IndexElementSize.ThirtyTwoBits);
                    var indexBuffer = new IndexBuffer(_graphicsDevice, indicesSizeInBytes, BufferUsage.WriteOnly, indexElementSize);
                    indexBuffer.SetData(buffers[indicesBufferView.buffer], indicesBufferView.byteOffset, indicesSizeInBytes);

                    // Material
                    var material = gltf.materials[primitive.material];
                    var imageUri = gltf.images[gltf.textures[material.pbrMetallicRoughness.baseColorTexture.index].source].uri;

                    primitives.Add(new()
                    {
                        VertexStride = vertexStride,
                        NumVertices = positionAccessor.count,
                        VertexDeclaration = new VertexDeclaration(_graphicsDevice, elements.ToArray()),
                        VertexBuffer = vertexBuffer,
                        IndexBuffer = indexBuffer,
                        PrimitiveCount = indicesAccessor.count / 3,
                        Texture = _textureLoader.LoadTexture(Path.Combine(basedir, imageUri)),
                        DoubleSided = material.doubleSided,
                    });
                }

                meshes.Add(new() { Primitives = primitives.ToArray() });
            }

            return new() { Meshes = meshes.ToArray() };
        }
    }
}
