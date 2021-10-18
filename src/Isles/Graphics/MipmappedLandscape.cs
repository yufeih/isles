//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    public class MipmappedLandscape : Landscape
    {
        #region Draw Surface

        /// <summary>
        /// Effect for drawing surface
        /// </summary>
        BasicEffect surfaceEffect;

        VertexDeclaration surfaceDeclaraction;

        /// <summary>
        /// Draw a texture on the landscape surface
        /// </summary>
        /// <param name="texture">Texture to be drawed</param>
        /// <param name="position">Center position of the texture</param>
        /// <param name="Size">Texture Size</param>
        public void DrawSurface(Texture2D texture, Vector2 position, Vector2 Size)
        {
            // Generate a mesh for drawing the texture at a given position
            Vector2 vMin = position - Size / 2;
            Vector2 vMax = position + Size / 2;

            Point pMin = PositionToGrid(vMin.X, vMin.Y);
            Point pMax = PositionToGrid(vMax.X, vMax.Y);

            pMax.X++; pMax.Y++;

            int width = pMax.X - pMin.X + 1;
            int height = pMax.Y - pMin.Y + 1;

            int iIndex = 0, iVertex = 0;
            int vertexCount = width * height;
            int indexCount = (width - 1) * (height - 1) * 6; // Triangle list

            VertexPositionTexture[] vertices = new VertexPositionTexture[vertexCount];
            int[] indices = new int[indexCount];

            float z;
            Vector2 v;

            // Generate vertices
            for (int y = pMin.Y; y <= pMax.Y; y++)
                for (int x = pMin.X; x <= pMax.X; x++)
                {
                    v = GridToPosition(x, y);
                    if (x >= 0 && x < GridCountOnXAxis &&
                        y >= 0 && y < GridCountOnYAxis && HeightField[x, y] > 0)
                        z = HeightField[x, y] + 0.5f; // Offset a little bit :)
                    else
                        z = 0;

                    vertices[iVertex++] = new VertexPositionTexture(
                        new Vector3(v, z), (v - vMin) / Size);
                }

            // Generate indices
            for (int y = 0; y < height - 1; y++)
                for (int x = 0; x < width - 1; x++)
                {
                    indices[iIndex++] = width * y + x;              // 0
                    indices[iIndex++] = width * y + x + 1;          // 1
                    indices[iIndex++] = width * (y + 1) + x + 1;    // 3
                    indices[iIndex++] = width * y + x;              // 0
                    indices[iIndex++] = width * (y + 1) + x + 1;    // 3
                    indices[iIndex++] = width * (y + 1) + x;        // 2
                }

            // Finally draw the mesh
            surfaceEffect.Texture = texture;
            surfaceEffect.TextureEnabled = true;
            surfaceEffect.View = game.View;
            surfaceEffect.Projection = game.Projection;

            // Enable alpha blending :)
            graphics.RenderState.AlphaBlendEnable = true;
            graphics.RenderState.DepthBufferWriteEnable = false;
            //graphics.RenderState.DepthBufferFunction = CompareFunction.Always;
            graphics.RenderState.CullMode = CullMode.None;
            graphics.SamplerStates[0].AddressU = TextureAddressMode.Border;
            graphics.SamplerStates[0].AddressV = TextureAddressMode.Border;

            graphics.VertexDeclaration = surfaceDeclaraction;

            surfaceEffect.Begin();

            foreach (EffectPass pass in surfaceEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                graphics.DrawUserIndexedPrimitives<VertexPositionTexture>(
                    PrimitiveType.TriangleList,
                    vertices, 0, vertexCount,
                    indices, 0, indexCount / 3);

                pass.End();
            }
            surfaceEffect.End();

            // Restore settings
            graphics.RenderState.AlphaBlendEnable = false;
            graphics.RenderState.DepthBufferWriteEnable = true;
            //graphics.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            graphics.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            graphics.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
        }

        #endregion
        
        #region Fields

        /// <summary>
        /// Effect used to render the terrain
        /// </summary>
        Effect terrainEffect;

        /// <summary>
        /// Terrain vertices
        /// </summary>
        TerrainVertex[,] terrainVertices;

        /// <summary>
        /// Terrain vertex buffer
        /// </summary>
        VertexBuffer terrainVertexBuffer;

        /// <summary>
        /// Terrain index buffer set.
        /// One index buffer for each patch group.
        /// </summary>
        List<IndexBuffer> terrainIndexBufferSet = new List<IndexBuffer>();

        /// <summary>
        /// Terrain vertex declaration
        /// </summary>
        VertexDeclaration terrainVertexDeclaration;


        /// <summary>
        /// Gets or sets the error ratio when computing terrain LOD
        /// </summary>
        public float TerrainErrorRatio
        {
            get { return terrainErrorRatio; }
            set { terrainErrorRatio = value; }
        }

        float terrainErrorRatio = 0.0012f;
        #endregion

        #region Methods
        /// <summary>
        /// This drawing method is used for generating water reflection
        /// </summary>
        public override void DrawTerrain(Matrix view, Matrix projection, bool upper)
        {
            if (terrainVertexCount == 0 || Layers.Count <= 0)
                return;

            graphics.VertexDeclaration = terrainVertexDeclaration;
            graphics.Vertices[0].SetSource(
                terrainVertexBuffer, 0, TerrainVertex.SizeInBytes);

            // FIXME: There's a conflict here, originally, terrain rendering
            // uses several tiled textures called Layers. But now, there is
            // only one large terrain texture.
            graphics.Indices = terrainIndexBufferSet[0];

            terrainEffect.Parameters["WorldViewProjection"].SetValue(view * projection);
            terrainEffect.CurrentTechnique = terrainEffect.Techniques["Fast"];
            terrainEffect.Parameters["ColorTexture"].SetValue(Layers[0].ColorTexture);

            terrainEffect.Begin();
            foreach (EffectPass pass in terrainEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                for (int i = 0; i < Layers.Count; i++)
                {
                    graphics.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0, 0, (int)terrainVertexCount,
                        0, (int)terrainIndexCount[0] / 3);
                }
                pass.End();
            }
            terrainEffect.End();
        }

        /// <summary>
        /// This method draws the terrain with a higher precision
        /// </summary>
        public override void DrawTerrain(GameTime gameTime, ShadowEffect shadow)
        {
            if (terrainVertexCount == 0)
                return;

            // This code would go between a device 
            // BeginScene-EndScene block.
            graphics.Vertices[0].SetSource(
                terrainVertexBuffer, 0, TerrainVertex.SizeInBytes);
            graphics.VertexDeclaration = terrainVertexDeclaration;

            Matrix viewInv = Matrix.Invert(game.View);
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

            int layerCount = 0;
            int patchCount = 0;

            terrainEffect.Begin();
            foreach (EffectPass pass in terrainEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                for (int i = 0; i < Layers.Count; i++)
                {
                    // Disable alpha blending when drawing our first layer
                    graphics.RenderState.AlphaBlendEnable = true;//(i != 0);

                    // It turns out that set indices are soo expensive when drawing
                    // the terrain with simple shaders. But for complex shaders, such
                    // as normal mapping, it's better to use the patch group :)
                    graphics.Indices = terrainIndexBufferSet[Layers[i].PatchGroup];

                    terrainEffect.Parameters["ColorTexture"].SetValue(Layers[i].ColorTexture);
                    terrainEffect.Parameters["AlphaTexture"].SetValue(Layers[i].AlphaTexture);
                    terrainEffect.Parameters["NormalTexture"].SetValue(Layers[i].NormalTexture);
                    terrainEffect.CommitChanges();

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
                pass.End();
            }
            terrainEffect.End();

            graphics.RenderState.DepthBufferEnable = true;
            graphics.RenderState.DepthBufferWriteEnable = true;
            graphics.RenderState.CullMode = CullMode.None;
            graphics.RenderState.AlphaBlendEnable = false;
        }

        public override void Initialize(BaseGame game)
        {
            base.Initialize(game);

            this.surfaceEffect = new BasicEffect(graphics, null);
            this.surfaceDeclaraction = new VertexDeclaration(
                graphics, VertexPositionTexture.VertexElements);

            terrainVertexDeclaration = new VertexDeclaration(
                graphics, TerrainVertex.VertexElements);

            // Load effect
            terrainEffect = game.ZipContent.Load<Effect>("Effects/MipmappedTerrain");

            // Initialize terrain vertices
            terrainVertices =
                new TerrainVertex[GridCountOnXAxis, GridCountOnYAxis];

            for (int x = 0; x < GridCountOnXAxis; x++)
                for (int y = 0; y < GridCountOnYAxis; y++)
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

            LoadManualContent();
        }

        /// <summary>
        /// Gets a terrain vertex at a given position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public TerrainVertex GetTerrainVertex(int x, int y)
        {
            return terrainVertices[x, y];
        }


#if SHORTINDEX
        UInt16[] workingIndices;
#else
        UInt32[] workingIndices;
#endif

        TerrainVertex[] workingVertices;

        uint terrainVertexCount;
        uint[] terrainIndexCount;

        Vector3 GetVertexPosition(int x, int y)
        {
            return terrainVertices[x, y].Position;
        }

        void SetVertexPosition(uint index, Vector3 position)
        {
            workingVertices[index].Position = position;
        }

        void SetVertex(uint index, int x, int y)
        {
            workingVertices[index] = terrainVertices[x, y];
        }

        private void UpdateTerrainVertexBuffer()
        {
            if (workingVertices == null)
            {
                //workingVertices = new TerrainVertex[
                //    6 * PatchCountOnXAxis * PatchCountOnYAxis *
                //    Patch.MaxPatchResolution * Patch.MaxPatchResolution];
                workingVertices = new TerrainVertex[GridCountOnXAxis * GridCountOnYAxis];
            }

            terrainVertexCount = 0;
            for (int i = 0; i < Patches.Count; i++)
            {
                if (Patches[i].Visible)
                {
                    // Update patch starting vertex
#if SHORTINDEX
                    Patches[i].StartingVertex = (UInt16)terrainVertexCount;
#else
                    Patches[i].StartingVertex = terrainVertexCount;
#endif
                    terrainVertexCount += Patches[i].FillVertices(terrainVertexCount,
                                                                  GetVertexPosition,
                                                                  SetVertexPosition,
                                                                  SetVertex);
                }
            }

            if (terrainVertexCount > 0)
            {
                terrainVertexBuffer.SetData<TerrainVertex>(
                    workingVertices, 0, (int)terrainVertexCount);
            }
        }

        private void UpdateTerrainIndexBufferSet()
        {
            if (workingIndices == null)
            {
#if SHORTINDEX
                workingIndices = new UInt16[
                    6 * PatchCountOnXAxis * PatchCountOnYAxis *
                    Patch.MaxPatchResolution * Patch.MaxPatchResolution];
#else
                workingIndices = new UInt32[
                    6 * PatchCountOnXAxis * PatchCountOnYAxis *
                    Patch.MaxPatchResolution * Patch.MaxPatchResolution];
#endif
                terrainIndexCount = new uint[PatchGroups.Length];
            }

            for (int i = 0; i < PatchGroups.Length; i++)
            {
                terrainIndexCount[i] = 0;
                foreach (int index in PatchGroups[i])
                {
                    if (Patches[index].Visible)
                        terrainIndexCount[i] += Patches[index].
#if SHORTINDEX
                            FillIndices16(ref workingIndices, terrainIndexCount[i]);
#else
                            FillIndices32(ref workingIndices, terrainIndexCount[i]);
#endif
                }

                if (terrainIndexCount[i] > 0)
                {
#if SHORTINDEX
                    // Clamp index count
                    if (terrainIndexCount[i] > UInt16.MaxValue)
                        terrainIndexCount[i] = UInt16.MaxValue;

                    terrainIndexBufferSet[i].SetData<UInt16>(
                        workingIndices, 0, (int)terrainIndexCount[i]);
#else
                    terrainIndexBufferSet[i].SetData<UInt32>(
                        workingIndices, 0, (int)terrainIndexCount[i]);
#endif
                }
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        protected virtual void DisposeTerrain()
        {
            if (terrainVertexBuffer != null)
                terrainVertexBuffer.Dispose();

            if (terrainEffect != null)
                terrainEffect.Dispose();

            foreach (IndexBuffer indexBuffer in terrainIndexBufferSet)
                indexBuffer.Dispose();

            foreach (Layer layer in Layers)
                layer.Dispose();
        }

        #endregion

        #region Update

        /// <summary>
        /// Update landscape every frame
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Get current view frustum from camera
            BoundingFrustum viewFrustum = game.ViewFrustum;

            bool visible;
            bool LODChanged = false;
            bool visibleAreaChanged = false;
            bool visibleAreaEnlarged = false;

            Vector3 eye = Vector3.Transform(Vector3.Zero, game.ViewInverse);

            for (int i = 0; i < Patches.Count; i++)
            {
                // Perform a bounding box test on each terrain patch
                visible = viewFrustum.Intersects(Patches[i].BoundingBox);
                if (visible != Patches[i].Visible)
                {
                    if (visible)
                        visibleAreaEnlarged = true;
                    visibleAreaChanged = true;
                    Patches[i].Visible = visible;
                }

                // Update patch LOD if patch visibility has changed
                if (Patches[i].UpdateLOD(eye, terrainErrorRatio))
                    LODChanged = true;
            }

            // No need to update anything if visibility hasn't changed.
            // (That means terrain LOD hasn't changed too)
            if (!visibleAreaChanged && !LODChanged)
                return;

            // If patch LOD hasn't changed and the visible area
            // isn't enlarged, we only need to update index buffers :)
            if (LODChanged || visibleAreaEnlarged)
                UpdateTerrainVertexBuffer();
            UpdateTerrainIndexBufferSet();
        }

        #endregion

        #region Device Lost & Reset

        /// <summary>
        /// Call this when device is reset
        /// </summary>
        void LoadManualContent()
        {
            // Initialize vertex buffer
            terrainVertexBuffer = new DynamicVertexBuffer(
                graphics,
                TerrainVertex.SizeInBytes * PatchCountOnXAxis * PatchCountOnYAxis *
                Patch.MaxPatchResolution * Patch.MaxPatchResolution,
                BufferUsage.WriteOnly);

            // Initialize index buffer
            for (int i = 0; i < PatchGroups.Length; i++)
            {
                // Note we use 16 bit index buffer now.
                // Some video card do not support 32 bit index buffer :(
                // Using LOD control, we are likely to limit the number
                // of 256 * 256 terrain triangles within 65535.
                int elementCount = 6 * PatchGroups[i].Count *
                    Patch.MaxPatchResolution * Patch.MaxPatchResolution;

#if SHORTINDEX
                if (elementCount > UInt16.MaxValue)
                    elementCount = UInt16.MaxValue;

                terrainIndexBufferSet.Add(new IndexBuffer(
                    graphics,typeof(UInt16), elementCount,BufferUsage.WriteOnly));
#else
                terrainIndexBufferSet.Add(new IndexBuffer(
                    graphics, typeof(UInt32), elementCount, BufferUsage.WriteOnly));
#endif
            }

            UpdateTerrainVertexBuffer();
            UpdateTerrainIndexBufferSet();
        }

        /// <summary>
        /// Call this when device is lost
        /// </summary>
        void UnloadManualContent()
        {
            if (terrainVertexBuffer != null)
                terrainVertexBuffer.Dispose();

            foreach (IndexBuffer indexBuffer in terrainIndexBufferSet)
                indexBuffer.Dispose();
            terrainIndexBufferSet.Clear();
        }

        public void Unload()
        {
            UnloadManualContent();
        }
        #endregion

        #region Terrain vertex
        /// <summary>
        /// Tangent vertex format for shader vertex format used all over the place.
        /// It contains: Position, Normal vector, texture coords, tangent vector.
        /// </summary>
        public struct TerrainVertex
        {
            // Grabbed from racing game :)

            #region Variables
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
            /// Stride Size, in XNA called SizeInBytes. I'm just conforming with that.
            /// </summary>
            public static int SizeInBytes
            {
                // 4 bytes per float:
                // 3 floats pos, 2 floats uv, 3 floats normal and 3 float tangent.
                get { return 4 * (3 + 2 + 3 + 3); }
            }

            /// <summary>
            /// U texture coordinate
            /// </summary>
            /// <returns>Float</returns>
            public float U
            {
                get { return TextureCoordinate.X; }
            }

            /// <summary>
            /// V texture coordinate
            /// </summary>
            /// <returns>Float</returns>
            public float V
            {
                get { return TextureCoordinate.Y; }
            }
            #endregion

            #region Constructor
            /// <summary>
            /// Create tangent vertex
            /// </summary>
            /// <param name="setPos">Set position</param>
            /// <param name="setU">Set u texture coordinate</param>
            /// <param name="setV">Set v texture coordinate</param>
            /// <param name="setNormal">Set normal</param>
            /// <param name="setTangent">Set tangent</param>
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
            /// Create tangent vertex
            /// </summary>
            /// <param name="setPos">Set position</param>
            /// <param name="setUv">Set uv texture coordinates</param>
            /// <param name="setNormal">Set normal</param>
            /// <param name="setTangent">Set tangent</param>
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
            #endregion

            #region Generate vertex declaration
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
                VertexElement[] decl = new VertexElement[]
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
            #endregion

            #region Is declaration tangent vertex declaration
            /// <summary>
            /// Returns true if declaration is tangent vertex declaration.
            /// </summary>
            public static bool IsTangentVertexDeclaration(
                VertexElement[] declaration)
            {
                if (declaration == null)
                    throw new ArgumentNullException("declaration");

                return
                    declaration.Length == 4 &&
                    declaration[0].VertexElementUsage == VertexElementUsage.Position &&
                    declaration[1].VertexElementUsage ==
                    VertexElementUsage.TextureCoordinate &&
                    declaration[2].VertexElementUsage == VertexElementUsage.Normal &&
                    declaration[3].VertexElementUsage == VertexElementUsage.Tangent;
            }
            #endregion
        }
        #endregion
    }
}
