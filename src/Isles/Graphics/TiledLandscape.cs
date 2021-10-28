// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class TiledLandscape : Landscape
    {
        private Effect terrainEffect;
        private IndexBuffer indexBuffer;
        private VertexBuffer[] vertexBuffers;
        private VertexDeclaration declaraction;

        private int vertexCount;
        private int primitiveCount;

        public override void Initialize(BaseGame game)
        {
            base.Initialize(game);

            // Load terrain effect
            terrainEffect = game.Content.Load<Effect>("Effects/TiledTerrain");

            // Create vertex declaraction
            declaraction = new VertexDeclaration(game.GraphicsDevice, TerrainVertex.VertexElements);

            // Set patch LOD to highest
            foreach (Patch patch in Patches)
            {
                patch.LevelOfDetail = Patch.HighestLOD;
            }

            // Initialize index buffer.
            // All patches use the same index buffer since tiled landscape
            // do not deal with LOD stuff :)
            indexBuffer = new IndexBuffer(game.GraphicsDevice, typeof(ushort),
                                          6 * Patch.MaxPatchResolution *
                                              Patch.MaxPatchResolution,
                                          BufferUsage.WriteOnly);

            var indices = new ushort[6 * Patch.MaxPatchResolution *
                                              Patch.MaxPatchResolution];

            // Fill index buffer and
            Patches[0].FillIndices16(ref indices, 0);

            indexBuffer.SetData(indices);

            // Initialize vertices
            var vertexBufferElementCount = (Patch.MaxPatchResolution + 1) *
                                           (Patch.MaxPatchResolution + 1);

            vertexCount = vertexBufferElementCount;
            primitiveCount = Patch.MaxPatchResolution * Patch.MaxPatchResolution * 2;

            // Create a vertex buffer for each patch
            vertexBuffers = new VertexBuffer[PatchCountOnXAxis * PatchCountOnYAxis];

            // Create an array to store the vertices
            var vertices = new TerrainVertex[vertexBufferElementCount];

            // Initialize individual patch vertex buffer
            var patchIndex = 0;
            for (var yPatch = 0; yPatch < PatchCountOnYAxis; yPatch++)
            {
                for (var xPatch = 0; xPatch < PatchCountOnYAxis; xPatch++)
                {
                    // Fill patch vertices
                    Patches[patchIndex].FillVertices(0,
                    delegate (int x, int y)
                    {
                        return new Vector3(x * Size.X / (GridCountOnXAxis - 1),
                                           y * Size.Y / (GridCountOnYAxis - 1),
                                           HeightField[x, y]);
                    },
                    delegate (uint index, Vector3 position)
                    {
                        vertices[index].Position = position;
                    },
                    delegate (uint index, int x, int y)
                    {
                        vertices[index].Normal = NormalField[x, y];
                        vertices[index].Tangent = TangentField[x, y];

                        vertices[index].Position = new Vector3(
                            x * Size.X / (GridCountOnXAxis - 1),
                            y * Size.Y / (GridCountOnYAxis - 1), HeightField[x, y]);

                        // Texture0 is the tile texture, which only covers half patch
                        vertices[index].TextureCoordinate0 = new Vector2(
                            2.0f * PatchCountOnXAxis * x / (GridCountOnXAxis - 1),
                            2.0f * PatchCountOnYAxis * y / (GridCountOnYAxis - 1));

                        // Texture1 is the visibility texture, which covers the entire terrain
                        vertices[index].TextureCoordinate1 = new Vector2(
                            1.0f * x / (GridCountOnXAxis - 1),
                            1.0f * y / (GridCountOnYAxis - 1));
                    });

                    // Create a vertex buffer for the patch
                    vertexBuffers[patchIndex] = new VertexBuffer(game.GraphicsDevice,
                                                                 typeof(TerrainVertex),
                                                                 vertexBufferElementCount,
                                                                 BufferUsage.WriteOnly);

                    // Set vertex buffer vertices
                    vertexBuffers[patchIndex].SetData(vertices);

                    // Next patch
                    patchIndex++;
                }
            }
        }

        public override void DrawTerrain(Matrix view, Matrix projection, bool upper)
        {
            EffectTechnique technique = upper ?
                terrainEffect.Techniques["FastUpper"] : terrainEffect.Techniques["FastLower"];

            DrawTerrain(view, projection, technique);
        }

        public override void DrawTerrain(GameTime gameTime, ShadowEffect shadowEffect)
        {
            DrawTerrain(game.View, game.Projection, terrainEffect.Techniques["Default"]);

            if (shadowEffect != null)
            {
                DrawTerrainShadow(shadowEffect);
            }
        }

        private void DrawTerrain(Matrix view, Matrix projection, EffectTechnique technique)
        {
            graphics.SetRenderState(BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullNone);

            // Set parameters
            Matrix viewProjection = view * projection;
            if (FogTexture != null)
            {
                terrainEffect.Parameters["FogTexture"].SetValue(FogTexture);
            }

            terrainEffect.Parameters["WorldView"].SetValue(view);
            terrainEffect.Parameters["WorldViewProjection"].SetValue(viewProjection);

            var viewFrustum = new BoundingFrustum(viewProjection);

            // Set indices and vertices
            game.GraphicsDevice.Indices = indexBuffer;
            game.GraphicsDevice.VertexDeclaration = declaraction;

            terrainEffect.CurrentTechnique = technique;

            terrainEffect.Begin();
            terrainEffect.CurrentTechnique.Passes[0].Begin();

            // Draw each patch
            for (var iPatch = 0; iPatch < Patches.Count; iPatch++)
            {
                Patches[iPatch].Visible = viewFrustum.Intersects(Patches[iPatch].BoundingBox);
                if (Patches[iPatch].Visible)
                {
                    // Set patch vertex buffer
                    game.GraphicsDevice.Vertices[0].SetSource(
                        vertexBuffers[iPatch], 0, TerrainVertex.SizeInBytes);

                    // Draw each layer
                    foreach (Layer layer in Layers)
                    {
                        // Set textures
                        terrainEffect.Parameters["ColorTexture"].SetValue(layer.ColorTexture);
                        terrainEffect.Parameters["AlphaTexture"].SetValue(layer.AlphaTexture);
                        terrainEffect.CommitChanges();

                        // Draw patch primitives
                        game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                                  0, 0, vertexCount, 0, primitiveCount);
                    }
                }
            }

            terrainEffect.CurrentTechnique.Passes[0].End();
            terrainEffect.End();
        }

        private void DrawTerrainShadow(ShadowEffect shadowEffect)
        {
            graphics.SetRenderState(BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullNone);

            terrainEffect.Parameters["ShadowMap"].SetValue(shadowEffect.ShadowMap);
            terrainEffect.Parameters["LightViewProjection"].SetValue(shadowEffect.LightViewProjection);
            terrainEffect.CurrentTechnique = terrainEffect.Techniques["ShadowMapping"];

            terrainEffect.Begin();
            terrainEffect.CurrentTechnique.Passes[0].Begin();

            // Draw each patch
            for (var iPatch = 0; iPatch < Patches.Count; iPatch++)
            {
                if (Patches[iPatch].Visible)
                {
                    // Set patch vertex buffer
                    game.GraphicsDevice.Vertices[0].SetSource(
                        vertexBuffers[iPatch], 0, TerrainVertex.SizeInBytes);

                    // Draw patch primitives
                    game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                              0, 0, vertexCount, 0, primitiveCount);
                }
            }

            terrainEffect.CurrentTechnique.Passes[0].End();
            terrainEffect.End();
        }

        public struct TerrainVertex
        {
            public Vector3 Position;
            public Vector2 TextureCoordinate0;
            public Vector2 TextureCoordinate1;
            public Vector3 Normal;
            public Vector3 Tangent;

            public static int SizeInBytes => 4 * (3 + 4 + 3 + 3);

            public static VertexElement[] VertexElements
            {
                get
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
                        new VertexElement(0, 20, VertexElementFormat.Vector2,
                            VertexElementMethod.Default,
                            VertexElementUsage.TextureCoordinate, 1),
                        new VertexElement(0, 28, VertexElementFormat.Vector3,
                            VertexElementMethod.Default, VertexElementUsage.Normal, 0),

                        // And now the tangent
                        new VertexElement(0, 40, VertexElementFormat.Vector3,
                            VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
                    };
                    return decl;
                }
            }
        }
    }
}
