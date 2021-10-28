// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    /// <summary>
    /// Vertex format for shader vertex format used all over the place.
    /// It contains: Position, Normal vector, 2 texture coords.
    /// </summary>
    public struct VertexPositionNormalDuoTexture
    {
        /// <summary>
        /// Position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Normal.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Texture coordinates.
        /// </summary>
        public Vector2 TextureCoordinate0;

        /// <summary>
        /// Tangent.
        /// </summary>
        public Vector2 TextureCoordinate1;

        /// <summary>
        /// Stride size, in XNA called SizeInBytes. I'm just conforming with that.
        /// </summary>
        public static int SizeInBytes => 4 * (3 + 4 + 3);

        /// <summary>
        /// Create tangent vertex.
        /// </summary>
        /// <param name="setPos">Set position.</param>
        /// <param name="setUv">Set uv texture coordinates.</param>
        /// <param name="setNormal">Set normal.</param>
        /// <param name="setTangent">Set tangent.</param>
        public VertexPositionNormalDuoTexture(
            Vector3 position,
            Vector3 normal,
            Vector2 uv0,
            Vector2 uv1)
        {
            Position = position;
            TextureCoordinate0 = uv0;
            TextureCoordinate1 = uv1;
            Normal = normal;
        }

        /// <summary>
        /// Vertex elements for Mesh.Clone.
        /// </summary>
        public static readonly VertexElement[] VertexElements =
            GenerateVertexElements();

        /// <summary>
        /// Generate vertex declaration.
        /// </summary>
        private static VertexElement[] GenerateVertexElements()
        {
            var decl = new VertexElement[]
                {
                    // Construct new vertex declaration with tangent info
                    // First the normal stuff (we should already have that)
                    new VertexElement(0, 0, VertexElementFormat.Vector3,
                        VertexElementMethod.Default, VertexElementUsage.Position, 0),
                    new VertexElement(0, 12, VertexElementFormat.Vector3,
                        VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                    new VertexElement(0, 24, VertexElementFormat.Vector2,
                        VertexElementMethod.Default,
                        VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(0, 32, VertexElementFormat.Vector2,
                        VertexElementMethod.Default,
                        VertexElementUsage.TextureCoordinate, 1),
                };
            return decl;
        }
    }
}
