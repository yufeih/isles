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
        float earthRadius;
        Texture waterTexture;
        Texture waterDstortion;
        Effect waterEffect;
        int waterVertexCount;
        int waterPrimitiveCount;

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
            earthRadius = input.ReadSingle();
            waterTexture = input.ReadExternalReference<Texture>();
            waterEffect = input.ContentManager.Load<Effect>("Effects/Water");
            waterDstortion = input.ContentManager.Load<Texture>("Textures/Distortion");
        }

        void InitializeWater()
        {
            waterVertexDeclaration = new VertexDeclaration(
                graphics, VertexPositionTexture.VertexElements);

            // Create vb / ib
            const int CellCount = 16;
            const int TextureRepeat = 4;

            waterVertexCount = (CellCount + 1) * (CellCount + 1);
            waterPrimitiveCount = CellCount * CellCount * 2;
            VertexPositionTexture[] vertexData = new VertexPositionTexture[waterVertexCount];

            // Water height is zero at the 4 corners of the terrain quad
            float highest = earthRadius - (float)Math.Sqrt(
                earthRadius * earthRadius - size.X * size.X / 4);

            float waterSize = Math.Max(size.X, size.Y) * 8;
            float cellSize = waterSize / CellCount;

            int i = 0;
            float len = 0;
            Vector2 pos;
            Vector2 center = new Vector2(size.X / 2, size.Y / 2);

            for (int y = 0; y <= CellCount; y++)
                for (int x = 0; x <= CellCount; x++)
                {
                    pos.X = (size.X - waterSize) / 2 + cellSize * x;
                    pos.Y = (size.Y - waterSize) / 2 + cellSize * y;

                    len = Vector2.Subtract(pos, center).Length();

                    vertexData[i].Position.X = pos.X;
                    vertexData[i].Position.Y = pos.Y;

                    // Make the water a sphere surface
                    vertexData[i].Position.Z = highest - earthRadius +
                        (float)Math.Sqrt(earthRadius * earthRadius - len * len);

                    vertexData[i].TextureCoordinate.X = (float)x * TextureRepeat / CellCount;
                    vertexData[i].TextureCoordinate.Y = (float)y * TextureRepeat / CellCount;

                    i++;
                }

            short[] indexData = new short[waterPrimitiveCount * 3];

            i = 0;
            for (int y = 0; y < CellCount; y++)
                for (int x = 0; x < CellCount; x++)
                {
                    indexData[i++] = (short)((CellCount + 1) * (y + 1) + x);     // 0
                    indexData[i++] = (short)((CellCount + 1) * y + x + 1);       // 2
                    indexData[i++] = (short)((CellCount + 1) * (y + 1) + x + 1); // 1
                    indexData[i++] = (short)((CellCount + 1) * (y + 1) + x);     // 0
                    indexData[i++] = (short)((CellCount + 1) * y + x);           // 3
                    indexData[i++] = (short)((CellCount + 1) * y + x + 1);       // 2
                }


            waterVertices = new VertexBuffer(
                graphics, typeof(VertexPositionTexture), waterVertexCount, BufferUsage.WriteOnly);

            waterVertices.SetData<VertexPositionTexture>(vertexData);

            waterIndices = new IndexBuffer(
                graphics, typeof(short), waterPrimitiveCount * 3, BufferUsage.WriteOnly);

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
            waterEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.01f));

            waterEffect.Begin();
            foreach (EffectPass pass in waterEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                graphics.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, waterVertexCount, 0, waterPrimitiveCount);
                pass.End();
            }
            waterEffect.End();

            graphics.RenderState.DepthBufferWriteEnable = true;
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
