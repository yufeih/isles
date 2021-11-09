// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Isles.Graphics;

public class ModelLoader
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly TextureLoader _textureLoader;
    private readonly ConcurrentDictionary<string, Model> _models = new();

    public ModelLoader(GraphicsDevice graphicsDevice, TextureLoader textureLoader)
    {
        _graphicsDevice = graphicsDevice;
        _textureLoader = textureLoader;
    }

    public Model LoadModel(string path)
    {
        return _models.GetOrAdd(path, LoadModelCore);
    }

    private Model LoadModelCore(string path)
    {
        var schema = new
        {
            buffers = new[] { new { uri = "", byteLength = 0 } },
            bufferViews = new[] { new { buffer = 0, byteOffset = 0, byteLength = 0, byteStride = 0, target = 0, } },
            accessors = new[] { new { bufferView = 0, byteOffset = 0, componentType = 0, type = "", count = 0, min = Array.Empty<float>(), max = Array.Empty<float>() } },
            meshes = new[] { new { primitives = new[] { new { attributes = new { POSITION = 0, NORMAL = 0, TEXCOORD_0 = 0, JOINTS_0 = (int?)0, WEIGHTS_0 = (int?)0 }, indices = 0, material = (int?)0, mode = 0 } } } },
            images = new[] { new { uri = "" } },
            textures = new[] { new { source = 0 } },
            materials = new[] { new { doubleSided = false, pbrMetallicRoughness = new { baseColorTexture = new { index = 0 } } } },
            nodes = new[] { new { name = "", mesh = (int?)0, skin = (int?)0, children = Array.Empty<int>(), scale = Array.Empty<float>(), rotation = Array.Empty<float>(), translation = Array.Empty<float>() } },
            animations = new[] { new { name = "", channels = new[] { new { sampler = 0, target = new { node = 0, path = "" } } }, samplers = new[] { new { input = 0, output = 0 } } } },
            skins = new[] { new { inverseBindMatrices = 0, skeleton = 0, joints = Array.Empty<int>() } },
        };

        var model = JsonHelper.DeserializeAnonymousType(File.ReadAllBytes(path), schema);
        var basedir = Path.GetDirectoryName(path);
        var buffers = model.buffers.Select(buffer => File.ReadAllBytes(Path.Combine(basedir, buffer.uri))).ToArray();
        var meshToNode = new Model.Node[model.meshes.Length];

        var nodes = LoadNodes();
        var nodeNames = nodes.Where(node => !string.IsNullOrEmpty(node.Name)).ToDictionary(node => node.Name, StringComparer.OrdinalIgnoreCase);
        var meshes = LoadMeshes();
        var animations = LoadAnimations();

        return new() { Meshes = meshes.ToArray(), Nodes = nodes.ToArray(), NodeNames = nodeNames, Animations = animations };

        List<Model.Node> LoadNodes()
        {
            var nodes = new List<Model.Node>();
            var nodeParentIndex = new int[model.nodes.Length];

            for (var nodeIndex = 0; nodeIndex < model.nodes.Length; nodeIndex++)
            {
                var node = model.nodes[nodeIndex];
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        nodeParentIndex[child] = nodeIndex + 1;
                    }
                }
            }

            for (var nodeIndex = 0; nodeIndex < model.nodes.Length; nodeIndex++)
            {
                var node = model.nodes[nodeIndex];
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
                    meshToNode[node.mesh.Value] = nodes[^1];
                }
            }

            return nodes;
        }

        List<Model.Mesh> LoadMeshes()
        {
            var meshes = new List<Model.Mesh>();
            for (var meshIndex = 0; meshIndex < model.meshes.Length; meshIndex++)
            {
                var mesh = model.meshes[meshIndex];
                var primitives = new List<Model.Primitive>();

                foreach (var primitive in mesh.primitives)
                {
                    // Vertex Buffer
                    var positionAccessor = model.accessors[primitive.attributes.POSITION];
                    var boundingBox = new BoundingBox(
                        min: new(positionAccessor.min[0], positionAccessor.min[1], positionAccessor.min[2]),
                        max: new(positionAccessor.max[0], positionAccessor.max[1], positionAccessor.max[2]));

                    var vertexStride = model.bufferViews[positionAccessor.bufferView].byteStride;
                    var verticesSizeInBytes = positionAccessor.count * vertexStride;
                    var elements = new List<VertexElement>
                        {
                            new((short)model.accessors[primitive.attributes.POSITION].byteOffset, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                            new((short)model.accessors[primitive.attributes.NORMAL].byteOffset, VertexElementFormat.Vector2, VertexElementUsage.Normal, 0),
                            new((short)model.accessors[primitive.attributes.TEXCOORD_0].byteOffset, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
                        };

                    if (primitive.attributes.JOINTS_0 != null && primitive.attributes.WEIGHTS_0 != 0)
                    {
                        elements.Add(new((short)model.accessors[primitive.attributes.JOINTS_0.Value].byteOffset, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0));
                        elements.Add(new((short)model.accessors[primitive.attributes.WEIGHTS_0.Value].byteOffset, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0));
                    }

                    var vertexBuffer = new VertexBuffer(_graphicsDevice, new VertexDeclaration(vertexStride, elements.ToArray()), positionAccessor.count, BufferUsage.WriteOnly);
                    var (vertices, verticesStartIndex) = ReadBuffer(primitive.attributes.POSITION);
                    vertexBuffer.SetData(vertices, verticesStartIndex, verticesSizeInBytes);

                    // Index Buffer
                    var indicesAccessor = model.accessors[primitive.indices];
                    var (indicesSizeInBytes, indexElementSize) = indicesAccessor.componentType == 5123
                        ? (indicesAccessor.count * 2, IndexElementSize.SixteenBits)
                        : (indicesAccessor.count * 4, IndexElementSize.ThirtyTwoBits);
                    var indexBuffer = new IndexBuffer(_graphicsDevice, indexElementSize, indicesAccessor.count, BufferUsage.WriteOnly);
                    var (indices, indicesStartIndex) = ReadBuffer(primitive.indices);
                    indexBuffer.SetData(indices, indicesStartIndex, indicesSizeInBytes);

                    // Material
                    var material = primitive.material is null ? null : model.materials[primitive.material.Value];
                    var imageUri = material is null ? null : model.images[model.textures[material.pbrMetallicRoughness.baseColorTexture.index].source].uri;
                    var texture = imageUri is null ? null : _textureLoader.LoadTexture(Path.Combine(basedir, imageUri));

                    primitives.Add(new()
                    {
                        VertexStride = vertexStride,
                        NumVertices = positionAccessor.count,
                        VertexBuffer = vertexBuffer,
                        IndexBuffer = indexBuffer,
                        PrimitiveCount = indicesAccessor.count / 3,
                        BoundingBox = boundingBox,
                        Texture = texture,
                        DoubleSided = material?.doubleSided ?? false,
                    });
                }

                var node = meshToNode[meshIndex];
                var skinIndex = model.nodes[node.Index].skin;
                var skin = skinIndex is null ? null : model.skins[skinIndex.Value];

                meshes.Add(new()
                {
                    Node = node,
                    Primitives = primitives.ToArray(),
                    InverseBindMatrices = skin is null ? null : ReadBufferAs<Matrix>(skin.inverseBindMatrices),
                    Joints = skin is null ? null : Array.ConvertAll(skin.joints, i => nodes[i]),
                });
            }

            return meshes;
        }

        Dictionary<string, Model.Animation> LoadAnimations()
        {
            if (model.animations is null)
            {
                return null;
            }

            var animations = new Dictionary<string, Model.Animation>();
            foreach (var animation in model.animations)
            {
                var duration = 0f;
                var channels = new Dictionary<int, Model.Animation.Channel>();

                foreach (var channel in animation.channels)
                {
                    var input = animation.samplers[channel.sampler].input;
                    var output = animation.samplers[channel.sampler].output;
                    var times = ReadBufferAs<float>(input);

                    if (model.accessors[input].max[0] > duration)
                    {
                        duration = model.accessors[input].max[0];
                    }

                    var node = channel.target.node;
                    if (!channels.TryGetValue(node, out var item))
                    {
                        item = channels[node] = new();
                    }

                    switch (channel.target.path)
                    {
                        case "scale":
                            item.Scale = new() { Times = times, Values = ReadBufferAs<Vector3>(output) };
                            break;
                        case "rotation":
                            item.Rotation = new() { Times = times, Values = ReadBufferAs<Quaternion>(output) };
                            break;
                        case "translation":
                            item.Translation = new() { Times = times, Values = ReadBufferAs<Vector3>(output) };
                            break;
                    }
                }

                animations.Add(animation.name, new() { Duration = duration, Channels = channels });
            }

            return animations;
        }

        (byte[] bytes, int start) ReadBuffer(int accessorIndex)
        {
            var accessor = model.accessors[accessorIndex];
            var bufferView = model.bufferViews[accessor.bufferView];
            var bytes = buffers[bufferView.buffer];

            return (bytes, bufferView.byteOffset + accessor.byteOffset);
        }

        T[] ReadBufferAs<T>(int accessorIndex) where T : struct
        {
            var accessor = model.accessors[accessorIndex];
            var bufferView = model.bufferViews[accessor.bufferView];
            var bytes = buffers[bufferView.buffer]
                .AsSpan(bufferView.byteOffset, bufferView.byteLength)
                .Slice(accessor.byteOffset, accessor.count * Marshal.SizeOf<T>());

            return MemoryMarshal.Cast<byte, T>(bytes).ToArray();
        }
    }
}
