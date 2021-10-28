// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Isles.Graphics
{
    public class GltfModel
    {
        public Mesh[] Meshes { get; init; }
        public Node[] Nodes { get; init; }

        public class Mesh
        {
            public Node Node { get; init; }
            public Primitive[] Primitives { get; init; }
        }

        public class Primitive
        {
            public VertexDeclaration VertexDeclaration { get; init; }
            public VertexBuffer VertexBuffer { get; init; }
            public IndexBuffer IndexBuffer { get; init; }
            public BoundingBox BoundingBox { get; init; }
            public Texture2D Texture { get; init; }
            public bool DoubleSided { get; init; }
            public int VertexStride { get; init; }
            public int NumVertices { get; init; }
            public int PrimitiveCount { get; init; }
        }

        public class Node
        {
            public int Index { get; init; }
            public int ParentIndex { get; init; }
            public string Name { get; init; }
            public Vector3 Scale { get; init; }
            public Vector3 Translation { get; init; }
            public Quaternion Rotation { get; init; }
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
                meshes = new[] { new { primitives = new[] { new { attributes = new { POSITION = 0, NORMAL = 0, TEXCOORD_0 = 0, JOINTS_0 = (int?)0, WEIGHTS_0 = (int?)0 }, indices = 0, material = (int?)0, mode = 0 } } } },
                images = new[] { new { uri = "" } },
                textures = new[] { new { source = 0 } },
                materials = new[] { new { doubleSided = false, pbrMetallicRoughness = new { baseColorTexture = new { index = 0 } } } },
                nodes = new[] { new { name = "", mesh = (int?)0, skin = (int?)0, children = new[] { 0 }, scale = new[] { 0f }, rotation = new[] { 0f }, translation = new[] { 0f } } },
            };

            var gltf = JsonHelper.DeserializeAnonymousType(File.ReadAllBytes(path), gltfSchema);
            var basedir = Path.GetDirectoryName(path);
            var buffers = gltf.buffers.Select(buffer => File.ReadAllBytes(Path.Combine(basedir, buffer.uri))).ToArray();
            var meshes = new List<GltfModel.Mesh>();
            var nodes = new List<GltfModel.Node>();
            var nodeParentIndex = new int[gltf.nodes.Length];
            var meshToNode = new GltfModel.Node[gltf.meshes.Length];

            // Load nodes
            for (var nodeIndex = 0; nodeIndex < gltf.nodes.Length; nodeIndex++)
            {
                var node = gltf.nodes[nodeIndex];
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        nodeParentIndex[child] = nodeIndex + 1;
                    }
                }
            }

            for (var nodeIndex= 0; nodeIndex < gltf.nodes.Length; nodeIndex ++)
            {
                var node = gltf.nodes[nodeIndex];
                nodes.Add(new()
                {
                    Index = nodeIndex,
                    Name = node.name,
                    ParentIndex = nodeParentIndex[nodeIndex] - 1,
                    Scale = node.scale is null ? Vector3.One : new(node.scale[0], node.scale[1], node.scale[2]),
                    Rotation = node.rotation is null ? Quaternion.Identity : new(node.rotation[0], node.rotation[1], node.rotation[2], node.rotation[3]),
                    Translation = node.translation is null ? Vector3.Zero : new(node.translation[0], node.translation[1], node.translation[2]),
                });

                if (node.mesh != null)
                {
                    meshToNode[node.mesh.Value] = nodes[nodes.Count - 1];
                }
            }

            // Load meshes
            for (var meshIndex = 0; meshIndex < gltf.meshes.Length; meshIndex++)
            {
                var mesh = gltf.meshes[meshIndex];
                var primitives = new List<GltfModel.Primitive>();

                foreach (var primitive in mesh.primitives)
                {
                    // Vertex Buffer
                    var positionAccessor = gltf.accessors[primitive.attributes.POSITION];
                    var positionBufferView = gltf.bufferViews[positionAccessor.bufferView];

                    var boundingBox = new BoundingBox(
                        min: new(positionAccessor.min[0], positionAccessor.min[1], positionAccessor.min[2]),
                        max: new(positionAccessor.max[0], positionAccessor.max[1], positionAccessor.max[2]));

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
                    var material = primitive.material is null ? null : gltf.materials[primitive.material.Value];
                    var imageUri = material is null ? null : gltf.images[gltf.textures[material.pbrMetallicRoughness.baseColorTexture.index].source].uri;
                    var texture = imageUri is null ? null : _textureLoader.LoadTexture(Path.Combine(basedir, imageUri));

                    primitives.Add(new()
                    {
                        VertexStride = vertexStride,
                        NumVertices = positionAccessor.count,
                        VertexDeclaration = new VertexDeclaration(_graphicsDevice, elements.ToArray()),
                        VertexBuffer = vertexBuffer,
                        IndexBuffer = indexBuffer,
                        PrimitiveCount = indicesAccessor.count / 3,
                        BoundingBox = boundingBox,
                        Texture = texture,
                        DoubleSided = material?.doubleSided ?? false,
                    });
                }

                meshes.Add(new() { Node = meshToNode[meshIndex], Primitives = primitives.ToArray() });
            }

            return new() { Meshes = meshes.ToArray(), Nodes = nodes.ToArray(), };
        }
    }
}
