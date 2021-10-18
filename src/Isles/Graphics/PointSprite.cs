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
    /// A pointSprite definition.
    /// </summary>
    public struct PointSprite
    {
        /// <summary>
        /// Texture used to draw the pointSprite.
        /// </summary>
        public Texture2D Texture;

        /// <summary>
        /// Position of the point sprite.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Size of the pointSprite.
        /// </summary>
        public float Size;
    }

    /// <summary>
    /// Manager class for pointSprite.
    /// </summary>
    public class PointSpriteManager : IDisposable
    {
        /// <summary>
        /// Max number of quads that can be rendered in one draw call.
        /// </summary>
        public const int ChunkSize = 1024;

        /// <summary>
        /// PointSprite effect.
        /// </summary>
        private readonly Effect effect;

        /// <summary>
        /// Internal pointSprite list.
        /// </summary>
        private readonly List<PointSprite> pointSprites = new();

        /// <summary>
        /// Point sprite vertices.
        /// </summary>
        private readonly DynamicVertexBuffer vertices;

        /// <summary>
        /// Vertex buffer used to generate vertices.
        /// </summary>
        private readonly VertexPositionTexture[] workingVertices = new VertexPositionTexture[ChunkSize];

        /// <summary>
        /// Graphics device.
        /// </summary>
        private readonly BaseGame game;

        /// <summary>
        /// Create a pointSprite manager.
        /// </summary>
        public PointSpriteManager(BaseGame game)
        {
            this.game = game;

            // Initialize pointSprite effect
            effect = game.ZipContent.Load<Effect>("Effects/PointSprite");

            // Create vertices
            vertices = new DynamicVertexBuffer(game.GraphicsDevice,
                typeof(VertexPositionTexture), ChunkSize, BufferUsage.WriteOnly);
        }

        /// <summary>
        /// Draw a pointSprite.
        /// </summary>
        public void Draw(Texture2D texture, Vector3 position, float size)
        {
            PointSprite pointSprite;

            pointSprite.Texture = texture;
            pointSprite.Position = position;
            pointSprite.Size = size;

            Draw(pointSprite);
        }

        /// <summary>
        /// Draw a pointSprite.
        /// </summary>
        /// <param name="pointSprite"></param>
        public void Draw(PointSprite pointSprite)
        {
            pointSprites.Add(pointSprite);
        }

        /// <summary>
        /// Draw all pointSprites in this frame.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Present(GameTime gameTime)
        {
            if (pointSprites.Count <= 0)
            {
                return;
            }

            game.GraphicsDevice.VertexDeclaration = new VertexDeclaration(
                game.GraphicsDevice, VertexPositionTexture.VertexElements);

            // Set effect parameters
            effect.Parameters["View"].SetValue(game.View);
            effect.Parameters["Projection"].SetValue(game.Projection);

            Texture texture = pointSprites[0].Texture;

            var baseVertex = 0;
            int begin = 0, end = 0;
            for (var i = 0; i <= pointSprites.Count; i++)
            {
                // We are at the end of the chunk
                if (i != pointSprites.Count && // End of list
                   (i - begin) < ChunkSize && texture == pointSprites[i].Texture)
                {
                    continue;
                }

                end = i;

                if (end == begin)
                {
                    continue;
                }

                // Setup graphics device
                game.GraphicsDevice.Vertices[0].SetSource(null, 0, 0);
                game.GraphicsDevice.Indices = null;

                // Build the mesh for this chunk of pointSprites
                baseVertex = 0;
                for (var k = begin; k < end; k++)
                {
                    workingVertices[baseVertex].Position = pointSprites[k].Position;
                    workingVertices[baseVertex].TextureCoordinate.X = pointSprites[k].Size;
                    baseVertex++;
                }

                // Update vertex/index buffer
                vertices.SetData(workingVertices, 0, baseVertex);

                // Setup graphics device
                game.GraphicsDevice.Vertices[0].SetSource(
                    vertices, 0, VertexPositionTexture.SizeInBytes);

                // Set effect texture
                effect.Parameters["Texture"].SetValue(pointSprites[begin].Texture);

                // Draw the chunk
                effect.Begin(SaveStateMode.SaveState);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Begin();

                    game.GraphicsDevice.DrawPrimitives(
                        PrimitiveType.PointList, 0, baseVertex);

                    pass.End();
                }

                effect.End();

                // Increment begin pointer
                begin = end;
            }

            // The pointSprite effect sets some unusual renderstates for
            // alphablending and depth testing the vegetation. We need to
            // put these back to the right settings for the ground geometry.
            game.GraphicsDevice.RenderState.AlphaBlendEnable = false;
            game.GraphicsDevice.RenderState.PointSpriteEnable = false;
            game.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            game.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            // Clear internal list after drawing
            pointSprites.Clear();
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (effect != null)
                {
                    effect.Dispose();
                }

                if (vertices != null)
                {
                    vertices.Dispose();
                }
            }
        }
    }
}
