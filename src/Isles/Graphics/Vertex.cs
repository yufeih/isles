//-----------------------------------------------------------------------------
//  Isles v1.0
//
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{

    /// <summary>
    /// Tangent vertex format for shader vertex format used all over the place.
    /// It contains: Position, Normal vector, texture coords, tangent vector.
    /// </summary>
    public struct VertexTangent
    {
        // Grabbed from racing game :)

        /// <summary>
        /// Position
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Texture coordinates
        /// </summary>
        public Vector2 TextureCoordinate;
        /// <summary>
        /// Normal
        /// </summary>
        public Vector3 Normal;
        /// <summary>
        /// Tangent
        /// </summary>
        public Vector3 Tangent;

        /// <summary>
        /// Stride size, in XNA called SizeInBytes. I'm just conforming with that.
        /// </summary>
        public static int SizeInBytes => 4 * (3 + 2 + 3 + 3);

        /// <summary>
        /// U texture coordinate
        /// </summary>
        /// <returns>Float</returns>
        public float U => TextureCoordinate.X;

        /// <summary>
        /// V texture coordinate
        /// </summary>
        /// <returns>Float</returns>
        public float V => TextureCoordinate.Y;

        /// <summary>
        /// Create tangent vertex
        /// </summary>
        /// <param name="setPos">Set position</param>
        /// <param name="setU">Set u texture coordinate</param>
        /// <param name="setV">Set v texture coordinate</param>
        /// <param name="setNormal">Set normal</param>
        /// <param name="setTangent">Set tangent</param>
        public VertexTangent(
            Vector3 setPos,
            float setU, float setV,
            Vector3 setNormal,
            Vector3 setTangent)
        {
            Position = setPos;
            TextureCoordinate = new Vector2(setU, setV);
            Normal = setNormal;
            Tangent = setTangent;
        }

        /// <summary>
        /// Create tangent vertex
        /// </summary>
        /// <param name="setPos">Set position</param>
        /// <param name="setUv">Set uv texture coordinates</param>
        /// <param name="setNormal">Set normal</param>
        /// <param name="setTangent">Set tangent</param>
        public VertexTangent(
            Vector3 setPos,
            Vector2 setUv,
            Vector3 setNormal,
            Vector3 setTangent)
        {
            Position = setPos;
            TextureCoordinate = setUv;
            Normal = setNormal;
            Tangent = setTangent;
        }

        /// <summary>
        /// Vertex elements for Mesh.Clone
        /// </summary>
        public static readonly VertexElement[] VertexElements =
            GenerateVertexElements();

        /// <summary>
        /// Generate vertex declaration
        /// </summary>
        private static VertexElement[] GenerateVertexElements()
        {
            var decl = new VertexElement[]
                {
                    // Construct new vertex declaration with tangent info
                    // First the normal stuff (we should already have that)
                    new VertexElement(0, 0, VertexElementFormat.Vector3,
                        VertexElementMethod.Default, VertexElementUsage.Position, 0),
                    new VertexElement(0, 12, VertexElementFormat.Vector2,
                        VertexElementMethod.Default,
                        VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(0, 20, VertexElementFormat.Vector3,
                        VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                    // And now the tangent
                    new VertexElement(0, 32, VertexElementFormat.Vector3,
                        VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
                };
            return decl;
        }

        /// <summary>
        /// Returns true if declaration is tangent vertex declaration.
        /// </summary>
        public static bool IsTangentVertexDeclaration(
            VertexElement[] declaration)
        {
            return declaration == null
                ? throw new ArgumentNullException("declaration")
                : declaration.Length == 4 &&
                declaration[0].VertexElementUsage == VertexElementUsage.Position &&
                declaration[1].VertexElementUsage ==
                VertexElementUsage.TextureCoordinate &&
                declaration[2].VertexElementUsage == VertexElementUsage.Normal &&
                declaration[3].VertexElementUsage == VertexElementUsage.Tangent;
        }

    }

    /// <summary>
    /// Vertex format for shader vertex format used all over the place.
    /// It contains: Position, Normal vector, 2 texture coords
    /// </summary>
    public struct VertexPositionNormalDuoTexture
    {

        /// <summary>
        /// Position
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Normal
        /// </summary>
        public Vector3 Normal;
        /// <summary>
        /// Texture coordinates
        /// </summary>
        public Vector2 TextureCoordinate0;
        /// <summary>
        /// Tangent
        /// </summary>
        public Vector2 TextureCoordinate1;

        /// <summary>
        /// Stride size, in XNA called SizeInBytes. I'm just conforming with that.
        /// </summary>
        public static int SizeInBytes => 4 * (3 + 4 + 3);

        /// <summary>
        /// Create tangent vertex
        /// </summary>
        /// <param name="setPos">Set position</param>
        /// <param name="setUv">Set uv texture coordinates</param>
        /// <param name="setNormal">Set normal</param>
        /// <param name="setTangent">Set tangent</param>
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
        /// Vertex elements for Mesh.Clone
        /// </summary>
        public static readonly VertexElement[] VertexElements =
            GenerateVertexElements();

        /// <summary>
        /// Generate vertex declaration
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
