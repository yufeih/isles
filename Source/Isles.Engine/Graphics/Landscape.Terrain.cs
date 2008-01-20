//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    public partial class Landscape
    {
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
        /// All terrain patches
        /// </summary>
        List<Patch> patches;

        /// <summary>
        /// Describes the patch count
        /// </summary>
        int xPatchCount, yPatchCount;

        /// <summary>
        /// All terrain layers
        /// </summary>
        List<Layer> layers = new List<Layer>();

        /// <summary>
        /// Patch groups
        /// </summary>
        List<int>[] patchGroups;

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

        /// <summary>
        /// Bounding box of the terrain
        /// </summary>
        BoundingBox terrainBoundingBox;

        #endregion

        #region Methods

        void InitializeTerrain()
        {
            terrainVertexDeclaration = new VertexDeclaration(
                graphics, TerrainVertex.VertexElements);

            LoadManualContent();
        }

        void DrawTerrain(Matrix lightViewProjection, Texture shadowMap)
        {
            if (terrainVertexCount == 0)
                return;

            // This code would go between a device 
            // BeginScene-EndScene block.
            graphics.Vertices[0].SetSource(
                terrainVertexBuffer, 0, TerrainVertex.SizeInBytes);
            graphics.VertexDeclaration = terrainVertexDeclaration;

            Matrix viewInv = Matrix.Invert(game.View);
            terrainEffect.Parameters["ViewInv"].SetValue(viewInv);
            terrainEffect.Parameters["WorldViewProj"].SetValue(game.ViewProjection);

            if (shadowMap != null)
            {
                terrainEffect.Parameters["ShadowMap"].SetValue(shadowMap);
                terrainEffect.Parameters["LightWorldViewProj"].SetValue(lightViewProjection);
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

                for (int i = 0; i < layers.Count; i++)
                {
                    // Disable alpha blending when drawing our first layer
                    graphics.RenderState.AlphaBlendEnable = (i != 0);

                    // It turns out that set indices are soo expensive when drawing
                    // the terrain with simple shaders. But for complex shaders, such
                    // as normal mapping, it's better to use the patch group :)
                    graphics.Indices = terrainIndexBufferSet[layers[i].PatchGroup];

                    terrainEffect.Parameters["ColorTexture"].SetValue(layers[i].ColorTexture);
                    terrainEffect.Parameters["AlphaTexture"].SetValue(layers[i].AlphaTexture);
                    terrainEffect.Parameters["NormalTexture"].SetValue(layers[i].NormalTexture);
                    terrainEffect.CommitChanges();

                    if (terrainIndexCount[layers[i].PatchGroup] != 0)
                    {
                        layerCount++;
                        patchCount += (int)terrainIndexCount[layers[i].PatchGroup] / 3;
                        graphics.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            0, 0, (int)terrainVertexCount,
                            0, (int)terrainIndexCount[layers[i].PatchGroup] / 3);
                    }
                }
                pass.End();
            }
            terrainEffect.End();
        }

        void ReadTerrainContent(ContentReader input)
        {
            // Load effect
            terrainEffect = input.ContentManager.Load<Effect>("Effects/Terrain");

            terrainBoundingBox = new BoundingBox(Vector3.Zero, size);

            // Heightfield
            gridColumnCount = input.ReadInt32();
            gridRowCount = input.ReadInt32();
            heightfield = new float[gridColumnCount, gridRowCount];
            for (int y = 0; y < gridRowCount; y++)
                for (int x = 0; x < gridColumnCount; x++)
                {
                    // Remember how we write heighfield data
                    heightfield[x, y] = input.ReadSingle();
                }

            // Normals
            Vector3[,] normalData = new Vector3[gridColumnCount, gridRowCount];
            for (int y = 0; y < gridRowCount; y++)
                for (int x = 0; x < gridColumnCount; x++)
                    normalData[x, y] = input.ReadVector3();

            // Tangents
            Vector3[,] tangentData = new Vector3[gridColumnCount, gridRowCount];
            for (int y = 0; y < gridRowCount; y++)
                for (int x = 0; x < gridColumnCount; x++)
                    tangentData[x, y] = input.ReadVector3();

            // Patches
            xPatchCount = input.ReadInt32();
            yPatchCount = input.ReadInt32();
            patches = new List<Patch>(xPatchCount * yPatchCount);
            for (int i = 0; i < xPatchCount * yPatchCount; i++)
                patches.Add(new Patch(input, i, this));

            // Patch groups
            int patchGroupCount = input.ReadInt32();
            patchGroups = new List<int>[patchGroupCount];
            for (int i = 0; i < patchGroupCount; i++)
            {
                int n = input.ReadInt32();
                patchGroups[i] = new List<int>(n);
                for (int k = 0; k < n; k++)
                    patchGroups[i].Add(input.ReadInt32());
            }

            // Layers
            int layerCount = input.ReadInt32();
            layers = new List<Layer>(layerCount);
            for (int i = 0; i < layerCount; i++)
                layers.Add(new Layer(input));

            // Initialize terrain vertices
            terrainVertices =
                new TerrainVertex[gridColumnCount, gridRowCount];

            for (int x = 0; x < gridColumnCount; x++)
                for (int y = 0; y < gridRowCount; y++)
                {
                    terrainVertices[x, y] = new TerrainVertex(
                        // Position
                        new Vector3(x * size.X / (gridColumnCount - 1),
                                    y * size.Y / (gridRowCount - 1), GetGridHeight(x, y)),
                        // Texture coordinate
                        new Vector2(x * 16.0f / (gridColumnCount - 1), y * 16.0f / (gridRowCount - 1)),
                        // Normal
                        normalData[x, y],
                        // Tangent
                        tangentData[x, y]
                    );
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

            foreach (Layer layer in layers)
                layer.Dispose();
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
                TerrainVertex.SizeInBytes * xPatchCount * yPatchCount *
                Patch.MaxPatchResolution * Patch.MaxPatchResolution,
                BufferUsage.WriteOnly);

            // Initialize index buffer
            for (int i = 0; i < patchGroups.Length; i++)
            {
                terrainIndexBufferSet.Add(
                    new IndexBuffer(
                        graphics,
                        typeof(uint),
                        6 * patchGroups[i].Count *
                        Patch.MaxPatchResolution * Patch.MaxPatchResolution,
                        BufferUsage.WriteOnly));
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
            /// Stride size, in XNA called SizeInBytes. I'm just conforming with that.
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
