// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Graphics;

public readonly struct MeshDrawable
{
    public readonly int PrimitiveCount { get; init; }
    public readonly int VertexCount { get; init; }
    public readonly VertexBuffer VertexBuffer { get; init; }
    public readonly IndexBuffer IndexBuffer { get; init; }

    public void DrawPrimitives()
    {
        var graphicsDevice = VertexBuffer.GraphicsDevice;
        graphicsDevice.SetVertexBuffer(VertexBuffer);
        graphicsDevice.Indices = IndexBuffer;
        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexCount, 0, PrimitiveCount);
    }
}

public readonly ref struct Mesh<T> where T: unmanaged, IVertexType
{
    public readonly Span<T> Vertices { get; init; }
    public readonly Span<ushort> Indices { get; init; }
}

public static class MeshHelper
{
    public static unsafe MeshDrawable ToDrawable<T>(this Mesh<T> mesh, GraphicsDevice graphicsDevice) where T: unmanaged, IVertexType
    {
        var vertexBuffer = new VertexBuffer(graphicsDevice, mesh.Vertices[0].VertexDeclaration, mesh.Vertices.Length, BufferUsage.WriteOnly);
        var indexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort), mesh.Indices.Length, BufferUsage.WriteOnly);

        fixed (void* vertices = mesh.Vertices)
            vertexBuffer.SetDataPointerEXT(0, new(vertices), mesh.Vertices.Length * sizeof(T), SetDataOptions.None);
        fixed (void* indices = mesh.Indices)
            indexBuffer.SetDataPointerEXT(0, new(indices), mesh.Indices.Length * sizeof(ushort), SetDataOptions.None);

        return new()
        {
            VertexCount = mesh.Vertices.Length,
            PrimitiveCount = mesh.Indices.Length / 3,
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
        };
    }

    public delegate void SetVertex<T>(int x, int y, ref T vertex);

    public static Mesh<T> CreateHeightmap<T>(Heightmap heightmap, int uvStep = 1, SetVertex<T>? setVertex = null)
        where T: unmanaged, IVertexPositionTexture, IVertexType
    {
        return CreateGrid<T>(heightmap.Width - 1, heightmap.Height - 1, heightmap.Step, uvStep, SetHeightmapVertex);

        void SetHeightmapVertex(int x, int y, ref T vertex)
        {
            var position = vertex.Position;
            var z = (float)heightmap.Heights[x + y * heightmap.Width];
            vertex.Position = new(position.X, position.Y, z);
            if (setVertex != null)
                setVertex(x, y, ref vertex);
        }
    }

    public static Mesh<T> CreateGrid<T>(int w, int h, float step = 1, int uvStep = 1, SetVertex<T>? setVertex = null)
        where T: unmanaged, IVertexPositionTexture, IVertexType
    {
        var vertexCount = (w + 1) * (h + 1);
        var primitiveCount = w * h * 2;
        var vertices = GC.AllocateUninitializedArray<T>(vertexCount, pinned: true);
        var indices = GC.AllocateUninitializedArray<ushort>(primitiveCount * 3, pinned: true);

        for (var y = 0; y <= h; y++)
        {
            for (var x = 0; x <= w; x++)
            {
                var i = x + y * (w + 1);
                vertices[i].Position = new(x * step, y * step, 0);
                vertices[i].TextureCoordinate = new(1.0f * x / w * uvStep, 1.0f * y / h * uvStep);
                if (setVertex != null)
                    setVertex(x, y, ref vertices[i]);
            }
        }

        var index = 0;
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var v0 = (ushort)(x + y * (w + 1));
                var v1 = (ushort)(v0 + 1);
                var v2 = (ushort)(x + (y + 1) * (w + 1));
                var v3 = (ushort)(v2 + 1);

                indices[index++] = v0;
                indices[index++] = v1;
                indices[index++] = v3;

                indices[index++] = v0;
                indices[index++] = v3;
                indices[index++] = v2;
            }
        }

        return new() { Vertices = vertices, Indices = indices };
    }
}
