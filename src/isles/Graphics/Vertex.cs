// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public struct VertexPositionNormalDuoTexture : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate0;
        public Vector2 TextureCoordinate1;

        public static readonly VertexDeclaration VertexDeclaration = new(new VertexElement[]
        {
            new(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
        });

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
