// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles.Graphics;

public interface IVertexPositionTexture
{
    Vector3 Position { get; set; }
    Vector2 TextureCoordinate { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionTexture : IVertexType, IVertexPositionTexture
{
    public Vector3 Position { get; set; }
    public Vector2 TextureCoordinate { get; set; }

    public static readonly VertexDeclaration VertexDeclaration = new(new VertexElement[]
    {
        new(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
    });

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionTexture2 : IVertexType, IVertexPositionTexture
{
    public Vector3 Position { get; set; }
    public Vector2 TextureCoordinate { get; set; }
    public Vector2 TextureCoordinate1 { get; set; }

    public static readonly VertexDeclaration VertexDeclaration = new(new VertexElement[]
    {
        new(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
    });

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}