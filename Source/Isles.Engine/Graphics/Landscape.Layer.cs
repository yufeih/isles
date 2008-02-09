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
    /// <summary>
    /// Manipulate terrain, patches for landscape visualization.
    /// Uses Geometry Mipmapping to generate continously level of detailed terrain.
    /// Uses multi-pass technology to render different terrain layers (textures)
    /// </summary>
    public partial class Landscape
    {
        #region Layer
        /// <summary>
        /// Represents a texture layer on the terrain
        /// </summary>
        public class Layer : IDisposable
        {
            int patchGroup;
            string technology;

            Texture2D colorTexture;
            Texture2D alphaTexture;
            Texture2D normalTexture;

            /// <summary>
            /// Gets Which patch group the layer is in
            /// </summary>
            public int PatchGroup
            {
                get { return patchGroup; }
            }

            /// <summary>
            /// Gets or sets the technology used to render this layer
            /// </summary>
            public string Technology
            {
                get { return technology; }
                set { technology = value; }
            }

            /// <summary>
            /// Gets or sets the color texture of this layer
            /// </summary>
            public Texture2D ColorTexture
            {
                get { return colorTexture; }
                set { colorTexture = value; }
            }

            /// <summary>
            /// Gets the alpha texture of this layer
            /// </summary>
            /// <remarks>
            /// Alpha texture can't be set since it will affect the patch group
            /// </remarks>
            public Texture2D AlphaTexture
            {
                get { return alphaTexture; }
            }

            /// <summary>
            /// Gets or sets the normal texture of this layer
            /// </summary>
            public Texture2D NormalTexture
            {
                get { return normalTexture; }
                set { normalTexture = value; }
            }

            /// <summary>
            /// Create a layer from a content input
            /// </summary>
            /// <param name="input"></param>
            public Layer(ContentReader input)
            {
                patchGroup = input.ReadInt32();
                technology = input.ReadString();
                colorTexture = input.ReadExternalReference<Texture2D>();
                alphaTexture = input.ReadExternalReference<Texture2D>();
                normalTexture = input.ReadExternalReference<Texture2D>();
            }
            
            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose
            /// </summary>
            /// <param name="disposing">Disposing</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (colorTexture != null)
                        colorTexture.Dispose();
                    if (alphaTexture != null)
                        alphaTexture.Dispose();
                    if (normalTexture != null)
                        normalTexture.Dispose();
                }
            }
        }
        #endregion

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
        /// <param name="size">Texture size</param>
        public void DrawSurface(Texture2D texture, Vector2 position, Vector2 size)
        {
            // Generate a mesh for drawing the texture at a given position
            Vector2 vMin = position - size / 2;
            Vector2 vMax = position + size / 2;

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
                    if (x >= 0 && x < gridCountOnXAxis &&
                        y >= 0 && y < gridCountOnYAxis && heightField[x, y] > 0)
                        z = heightField[x, y] + 0.5f; // Offset a little bit :)
                    else
                        z = 0;

                    vertices[iVertex++] = new VertexPositionTexture(
                        new Vector3(v, z), (v - vMin) / size);
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
    }
}
