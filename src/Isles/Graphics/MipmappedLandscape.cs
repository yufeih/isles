// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class MipmappedLandscape : Landscape
    {
        /// <summary>
        /// Effect for drawing surface.
        /// </summary>
        private BasicEffect surfaceEffect;

        /// <summary>
        /// Draw a texture on the landscape surface.
        /// </summary>
        /// <param name="texture">Texture to be drawed.</param>
        /// <param name="position">Center position of the texture.</param>
        /// <param name="Size">Texture Size.</param>
        public void DrawSurface(Texture2D texture, Vector2 position, Vector2 Size)
        {
            // Generate a mesh for drawing the texture at a given position
            Vector2 vMin = position - Size / 2;
            Vector2 vMax = position + Size / 2;

            Point pMin = PositionToGrid(vMin.X, vMin.Y);
            Point pMax = PositionToGrid(vMax.X, vMax.Y);

            pMax.X++;
            pMax.Y++;

            var width = pMax.X - pMin.X + 1;
            var height = pMax.Y - pMin.Y + 1;

            int iIndex = 0, iVertex = 0;
            var vertexCount = width * height;
            var indexCount = (width - 1) * (height - 1) * 6; // Triangle list

            var vertices = new VertexPositionTexture[vertexCount];
            var indices = new int[indexCount];

            float z;
            Vector2 v;

            // Generate vertices
            for (var y = pMin.Y; y <= pMax.Y; y++)
            {
                for (var x = pMin.X; x <= pMax.X; x++)
                {
                    v = GridToPosition(x, y);
                    if (x >= 0 && x < GridCountOnXAxis &&
                        y >= 0 && y < GridCountOnYAxis && HeightField[x, y] > 0)
                    {
                        z = HeightField[x, y] + 0.5f; // Offset a little bit :)
                    }
                    else
                    {
                        z = 0;
                    }

                    vertices[iVertex++] = new VertexPositionTexture(
                        new Vector3(v, z), (v - vMin) / Size);
                }
            }

            // Generate indices
            for (var y = 0; y < height - 1; y++)
            {
                for (var x = 0; x < width - 1; x++)
                {
                    indices[iIndex++] = width * y + x;              // 0
                    indices[iIndex++] = width * y + x + 1;          // 1
                    indices[iIndex++] = width * (y + 1) + x + 1;    // 3
                    indices[iIndex++] = width * y + x;              // 0
                    indices[iIndex++] = width * (y + 1) + x + 1;    // 3
                    indices[iIndex++] = width * (y + 1) + x;        // 2
                }
            }

            // Finally draw the mesh
            surfaceEffect.Texture = texture;
            surfaceEffect.TextureEnabled = true;
            surfaceEffect.View = game.View;
            surfaceEffect.Projection = game.Projection;

            // Enable alpha blending :)
            graphics.SetBlendState(BlendState.AlphaBlend);
            graphics.SetDepthStencilState(DepthStencilState.DepthRead);
            graphics.SetRasterizerStateState(RasterizerState.CullCounterClockwise);

            surfaceEffect.CurrentTechnique.Passes[0].Apply();

            graphics.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                vertices, 0, vertexCount,
                indices, 0, indexCount / 3);

            graphics.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            graphics.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
        }

        /// <summary>
        /// Effect used to render the terrain.
        /// </summary>
        private Effect terrainEffect;

        /// <summary>
        /// Terrain vertices.
        /// </summary>
        private TerrainVertex[,] terrainVertices;

        /// <summary>
        /// Terrain vertex buffer.
        /// </summary>
        private VertexBuffer terrainVertexBuffer;

        /// <summary>
        /// Terrain index buffer set.
        /// One index buffer for each patch group.
        /// </summary>
        private readonly List<IndexBuffer> terrainIndexBufferSet = new();

        /// <summary>
        /// Gets or sets the error ratio when computing terrain LOD.
        /// </summary>
        public float TerrainErrorRatio { get; set; } = 0.0012f;

        /// <summary>
        /// This drawing method is used for generating water reflection.
        /// </summary>
        public override void DrawTerrain(Matrix view, Matrix projection, bool upper)
        {
            if (terrainVertexCount == 0 || Layers.Count <= 0)
            {
                return;
            }

            graphics.SetVertexBuffer(terrainVertexBuffer);

            // FIXME: There's a conflict here, originally, terrain rendering
            // uses several tiled textures called Layers. But now, there is
            // only one large terrain texture.
            graphics.Indices = terrainIndexBufferSet[0];

            terrainEffect.Parameters["WorldViewProjection"].SetValue(view * projection);
            terrainEffect.CurrentTechnique = terrainEffect.Techniques["Fast"];
            terrainEffect.Parameters["ColorTexture"].SetValue(Layers[0].ColorTexture);

            terrainEffect.CurrentTechnique.Passes[0].Apply();
            for (var i = 0; i < Layers.Count; i++)
            {
                graphics.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0, 0, (int)terrainVertexCount,
                    0, (int)terrainIndexCount[0] / 3);
            }
        }

        /// <summary>
        /// This method draws the terrain with a higher precision.
        /// </summary>
        public override void DrawTerrain(GameTime gameTime, ShadowEffect shadow)
        {
            if (terrainVertexCount == 0)
            {
                return;
            }

            graphics.SetBlendState(BlendState.AlphaBlend);
            graphics.SetDepthStencilState(DepthStencilState.Default);
            graphics.SetRasterizerStateState(RasterizerState.CullCounterClockwise);

            // This code would go between a device
            // BeginScene-EndScene block.
            graphics.SetVertexBuffer(terrainVertexBuffer);

            var viewInv = Matrix.Invert(game.View);
            terrainEffect.Parameters["ViewInverse"].SetValue(viewInv);
            terrainEffect.Parameters["WorldViewProjection"].SetValue(game.ViewProjection);
            terrainEffect.Parameters["WorldView"].SetValue(game.View);

            if (shadow != null && shadow.ShadowMap != null)
            {
                terrainEffect.Parameters["ShadowMap"].SetValue(shadow.ShadowMap);
                terrainEffect.Parameters["LightViewProjection"].SetValue(shadow.ViewProjection);
                terrainEffect.CurrentTechnique = terrainEffect.Techniques["ShadowMapping"];
            }
            else
            {
                terrainEffect.CurrentTechnique = terrainEffect.Techniques
                    [game.Settings.NormalMappedTerrain ? "NormalMapping" : "Default"];
            }

            var layerCount = 0;
            var patchCount = 0;

            terrainEffect.CurrentTechnique.Passes[0].Apply();

            for (var i = 0; i < Layers.Count; i++)
            {
                // It turns out that set indices are soo expensive when drawing
                // the terrain with simple shaders. But for complex shaders, such
                // as normal mapping, it's better to use the patch group :)
                graphics.Indices = terrainIndexBufferSet[Layers[i].PatchGroup];

                terrainEffect.Parameters["ColorTexture"].SetValue(Layers[i].ColorTexture);
                terrainEffect.Parameters["AlphaTexture"].SetValue(Layers[i].AlphaTexture);
                terrainEffect.Parameters["NormalTexture"].SetValue(Layers[i].NormalTexture);
                terrainEffect.CurrentTechnique.Passes[0].Apply();

                if (terrainIndexCount[Layers[i].PatchGroup] != 0)
                {
                    layerCount++;
                    patchCount += (int)terrainIndexCount[Layers[i].PatchGroup] / 3;
                    graphics.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0, 0, (int)terrainVertexCount,
                        0, (int)terrainIndexCount[Layers[i].PatchGroup] / 3);
                }
            }
        }

        public override void Initialize(BaseGame game)
        {
            base.Initialize(game);

            surfaceEffect = new BasicEffect(graphics);

            // Load effect
            terrainEffect = game.Content.Load<Effect>("Effects/MipmappedTerrain");

            // Initialize terrain vertices
            terrainVertices =
                new TerrainVertex[GridCountOnXAxis, GridCountOnYAxis];

            for (var x = 0; x < GridCountOnXAxis; x++)
            {
                for (var y = 0; y < GridCountOnYAxis; y++)
                {
                    terrainVertices[x, y] = new TerrainVertex(
                        // Position
                        new Vector3(x * Size.X / (GridCountOnXAxis - 1),
                                    y * Size.Y / (GridCountOnYAxis - 1), GetGridHeight(x, y)),
                        // Texture coordinate
                        new Vector2(x * 16.0f / (GridCountOnXAxis - 1), y * 16.0f / (GridCountOnYAxis - 1)),
                        // Normal
                        NormalField[x, y],
                        // Tangent
                        TangentField[x, y]
                    );
                }
            }

            LoadManualContent();
        }

        /// <summary>
        /// Gets a terrain vertex at a given position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public TerrainVertex GetTerrainVertex(int x, int y)
        {
            return terrainVertices[x, y];
        }

        private uint[] workingIndices;

        private TerrainVertex[] workingVertices;
        private uint terrainVertexCount;
        private uint[] terrainIndexCount;

        private Vector3 GetVertexPosition(int x, int y)
        {
            return terrainVertices[x, y].Position;
        }

        private void SetVertexPosition(uint index, Vector3 position)
        {
            workingVertices[index].Position = position;
        }

        private void SetVertex(uint index, int x, int y)
        {
            workingVertices[index] = terrainVertices[x, y];
        }

        private void UpdateTerrainVertexBuffer()
        {
            if (workingVertices == null)
            {
                // workingVertices = new TerrainVertex[
                //    6 * PatchCountOnXAxis * PatchCountOnYAxis *
                //    Patch.MaxPatchResolution * Patch.MaxPatchResolution];
                workingVertices = new TerrainVertex[GridCountOnXAxis * GridCountOnYAxis];
            }

            terrainVertexCount = 0;
            for (var i = 0; i < Patches.Count; i++)
            {
                if (Patches[i].Visible)
                {
                    // Update patch starting vertex
                    Patches[i].StartingVertex = terrainVertexCount;
                    terrainVertexCount += Patches[i].FillVertices(terrainVertexCount,
                                                                  GetVertexPosition,
                                                                  SetVertexPosition,
                                                                  SetVertex);
                }
            }

            if (terrainVertexCount > 0)
            {
                terrainVertexBuffer.SetData(
                    workingVertices, 0, (int)terrainVertexCount);
            }
        }

        private void UpdateTerrainIndexBufferSet()
        {
            if (workingIndices == null)
            {
                workingIndices = new uint[
                    6 * PatchCountOnXAxis * PatchCountOnYAxis *
                    Patch.MaxPatchResolution * Patch.MaxPatchResolution];
                terrainIndexCount = new uint[PatchGroups.Length];
            }

            for (var i = 0; i < PatchGroups.Length; i++)
            {
                terrainIndexCount[i] = 0;
                foreach (var index in PatchGroups[i])
                {
                    if (Patches[index].Visible)
                        terrainIndexCount[i] += Patches[index].
                            FillIndices32(ref workingIndices, terrainIndexCount[i]);
                }

                if (terrainIndexCount[i] > 0)
                {
                    terrainIndexBufferSet[i].SetData(workingIndices, 0, (int)terrainIndexCount[i]);
                }
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        protected virtual void DisposeTerrain()
        {
            if (terrainVertexBuffer != null)
            {
                terrainVertexBuffer.Dispose();
            }

            if (terrainEffect != null)
            {
                terrainEffect.Dispose();
            }

            foreach (IndexBuffer indexBuffer in terrainIndexBufferSet)
            {
                indexBuffer.Dispose();
            }

            foreach (Layer layer in Layers)
            {
                layer.Dispose();
            }
        }

        /// <summary>
        /// Update landscape every frame.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Get current view frustum from camera
            BoundingFrustum viewFrustum = game.ViewFrustum;

            bool visible;
            var LODChanged = false;
            var visibleAreaChanged = false;
            var visibleAreaEnlarged = false;

            var eye = Vector3.Transform(Vector3.Zero, game.ViewInverse);

            for (var i = 0; i < Patches.Count; i++)
            {
                // Perform a bounding box test on each terrain patch
                visible = viewFrustum.Intersects(Patches[i].BoundingBox);
                if (visible != Patches[i].Visible)
                {
                    if (visible)
                    {
                        visibleAreaEnlarged = true;
                    }

                    visibleAreaChanged = true;
                    Patches[i].Visible = visible;
                }

                // Update patch LOD if patch visibility has changed
                if (Patches[i].UpdateLOD(eye, TerrainErrorRatio))
                {
                    LODChanged = true;
                }
            }

            // No need to update anything if visibility hasn't changed.
            // (That means terrain LOD hasn't changed too)
            if (!visibleAreaChanged && !LODChanged)
            {
                return;
            }

            // If patch LOD hasn't changed and the visible area
            // isn't enlarged, we only need to update index buffers :)
            if (LODChanged || visibleAreaEnlarged)
            {
                UpdateTerrainVertexBuffer();
            }

            UpdateTerrainIndexBufferSet();
        }

        /// <summary>
        /// Call this when device is reset.
        /// </summary>
        private void LoadManualContent()
        {
            // Initialize vertex buffer
            terrainVertexBuffer = new DynamicVertexBuffer(
                graphics,
                typeof(TerrainVertex),
                Patch.MaxPatchResolution * Patch.MaxPatchResolution,
                BufferUsage.WriteOnly);

            // Initialize index buffer
            for (var i = 0; i < PatchGroups.Length; i++)
            {
                // Note we use 16 bit index buffer now.
                // Some video card do not support 32 bit index buffer :(
                // Using LOD control, we are likely to limit the number
                // of 256 * 256 terrain triangles within 65535.
                var elementCount = 6 * PatchGroups[i].Count *
                    Patch.MaxPatchResolution * Patch.MaxPatchResolution;

                terrainIndexBufferSet.Add(new IndexBuffer(
                    graphics, typeof(uint), elementCount, BufferUsage.WriteOnly));
            }

            UpdateTerrainVertexBuffer();
            UpdateTerrainIndexBufferSet();
        }

        /// <summary>
        /// Call this when device is lost.
        /// </summary>
        private void UnloadManualContent()
        {
            if (terrainVertexBuffer != null)
            {
                terrainVertexBuffer.Dispose();
            }

            foreach (IndexBuffer indexBuffer in terrainIndexBufferSet)
            {
                indexBuffer.Dispose();
            }

            terrainIndexBufferSet.Clear();
        }

        public void Unload()
        {
            UnloadManualContent();
        }

        /// <summary>
        /// Tangent vertex format for shader vertex format used all over the place.
        /// It contains: Position, Normal vector, texture coords, tangent vector.
        /// </summary>
        public struct TerrainVertex : IVertexType
        {
            // Grabbed from racing game :)

            /// <summary>
            /// Position.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// Texture coordinates.
            /// </summary>
            public Vector2 TextureCoordinate;

            /// <summary>
            /// Normal.
            /// </summary>
            public Vector3 Normal;

            /// <summary>
            /// Tangent.
            /// </summary>
            public Vector3 Tangent;

            /// <summary>
            /// Stride Size, in XNA called SizeInBytes. I'm just conforming with that.
            /// </summary>
            public static int SizeInBytes => 4 * (3 + 2 + 3 + 3);

            /// <summary>
            /// U texture coordinate.
            /// </summary>
            /// <returns>Float.</returns>
            public float U => TextureCoordinate.X;

            /// <summary>
            /// V texture coordinate.
            /// </summary>
            /// <returns>Float.</returns>
            public float V => TextureCoordinate.Y;

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

            /// <summary>
            /// Create tangent vertex.
            /// </summary>
            /// <param name="setPos">Set position.</param>
            /// <param name="setU">Set u texture coordinate.</param>
            /// <param name="setV">Set v texture coordinate.</param>
            /// <param name="setNormal">Set normal.</param>
            /// <param name="setTangent">Set tangent.</param>
            public TerrainVertex(
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
            /// Create tangent vertex.
            /// </summary>
            /// <param name="setPos">Set position.</param>
            /// <param name="setUv">Set uv texture coordinates.</param>
            /// <param name="setNormal">Set normal.</param>
            /// <param name="setTangent">Set tangent.</param>
            public TerrainVertex(
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
            /// Vertex elements for Mesh.Clone.
            /// </summary>
            public static readonly VertexDeclaration VertexDeclaration = new(new VertexElement[]
            {
                // Construct new vertex declaration with tangent info
                // First the normal stuff (we should already have that)
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                // And now the tangent
                new VertexElement(32, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
            });

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
    }
}
