// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public abstract class Landscape : BaseLandscape
    {
        public override void Load(TerrainData data, TextureLoader textureLoader)
        {
            base.Load(data, textureLoader);

            earthRadius = data.EarthRadius;
            waterTexture = textureLoader.LoadTexture(data.WaterTexture);
            waterDstortion = textureLoader.LoadTexture(data.WaterBumpTexture);

            Initialize(BaseGame.Singleton);
        }

        public override void Initialize(BaseGame game)
        {
            base.Initialize(game);

            WaterEffect = game.ShaderLoader.LoadShader("data/shaders/Water.fx");
            InitializeWater();

            surfaceEffect = game.ShaderLoader.LoadShader("data/shaders/Surface.fx");
            surfaceVertexBuffer = new DynamicVertexBuffer(game.GraphicsDevice,
                typeof(VertexPositionColorTexture), MaxSurfaceVertices, BufferUsage.WriteOnly);
            surfaceIndexBuffer = new DynamicIndexBuffer(game.GraphicsDevice,
                typeof(ushort), MaxSurfaceIndices, BufferUsage.WriteOnly);
        }

        /// <summary>
        /// Draw the terrain for water reflection and refraction.
        /// </summary>
        /// <param name="upper">Only draw upper part or underwater part.</param>
        public abstract void DrawTerrain(Matrix viewProjection, bool upper);

        public abstract void DrawTerrain(ShadowEffect shadowEffect);

        private struct TexturedSurface
        {
            public Texture2D Texture;
            public Vector3 Position;
            public Color Color;
            public float Width;
            public float Height;
        }

        private const int MaxSurfaceVertices = 512;
        private const int MaxSurfaceIndices = 768;
        private Effect surfaceEffect;
        private DynamicVertexBuffer surfaceVertexBuffer;
        private DynamicIndexBuffer surfaceIndexBuffer;
        private readonly List<ushort> surfaceIndices = new();
        private readonly List<VertexPositionColorTexture> surfaceVertices = new();
        private readonly LinkedList<TexturedSurface> texturedSurfaces = new();

        /// <summary>
        /// Draw a textured surface on top of the landscape. (But below all world objects).
        /// </summary>
        public void DrawSurface(Texture2D texture, Vector3 position, float width, float height, Color color)
        {
            TexturedSurface surface;
            surface.Color = color;
            surface.Texture = texture ?? throw new ArgumentNullException();
            surface.Position = position;

            // Plus a little offset
            surface.Position.Z = GetHeight(position.X, position.Y) + 6;

            // Divided by 2 so we don't have to do this during presentation
            surface.Width = width / 2;
            surface.Height = height / 2;

            // Add a new textured surface to the list.
            // Sort the entries by texture.
            LinkedListNode<TexturedSurface> p = texturedSurfaces.First;

            while (p != null)
            {
                if (p.Value.Texture != texture)
                {
                    break;
                }

                p = p.Next;
            }

            if (p == null)
            {
                texturedSurfaces.AddFirst(surface);
            }
            else
            {
                texturedSurfaces.AddBefore(p, surface);
            }
        }

        public void PresentSurface()
        {
            if (texturedSurfaces.Count <= 0)
            {
                return;
            }

            game.GraphicsDevice.SetRenderState(BlendState.AlphaBlend, DepthStencilState.Default);

            surfaceEffect.Parameters["WorldViewProjection"].SetValue(game.ViewProjection);

            surfaceEffect.CurrentTechnique.Passes[0].Apply();

            LinkedListNode<TexturedSurface> start = texturedSurfaces.First;
            LinkedListNode<TexturedSurface> end = texturedSurfaces.First;
            Texture2D texture = start.Value.Texture;

            while (end != null)
            {
                if (end.Value.Texture != texture && start != end)
                {
                    PresentSurface(start, end);

                    start = end;
                    texture = end.Value.Texture;
                }

                end = end.Next;
            }

            PresentSurface(start, end);

            texturedSurfaces.Clear();
        }

        private void PresentSurface(LinkedListNode<TexturedSurface> start,
                            LinkedListNode<TexturedSurface> end)
        {
            Texture2D texture = start.Value.Texture;
            VertexPositionColorTexture vertex;

            surfaceIndices.Clear();
            surfaceVertices.Clear();

            while (start != end)
            {
                // Quad indices
                surfaceIndices.Add((ushort)(surfaceVertices.Count + 0));
                surfaceIndices.Add((ushort)(surfaceVertices.Count + 1));
                surfaceIndices.Add((ushort)(surfaceVertices.Count + 3));
                surfaceIndices.Add((ushort)(surfaceVertices.Count + 3));
                surfaceIndices.Add((ushort)(surfaceVertices.Count + 1));
                surfaceIndices.Add((ushort)(surfaceVertices.Count + 2));

                // Create 4 quad vertices
                vertex.Color = start.Value.Color;
                vertex.Position = start.Value.Position;
                vertex.Position.X -= start.Value.Width;
                vertex.Position.Y += start.Value.Height;
                vertex.TextureCoordinate.X = 0.02f;
                vertex.TextureCoordinate.Y = 0.98f;
                surfaceVertices.Add(vertex);

                vertex.Position = start.Value.Position;
                vertex.Position.X += start.Value.Width;
                vertex.Position.Y += start.Value.Height;
                vertex.TextureCoordinate.X = 0.98f;
                vertex.TextureCoordinate.Y = 0.98f;
                surfaceVertices.Add(vertex);

                vertex.Position = start.Value.Position;
                vertex.Position.X += start.Value.Width;
                vertex.Position.Y -= start.Value.Height;
                vertex.TextureCoordinate.X = 0.98f;
                vertex.TextureCoordinate.Y = 0.02f;
                surfaceVertices.Add(vertex);

                vertex.Position = start.Value.Position;
                vertex.Position.X -= start.Value.Width;
                vertex.Position.Y -= start.Value.Height;
                vertex.TextureCoordinate.X = 0.02f;
                vertex.TextureCoordinate.Y = 0.02f;
                surfaceVertices.Add(vertex);

                start = start.Next;
            }

            // Draw user primitives
            surfaceEffect.Parameters["BasicTexture"].SetValue(texture);

            surfaceIndexBuffer.SetData(surfaceIndices.ToArray());
            surfaceVertexBuffer.SetData(surfaceVertices.ToArray());

            game.GraphicsDevice.Indices = surfaceIndexBuffer;
            game.GraphicsDevice.SetVertexBuffer(surfaceVertexBuffer);
            game.GraphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList, 0, 0, surfaceVertices.Count, 0, surfaceIndices.Count / 3);
        }

        /// <summary>
        /// Gets or sets the fog texture used to draw the landscape.
        /// </summary>
        public Texture2D FogTexture { get; set; }

        /// <summary>
        /// The water is part of a spherical surface to make it look vast.
        /// But during rendering, e.g., for computing reflection and refraction,
        /// the water is treated as a flat plane with the height of zero.
        /// This value determines the shape of the surface.
        /// </summary>
        private float earthRadius;

        /// <summary>
        /// A static texture applied to the water surface.
        /// </summary>
        private Texture waterTexture;

        /// <summary>
        /// This texture is used as a bump map to simulate water.
        /// </summary>
        private Texture waterDstortion;

        /// <summary>
        /// Render target used to draw the reflection & refraction texture.
        /// </summary>
        private RenderTarget2D reflectionRenderTarget;

        /// <summary>
        /// This texture is generated each frame for water reflection color sampling.
        /// </summary>
        private Texture2D waterReflection;

        /// <summary>
        /// Water mesh.
        /// </summary>
        private int waterVertexCount;
        private int waterPrimitiveCount;
        private VertexBuffer waterVertices;
        private IndexBuffer waterIndices;

        public Effect WaterEffect { get; set; }

        private void InitializeWater()
        {
            // Reflection & Refraction textures
            reflectionRenderTarget = new RenderTarget2D(
                graphics, 1024, 1024, true, SurfaceFormat.Color, graphics.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.DiscardContents);

            // Create vb / ib
            const int CellCount = 16;
            const int TextureRepeat = 32;

            waterVertexCount = (CellCount + 1) * (CellCount + 1);
            waterPrimitiveCount = CellCount * CellCount * 2;
            var vertexData = new VertexPositionTexture[waterVertexCount];

            // Water height is zero at the 4 corners of the terrain quad
            var highest = earthRadius - (float)Math.Sqrt(
                earthRadius * earthRadius - Size.X * Size.X / 4);

            var waterSize = Math.Max(Size.X, Size.Y) * 2;
            var cellSize = waterSize / CellCount;

            var i = 0;
            Vector2 pos;
            var center = new Vector2(Size.X / 2, Size.Y / 2);

            for (var y = 0; y <= CellCount; y++)
            {
                for (var x = 0; x <= CellCount; x++)
                {
                    pos.X = (Size.X - waterSize) / 2 + cellSize * x;
                    pos.Y = (Size.Y - waterSize) / 2 + cellSize * y;

                    var len = Vector2.Subtract(pos, center).Length();

                    vertexData[i].Position.X = pos.X;
                    vertexData[i].Position.Y = pos.Y;

                    // Make the water a sphere surface
                    vertexData[i].Position.Z = highest - earthRadius +
                            (float)Math.Sqrt(earthRadius * earthRadius - len * len);

                    vertexData[i].TextureCoordinate.X = 1.0f * x * TextureRepeat / CellCount;
                    vertexData[i].TextureCoordinate.Y = 1.0f * y * TextureRepeat / CellCount;

                    i++;
                }
            }

            var indexData = new short[waterPrimitiveCount * 3];

            i = 0;
            for (var y = 0; y < CellCount; y++)
            {
                for (var x = 0; x < CellCount; x++)
                {
                    indexData[i++] = (short)((CellCount + 1) * (y + 1) + x);     // 0
                    indexData[i++] = (short)((CellCount + 1) * y + x + 1);       // 2
                    indexData[i++] = (short)((CellCount + 1) * (y + 1) + x + 1); // 1
                    indexData[i++] = (short)((CellCount + 1) * (y + 1) + x);     // 0
                    indexData[i++] = (short)((CellCount + 1) * y + x);           // 3
                    indexData[i++] = (short)((CellCount + 1) * y + x + 1);       // 2
                }
            }

            waterVertices = new VertexBuffer(
                graphics, typeof(VertexPositionTexture), waterVertexCount, BufferUsage.WriteOnly);

            waterVertices.SetData(vertexData);

            waterIndices = new IndexBuffer(
                graphics, typeof(short), waterPrimitiveCount * 3, BufferUsage.WriteOnly);

            waterIndices.SetData(indexData);
        }

        public event DrawDelegate DrawWaterReflection;

        public delegate void DrawDelegate(GameTime gameTime, Matrix view, Matrix projection);

        /// <summary>
        /// Draw landscape into an environment map.
        /// Call this before anything is drawed.
        /// </summary>
        /// <param name="gameTime"></param>
        public void UpdateWaterReflectionAndRefraction(GameTime gameTime)
        {
            graphics.PushRenderTarget(reflectionRenderTarget);

            graphics.Clear(Color.Black);

            // Create a reflection view matrix
            var viewReflect = Matrix.Multiply(
                Matrix.CreateReflection(new Plane(Vector3.UnitZ, 0)), game.View);

            DrawTerrain(viewReflect * game.Projection, true);

            // Draw other reflections
            if (game.Settings.ReflectionEnabled)
            {
                DrawWaterReflection(gameTime, viewReflect, game.Projection);
            }

            // Present the model manager to draw those models
            game.ModelRenderer.Draw(viewReflect * game.Projection);

            // Draw refraction onto the reflection texture
            DrawTerrain(game.ViewProjection, false);

            graphics.PopRenderTarget();

            // Retrieve refraction texture
            waterReflection = reflectionRenderTarget;
        }

        public void DrawWater(GameTime gameTime)
        {
            graphics.SetRenderState(BlendState.Opaque, DepthStencilState.DepthRead, RasterizerState.CullNone);

            // Draw water mesh
            graphics.Indices = waterIndices;
            graphics.SetVertexBuffer(waterVertices);

            if (FogTexture != null)
            {
                WaterEffect.Parameters["FogTexture"].SetValue(FogTexture);
            }

            WaterEffect.Parameters["ColorTexture"].SetValue(waterTexture);

            if (game.Settings.ReflectionEnabled)
            {
                WaterEffect.CurrentTechnique = WaterEffect.Techniques["Realisic"];
                WaterEffect.Parameters["ReflectionTexture"].SetValue(waterReflection);
            }
            else
            {
                WaterEffect.CurrentTechnique = WaterEffect.Techniques["Default"];
            }

            WaterEffect.Parameters["DistortionTexture"].SetValue(waterDstortion);
            WaterEffect.Parameters["ViewInverse"].SetValue(game.ViewInverse);
            WaterEffect.Parameters["WorldViewProj"].SetValue(game.ViewProjection);
            WaterEffect.Parameters["WorldView"].SetValue(game.View);
            WaterEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.01f));

            WaterEffect.CurrentTechnique.Passes[0].Apply();

            graphics.DrawIndexedPrimitives(
                PrimitiveType.TriangleList, 0, 0, waterVertexCount, 0, waterPrimitiveCount);
        }

        /// <summary>
        /// Helper for moving a value around in a circle.
        /// </summary>
        private static Vector2 MoveInCircle(GameTime gameTime, float speed)
        {
            var time = gameTime.TotalGameTime.TotalSeconds * speed;

            var x = (float)Math.Cos(time);
            var y = (float)Math.Sin(time);

            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// Game fog of war.
    /// </summary>
    public class FogMask
    {
        private const int Size = 128;

        /// <summary>
        /// Gets the default glow texture for each unit.
        /// </summary>
        public static Texture2D Glow
        {
            get
            {
                if (glow == null || glow.IsDisposed)
                {
                    glow = BaseGame.Singleton.TextureLoader.LoadTexture("data/ui/glow.png");
                }

                return glow;
            }
        }

        private static Texture2D glow;

        /// <summary>
        /// Gets the width of the mask.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height of the mask.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Objects are invisible when the intensity is below this value.
        /// </summary>
        public const float VisibleIntensity = 0.5f;

        /// <summary>
        /// Common stuff.
        /// </summary>
        private readonly GraphicsDevice graphics;
        private readonly SpriteBatch sprite;
        private Rectangle textureRectangle;

        /// <summary>
        /// Gets the result mask texture (Fog of war).
        /// </summary>
        public Texture2D Mask { get; private set; }

        private RenderTarget2D allFramesVisibleArea;
        private readonly RenderTarget2D thisFrameVisibleArea;
        private readonly RenderTarget2D maskCanvas;

        /// <summary>
        /// Fog intensities.
        /// </summary>
        private readonly bool[] visibility;

        /// <summary>
        /// Visible areas.
        /// </summary>
        private struct Entry
        {
            public float Radius;
            public Vector2 Position;
        }

        private readonly List<Entry> visibleAreas = new();

        /// <summary>
        /// Creates a new fog of war mask.
        /// </summary>
        public FogMask(GraphicsDevice graphics, float width, float height)
        {
            if (graphics == null || width <= 0 || height <= 0)
            {
                throw new ArgumentException();
            }

            Width = width;
            Height = height;
            this.graphics = graphics;
            sprite = new SpriteBatch(graphics);
            visibility = new bool[Size * Size];
            textureRectangle = new Rectangle(0, 0, Size, Size);
            thisFrameVisibleArea = new RenderTarget2D(graphics, Size, Size, true, SurfaceFormat.Color, graphics.PresentationParameters.DepthStencilFormat);
            maskCanvas = new RenderTarget2D(graphics, Size, Size, true, SurfaceFormat.Color, graphics.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
        }

        /// <summary>
        /// Gets the whether the specified point is in the fog of war.
        /// </summary>
        public bool Contains(float x, float y)
        {
            return x <= 0 || y <= 0 || x >= Width || y >= Height || !visibility[Size * (int)(Size * y / Height) + (int)(Size * x / Width)];
        }

        /// <summary>
        /// Call this each frame to mark an area as visible.
        /// </summary>
        /// <remarks>
        /// TODO: Custom glow texture.
        /// </remarks>
        public void DrawVisibleArea(float radius, float x, float y)
        {
            Entry entry;

            entry.Radius = radius;
            entry.Position.X = x;
            entry.Position.Y = y;

            visibleAreas.Add(entry);
        }

        /// <summary>
        /// Call this to refresh the fog of war texture.
        /// </summary>
        public void Refresh()
        {
            if (Mask != null && visibleAreas.Count <= 0)
            {
                return;
            }

            // Draw current glows
            graphics.PushRenderTarget(thisFrameVisibleArea);
            graphics.Clear(Color.Black);

            // Draw glows
            sprite.Begin(SpriteSortMode.Immediate, BlendState.Additive);

            Rectangle destination;
            foreach (Entry entry in visibleAreas)
            {
                destination.X = (int)(Size * (entry.Position.X - entry.Radius) / Width);
                destination.Y = (int)(Size * (entry.Position.Y - entry.Radius) / Height);
                destination.Width = (int)(Size * entry.Radius * 2 / Width);
                destination.Height = (int)(Size * entry.Radius * 2 / Height);

                // Draw the glow texture
                sprite.Draw(Glow, destination, Color.White);
            }

            sprite.End();

            // Draw discovered area texture without clearing it
            if (allFramesVisibleArea is null)
            {
                allFramesVisibleArea = new RenderTarget2D(graphics, Size, Size, true, SurfaceFormat.Color, graphics.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
                graphics.SetRenderTarget(allFramesVisibleArea);
                graphics.Clear(Color.Black);
            }
            else
            {
                graphics.SetRenderTarget(allFramesVisibleArea);
            }

            sprite.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            sprite.Draw(thisFrameVisibleArea, textureRectangle, Color.White);
            sprite.End();

            // Draw final mask texture
            graphics.SetRenderTarget(maskCanvas);
            graphics.Clear(Color.Black);

            sprite.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            sprite.Draw(allFramesVisibleArea, textureRectangle, Color.Gray * 0.5f);
            sprite.Draw(thisFrameVisibleArea, textureRectangle, Color.White);
            sprite.End();

            // Restore states
            graphics.PopRenderTarget();

            Mask = maskCanvas;

            // Manually update intensity map
            UpdateIntensity();

            // Clear visible areas
            visibleAreas.Clear();
        }

        private void UpdateIntensity()
        {
            for (var i = 0; i < visibility.Length; i++)
            {
                visibility[i] = false;
            }

            var CellWidth = Width / Size;
            var CellHeight = Height / Size;

            foreach (Entry entry in visibleAreas)
            {
                var minX = (int)(Size * (entry.Position.X - entry.Radius) / Width);
                var minY = (int)(Size * (entry.Position.Y - entry.Radius) / Height);
                var maxX = (int)(Size * (entry.Position.X + entry.Radius) / Width) + 1;
                var maxY = (int)(Size * (entry.Position.Y + entry.Radius) / Height) + 1;

                if (minX < 0)
                {
                    minX = 0;
                }

                if (minY < 0)
                {
                    minY = 0;
                }

                if (maxX >= Size)
                {
                    maxX = Size - 1;
                }

                if (maxY >= Size)
                {
                    maxY = Size - 1;
                }

                for (var y = minY; y <= maxY; y++)
                {
                    for (var x = minX; x <= maxX; x++)
                    {
                        Vector2 v;

                        v.X = x * CellWidth + CellWidth / 2 - entry.Position.X;
                        v.Y = y * CellHeight + CellHeight / 2 - entry.Position.Y;

                        if (v.LengthSquared() <= entry.Radius * entry.Radius)
                        {
                            visibility[y * Size + x] = true;
                        }
                    }
                }
            }
        }
    }
}
