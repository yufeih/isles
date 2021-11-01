// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    /// <summary>
    /// Type of a billboard.
    /// </summary>
    [Flags]
    public enum BillboardType
    {
        /// <summary>
        /// Uses a two pass rendering technique for vegetation rendering.
        /// </summary>
        Vegetation = 1,

        /// <summary>
        /// Rotate around the center.
        /// </summary>
        CenterOriented = 1 << 2,

        /// <summary>
        /// Rotate around a normal vector.
        /// </summary>
        NormalOriented = 1 << 3,

        /// <summary>
        /// Whether depth buffer is enabled when rendering the billboard.
        /// </summary>
        DepthBufferEnable = 1 << 4,
    }

    public enum AnchorType
    {
        /// <summary>
        /// Position anchor on top.
        /// </summary>
        Top = 0,

        /// <summary>
        /// Position anchor on Center.
        /// </summary>
        Center = 1,

        /// <summary>
        /// Position anchor on bottom.
        /// </summary>
        Bottom = 2,
    }

    /// <summary>
    /// A billboard definition.
    /// </summary>
    public class Billboard
    {
        public Texture2D Texture { get; set; }

        /// <summary>
        /// Position of the billboard. Bottom center in the texture.
        /// </summary>
        private Vector3 position;

        public Vector3 Position
        {
            get => position;

            set => position = value;
        }

        /// <summary>
        /// Type of the billboard.
        /// </summary>
        public BillboardType Type { get; set; }

        /// <summary>
        /// Type of position anchor.
        /// </summary>
        public AnchorType AchorType { get; set; }

        /// <summary>
        /// Normalized vector around which the billboard is rotating.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Size of the billboard.
        /// </summary>
        public Vector2 Size;

        /// <summary>
        /// Source rectangle (min, max). Measured in float [0 ~ 1].
        /// </summary>
        public Vector4 SourceRectangle;

        /// <summary>
        /// Default source rectangle.
        /// </summary>
        public static readonly Vector4 DefaultSourceRectangle = new(0, 0, 1, 1);
        public static readonly Vector4 DefaultSourceRectangleXInversed = new(1, 0, 0, 1);

        public Billboard()
        {
            AchorType = AnchorType.Center;
        }

        public void Draw()
        {
            BaseGame.Singleton.Billboard.Draw(this);
        }
    }

    /// <summary>
    /// Manager class for billboard.
    /// </summary>
    public class BillboardManager : IDisposable
    {
        private static Vector3 GetCenterPosition(Billboard billboard)
        {
            if (billboard.AchorType == AnchorType.Top)
            {
                return billboard.Position - new Vector3(0, 0, billboard.Size.Y / 2);
            }
            else
            {
                return billboard.AchorType == AnchorType.Bottom ? billboard.Position + new Vector3(0, 0, billboard.Size.Y / 2) : billboard.Position;
            }
        }

        /// <summary>
        /// Max number of quads that can be rendered in one draw call.
        /// </summary>
        public const int ChunkSize = 1024;

        /// <summary>
        /// Billboard effect.
        /// </summary>
        private readonly Effect effect;

        /// <summary>
        /// Billboard effect techniques.
        /// </summary>
        private readonly EffectTechnique techniqueVegetation;
        private readonly EffectTechnique techniqueNormal;
        private readonly EffectTechnique techniqueCenter;

        /// <summary>
        /// Internal billboard list.
        /// </summary>
        private readonly List<Billboard> billboards = new();

        /// <summary>
        /// Quad vertices.
        /// </summary>
        private readonly DynamicVertexBuffer vertices;

        /// <summary>
        /// Quad indices.
        /// </summary>
        private readonly DynamicIndexBuffer indices;

        /// <summary>
        /// Vertex buffer used to generate vertices.
        /// </summary>
        private VertexPositionNormalDuoTexture[] workingVertices =
            new VertexPositionNormalDuoTexture[ChunkSize * 4];

        /// <summary>
        /// Index buffer used to generate indices.
        /// </summary>
        private short[] workingIndices = new short[ChunkSize * 6];

        /// <summary>
        /// Graphics device.
        /// </summary>
        private readonly BaseGame game;

        /// <summary>
        /// Create a billboard manager.
        /// </summary>
        public BillboardManager(BaseGame game)
        {
            this.game = game;

            // Initialize billboard effect
            effect = game.ShaderLoader.LoadShader("data/shaders/billboard.fx");

            techniqueVegetation = effect.Techniques["Vegetation"];
            techniqueNormal = effect.Techniques["Normal"];
            techniqueCenter = effect.Techniques["Center"];

            // Create vertices & indices
            vertices = new DynamicVertexBuffer(game.GraphicsDevice,
                typeof(VertexPositionNormalDuoTexture), ChunkSize * 4, BufferUsage.WriteOnly);

            indices = new DynamicIndexBuffer(game.GraphicsDevice,
                typeof(short), ChunkSize * 6, BufferUsage.WriteOnly);
        }

        /// <summary>
        /// Draw a billboard.
        /// </summary>
        public void Draw(Texture2D texture, Vector3 position, Vector2 size, AnchorType achorType)
        {
            var billboard = new Billboard
            {
                AchorType = achorType,
                Texture = texture,
                Position = position,
                Normal = Vector3.UnitZ,
                Size = size,
                Type = BillboardType.CenterOriented,
                SourceRectangle = Billboard.DefaultSourceRectangle,
            };

            Draw(billboard);
        }

        public void Draw(Texture2D texture, Vector3 position,
            Vector2 size, Vector3 normal, Vector4 sourceRectangle, AnchorType achorType)
        {
            var billboard = new Billboard
            {
                AchorType = achorType,
                Texture = texture,
                Position = position,
                Normal = normal,
                Size = size,
                SourceRectangle = sourceRectangle,
                Type = BillboardType.NormalOriented,
            };

            Draw(billboard);
        }

        /// <summary>
        /// Draw a billboard.
        /// </summary>
        /// <param name="billboard"></param>
        public void Draw(Billboard billboard)
        {
            billboards.Add(billboard);
        }

        public void Present()
        {
            if (billboards.Count <= 0)
            {
                return;
            }

            BillboardType currentType = BillboardType.NormalOriented;

            // Set effect parameters
            effect.Parameters["View"].SetValue(game.View);
            effect.Parameters["Projection"].SetValue(game.Projection);

            // Make sure normal oriented is 0 and center oriented is 1
            effect.CurrentTechnique = effect.Techniques[0];

            // It's not fast to sort all billboards using texture and distance,
            // we just check if texture is changed. So always draw a bounch of
            // billboards using the same texture.
            //
            // Divide all billboards into small chunks. A chunk ends when the chunk
            // size reaches MaxChunkSize or when the texture is changed.
            // After the division, we setup effect paramters and render each chunk
            // in one draw call
            Texture2D texture = billboards[0].Texture;

            int baseIndex = 0, baseVertex = 0;
            int begin = 0;
            for (var i = 1; i <= billboards.Count; i++)
            {
                // We are at the end of the chunk
                if (i != billboards.Count && // End of list
                   (i - begin) < ChunkSize && texture == billboards[i].Texture)
                {
                    continue;
                }

                var end = i;
                if (i != billboards.Count)
                {
                    texture = billboards[i].Texture;
                }

                // Setup graphics device
                game.GraphicsDevice.SetVertexBuffer(null);
                game.GraphicsDevice.Indices = null;

                // Build the mesh for this chunk of billboards
                baseIndex = baseVertex = 0;
                for (var k = begin; k < end; k++)
                {
                    CreateQuad(billboards[k],
                        ref workingVertices, ref baseVertex, ref workingIndices, ref baseIndex);
                }

                if (baseVertex <= 0 || baseIndex <= 0)
                {
                    continue;
                }

                // Update vertex/index buffer
                vertices.SetData(workingVertices, 0, baseVertex);
                indices.SetData(workingIndices, 0, baseIndex);

                // Setup graphics device
                game.GraphicsDevice.SetVertexBuffer(vertices);
                game.GraphicsDevice.Indices = indices;

                // Set effect texture
                effect.Parameters["Texture"].SetValue(billboards[begin].Texture);

                // Set effect technique
                if (billboards[begin].Type != currentType)
                {
                    currentType = billboards[begin].Type;

                    if ((currentType & BillboardType.Vegetation) == BillboardType.Vegetation)
                    {
                        effect.CurrentTechnique = techniqueVegetation;
                    }
                    else if ((currentType & BillboardType.CenterOriented) == BillboardType.CenterOriented)
                    {
                        effect.CurrentTechnique = techniqueCenter;
                    }
                    else if ((currentType & BillboardType.NormalOriented) == BillboardType.NormalOriented)
                    {
                        effect.CurrentTechnique = techniqueNormal;
                    }

                    game.GraphicsDevice.SetRenderState(depthStencilState: (currentType & BillboardType.DepthBufferEnable) == BillboardType.DepthBufferEnable ? DepthStencilState.Default : DepthStencilState.None);
                }

                // Draw the chunk
                effect.CurrentTechnique.Passes[0].Apply();

                game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, baseVertex, 0, baseIndex / 3);

                // Increment begin pointer
                begin = end;
            }

            // Clear internal list after drawing
            billboards.Clear();
        }

        private static void CreateQuad(Billboard billboard,
            ref VertexPositionNormalDuoTexture[] vertices, ref int baseVertex,
            ref short[] indices, ref int baseIndex)
        {
            // Quad:
            //
            // 0 --- 1
            // | \   |
            // |  \  |
            // |   \ |
            // 3 --- 2
            for (var i = 0; i < 4; i++)
            {
                vertices[baseVertex + i].Position = GetCenterPosition(billboard);
                vertices[baseVertex + i].Normal = billboard.Normal;
            }

            // Use UV0 to store source rectangle
            vertices[baseVertex + 3].TextureCoordinate0.X =
            vertices[baseVertex + 0].TextureCoordinate0.X = billboard.SourceRectangle.X;

            vertices[baseVertex + 1].TextureCoordinate0.Y =
            vertices[baseVertex + 0].TextureCoordinate0.Y = billboard.SourceRectangle.Y;

            vertices[baseVertex + 1].TextureCoordinate0.X =
            vertices[baseVertex + 2].TextureCoordinate0.X = billboard.SourceRectangle.Z;

            vertices[baseVertex + 3].TextureCoordinate0.Y =
            vertices[baseVertex + 2].TextureCoordinate0.Y = billboard.SourceRectangle.W;

            // Use UV1 to store size
            if ((billboard.Type & BillboardType.Vegetation) == BillboardType.Vegetation)
            {
                vertices[baseVertex + 2].TextureCoordinate1.Y =
                vertices[baseVertex + 3].TextureCoordinate1.Y = 0;

                vertices[baseVertex + 0].TextureCoordinate1.Y =
                vertices[baseVertex + 1].TextureCoordinate1.Y = billboard.Size.Y;
            }
            else
            {
                vertices[baseVertex + 2].TextureCoordinate1.Y =
                vertices[baseVertex + 3].TextureCoordinate1.Y = -billboard.Size.Y / 2;

                vertices[baseVertex + 0].TextureCoordinate1.Y =
                vertices[baseVertex + 1].TextureCoordinate1.Y = billboard.Size.Y / 2;
            }

            vertices[baseVertex + 0].TextureCoordinate1.X =
            vertices[baseVertex + 3].TextureCoordinate1.X = -billboard.Size.X / 2;

            vertices[baseVertex + 1].TextureCoordinate1.X =
            vertices[baseVertex + 2].TextureCoordinate1.X = billboard.Size.X / 2;

            // Fill indices
            indices[baseIndex + 0] = (short)(baseVertex + 0);
            indices[baseIndex + 1] = (short)(baseVertex + 1);
            indices[baseIndex + 2] = (short)(baseVertex + 2);
            indices[baseIndex + 3] = (short)(baseVertex + 0);
            indices[baseIndex + 4] = (short)(baseVertex + 2);
            indices[baseIndex + 5] = (short)(baseVertex + 3);

            // Increment base vertex/index
            baseVertex += 4;
            baseIndex += 6;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            effect?.Dispose();
            vertices?.Dispose();
            indices?.Dispose();
        }
    }
}
