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
        Texture waterTexture;
        Texture waterDstortion;
        Effect waterEffect;

        VertexBuffer waterVertices;
        IndexBuffer waterIndices;

        VertexDeclaration waterVertexDeclaration;

        public Effect WaterEffect
        {
            get { return waterEffect; }
            set { waterEffect = value; }
        }

        void ReadWaterContent(ContentReader input)
        {
            waterTexture = input.ReadExternalReference<Texture>();
            waterEffect = input.ContentManager.Load<Effect>("Effects/Water");
            waterDstortion = input.ContentManager.Load<Texture>("Textures/Distortion");
        }

        void InitializeWater()
        {
            waterVertexDeclaration = new VertexDeclaration(
                graphics, VertexPositionTexture.VertexElements);

            // Create vb / ib
            VertexPositionTexture[] vertexData = new VertexPositionTexture[]
            {
                new VertexPositionTexture(
                    new Vector3(-3 * size.X, -3 * size.Y, 0),
                    new Vector2(0, 0)),
                new VertexPositionTexture(
                    new Vector3(4 * size.X, -3 * size.Y, 0),
                    new Vector2(4, 0)),
                new VertexPositionTexture(
                    new Vector3(4 * size.X, 4 * size.Y, 0),
                    new Vector2(4, 4)),
                new VertexPositionTexture(
                    new Vector3(-3 * size.X, 4 * size.Y, 0),
                    new Vector2(0, 4))
            };

            short[] indexData = new short[] { 0, 1, 2, 0, 2, 3 };

            waterVertices = new VertexBuffer(
                graphics, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);

            waterVertices.SetData<VertexPositionTexture>(vertexData);

            waterIndices = new IndexBuffer(
                graphics, typeof(short), 6, BufferUsage.WriteOnly);

            waterIndices.SetData<short>(indexData);
        }

        void DrawWater(GameTime gameTime)
        {
            graphics.Indices = waterIndices;
            graphics.VertexDeclaration = waterVertexDeclaration;
            graphics.Vertices[0].SetSource(waterVertices, 0, VertexPositionTexture.SizeInBytes);

            waterEffect.Parameters["ColorTexture"].SetValue(waterTexture);
            waterEffect.Parameters["DistortionTexture"].SetValue(waterDstortion);
            waterEffect.Parameters["WorldViewProj"].SetValue(game.ViewProjection);
            waterEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.02f));

            waterEffect.Begin();
            foreach (EffectPass pass in waterEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                graphics.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
                pass.End();
            }
            waterEffect.End();
        }

        /// <summary>
        /// Helper for moving a value around in a circle.
        /// </summary>
        static Vector2 MoveInCircle(GameTime gameTime, float speed)
        {
            double time = gameTime.TotalGameTime.TotalSeconds * speed;

            float x = (float)Math.Cos(time);
            float y = (float)Math.Sin(time);

            return new Vector2(x, y);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        void DisposeWater()
        {
            if (waterVertices != null)
                waterVertices.Dispose();
            if (waterIndices != null)
                waterIndices.Dispose();
        }
    }
}
