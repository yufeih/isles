//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    #region Landscape
    /// <summary>
    /// Landscape without terrain
    /// </summary>
    public abstract class Landscape : BaseLandscape
    {
        #region Methods
        /// <summary>
        /// Initialize landscape from XNB file
        /// </summary>
        /// <param name="input"></param>
        public override void ReadContent(ContentReader input)
        {
            base.ReadContent(input);

            // Initialize Water
            ReadWaterContent(input);

            // Initialize sky
            ReadSkyContent(input);

            // Initialize vegetation
            ReadVegetationContent(input);

            // Initialize everything
            Initialize(BaseGame.Singleton);

            // Log landscape info
            Log.Write("Landscape loaded...");
        }

        /// <summary>
        /// Call this everytime a landscape is loaded
        /// </summary>
        public override void Initialize(BaseGame game)
        {
            base.Initialize(game);

            InitializeWater();
            InitializeSky();

            surfaceEffect = game.ZipContent.Load<Effect>("Effects/Surface");
            surfaceDeclaration = new VertexDeclaration(game.GraphicsDevice,
                                     VertexPositionColorTexture.VertexElements);
            surfaceVertexBuffer = new DynamicVertexBuffer(game.GraphicsDevice,
                typeof(VertexPositionTexture), MaxSurfaceVertices, BufferUsage.WriteOnly);
            surfaceIndexBuffer = new DynamicIndexBuffer(game.GraphicsDevice,
                typeof(ushort), MaxSurfaceIndices, BufferUsage.WriteOnly);
        }
        #endregion

        #region Draw

        /// <summary>
        /// Draw the terrain for water reflection and refraction
        /// </summary>
        /// <param name="upper">Only draw upper part or underwater part</param>
        public abstract void DrawTerrain(Matrix view, Matrix projection, bool upper);
        public abstract void DrawTerrain(GameTime gameTime, ShadowEffect shadowEffect);

        /// <summary>
        /// Draw landscape
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            DrawSky(gameTime, game.View, game.Projection);
            DrawWater(gameTime);
            DrawTerrain(gameTime, null);

            // FIXME but this grass is soo ugly...
            //DrawVegetation(gameTime);
            //DrawGridStates();
        }

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
        private VertexDeclaration surfaceDeclaration;
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

        public void PresentSurface(GameTime gameTime)
        {
            if (texturedSurfaces.Count <= 0)
            {
                return;
            }

            graphics.VertexDeclaration = surfaceDeclaration;

            game.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            game.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            surfaceEffect.Parameters["WorldViewProjection"].SetValue(game.ViewProjection);

            surfaceEffect.Begin();
            foreach (EffectPass pass in surfaceEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

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

                pass.End();
            }
            surfaceEffect.End();

            texturedSurfaces.Clear();
        }

        private void PresentSurface(LinkedListNode<TexturedSurface> start,
                            LinkedListNode<TexturedSurface> end)
        {
            Texture2D texture = start.Value.Texture;
            VertexPositionColorTexture vertex;

            #region BuildVertices
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
            #endregion

            // Draw user primitives
            surfaceEffect.Parameters["BasicTexture"].SetValue(texture);

            surfaceIndexBuffer.SetData(surfaceIndices.ToArray());
            surfaceVertexBuffer.SetData(surfaceVertices.ToArray());

            game.GraphicsDevice.Indices = surfaceIndexBuffer;
            game.GraphicsDevice.Vertices[0].SetSource(surfaceVertexBuffer, 0,
                                                      VertexPositionColorTexture.SizeInBytes);
            game.GraphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList, 0, 0, surfaceVertices.Count, 0, surfaceIndices.Count / 3);
        }

        #endregion

        #region Sky

        private TextureCube skyTexture;
        private Effect skyEffect;
        private Model skyModel;

        private void ReadSkyContent(ContentReader input)
        {
            skyEffect = input.ContentManager.Load<Effect>("Effects/Sky");
            skyModel = input.ContentManager.Load<Model>("Models/Cube");
            skyTexture = input.ReadExternalReference<TextureCube>();
        }

        private void InitializeSky()
        {
        }

        public void DrawSky(GameTime gameTime)
        {
            DrawSky(gameTime, game.View, game.Projection);
        }

        private void DrawSky(GameTime gameTime, Matrix view, Matrix projection)
        {
            // We have to retrieve the new graphics device every frame,
            // since graphics device will be changed when resetting.
            graphics = game.GraphicsDevice;

            // Don't use or write to the z buffer
            graphics.RenderState.DepthBufferEnable = false;
            graphics.RenderState.DepthBufferWriteEnable = false;
            graphics.RenderState.CullMode = CullMode.None;

            // Also don't use any kind of blending.
            graphics.RenderState.AlphaBlendEnable = false;

            skyEffect.Parameters["View"].SetValue(view);
            skyEffect.Parameters["Projection"].SetValue(projection);
            skyEffect.Parameters["CubeTexture"].SetValue(skyTexture);

            // Override model's effect and render
            skyModel.Meshes[0].MeshParts[0].Effect = skyEffect;
            skyModel.Meshes[0].Draw();

            // Reset previous render states
            graphics.RenderState.DepthBufferEnable = true;
            graphics.RenderState.DepthBufferWriteEnable = true;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        private void DisposeSky()
        {
            if (skyTexture != null)
            {
                skyTexture.Dispose();
            }

            if (skyEffect != null)
            {
                skyEffect.Dispose();
            }
        }

        #endregion

        #region Vegetation

        private readonly float grassViewDistanceSquared = 400000;
        private readonly List<Billboard> vegetations = new(512);

        private void ReadVegetationContent(ContentReader input)
        {
            var count = input.ReadInt32();

            Vector2 position;
            var billboard = new Billboard();
            Texture2D texture;
            for (var i = 0; i < count; i++)
            {
                texture = input.ReadExternalReference<Texture2D>();

                var n = input.ReadInt32();

                for (var k = 0; k < n; k++)
                {
                    billboard.Texture = texture;
                    position = input.ReadVector2();
                    billboard.Position = new Vector3(position, GetHeight(position.X, position.Y));
                    billboard.Size = input.ReadVector2();
                    billboard.Normal = Vector3.UnitZ;
                    billboard.Type = BillboardType.NormalOriented;
                    billboard.SourceRectangle = Billboard.DefaultSourceRectangle;

                    vegetations.Add(billboard);
                }
            }
        }

        private void InitializeVegetation()
        {

        }

        private void DrawVegetation(GameTime gameTime)
        {
            foreach (Billboard billboard in vegetations)
            {
                if (grassViewDistanceSquared >=
                    Vector3.DistanceSquared(billboard.Position, game.Eye))
                {
                    game.Billboard.Draw(billboard);
                }
            }
        }

        #endregion

        #region Water

        /// <summary>
        /// Gets or sets the fog texture used to draw the landscape
        /// </summary>
        public Texture2D FogTexture { get; set; }

        /// <summary>
        /// The water is part of a spherical surface to make it look vast.
        /// But during rendering, e.g., for computing reflection and refraction,
        /// the water is treated as a flat plane with the height of zero.
        /// This value determines the shape of the surface
        /// </summary>
        private float earthRadius;

        /// <summary>
        /// A static texture applied to the water surface
        /// </summary>
        private Texture waterTexture;

        /// <summary>
        /// This texture is used as a bump map to simulate water
        /// </summary>
        private Texture waterDstortion;

        /// <summary>
        /// Render target used to draw the reflection & refraction texture
        /// </summary>
        private RenderTarget2D reflectionRenderTarget;

        /// <summary>
        /// Depth stencil buffer used when drawing reflection and refraction
        /// </summary>
        private DepthStencilBuffer waterDepthStencil;

        /// <summary>
        /// This texture is generated each frame for water reflection color sampling
        /// </summary>
        private Texture2D waterReflection;

        /// <summary>
        /// Water mesh
        /// </summary>
        private int waterVertexCount;
        private int waterPrimitiveCount;
        private VertexBuffer waterVertices;
        private IndexBuffer waterIndices;
        private VertexDeclaration waterVertexDeclaration;

        public Effect WaterEffect { get; set; }

        private void ReadWaterContent(ContentReader input)
        {
            earthRadius = input.ReadSingle();
            waterTexture = input.ReadExternalReference<Texture>();
            waterDstortion = input.ReadExternalReference<Texture>();
            WaterEffect = input.ContentManager.Load<Effect>("Effects/Water");
        }

        private void InitializeWater()
        {
            // Reflection & Refraction textures
            reflectionRenderTarget = new RenderTarget2D(
                graphics, 1024, 1024, 0, SurfaceFormat.Color, RenderTargetUsage.DiscardContents);

            waterDepthStencil = new DepthStencilBuffer(
                graphics, 1024, 1024, graphics.DepthStencilBuffer.Format);

            // Initialize water mesh
            waterVertexDeclaration = new VertexDeclaration(
                graphics, VertexPositionTexture.VertexElements);

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
            float len = 0;
            Vector2 pos;
            var center = new Vector2(Size.X / 2, Size.Y / 2);

            for (var y = 0; y <= CellCount; y++)
            {
                for (var x = 0; x <= CellCount; x++)
                {
                    pos.X = (Size.X - waterSize) / 2 + cellSize * x;
                    pos.Y = (Size.Y - waterSize) / 2 + cellSize * y;

                    len = Vector2.Subtract(pos, center).Length();

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
            if (!game.Settings.RealisticWater)
            {
                return;
            }

            DepthStencilBuffer prevDepth = graphics.DepthStencilBuffer;
            graphics.DepthStencilBuffer = waterDepthStencil;
            graphics.SetRenderTarget(0, reflectionRenderTarget);

            graphics.Clear(Color.Black);

            // Create a reflection view matrix
            var viewReflect = Matrix.Multiply(
                Matrix.CreateReflection(new Plane(Vector3.UnitZ, 0)), game.View);

            DrawSky(gameTime, viewReflect, game.Projection);

            if (game.Settings.ShowLandscape)
            {
                DrawTerrain(viewReflect, game.Projection, true);
            }

            // Draw other reflections
            if (game.Settings.ReflectionEnabled)
            {
                DrawWaterReflection(gameTime, viewReflect, game.Projection);
            }

            // Present the model manager to draw those models
            game.ModelManager.Present(gameTime, viewReflect, game.Projection);

            // Draw refraction onto the reflection texture
            if (game.Settings.ShowLandscape)
            {
                DrawTerrain(game.View, game.Projection, false);
            }

            graphics.SetRenderTarget(0, null);
            graphics.DepthStencilBuffer = prevDepth;

            // Retrieve refraction texture
            waterReflection = reflectionRenderTarget.GetTexture();

            graphics.RenderState.AlphaBlendEnable = false;
        }

        public void DrawWater(GameTime gameTime)
        {
            // Draw water mesh
            graphics.Indices = waterIndices;
            graphics.VertexDeclaration = waterVertexDeclaration;
            graphics.Vertices[0].SetSource(waterVertices, 0, VertexPositionTexture.SizeInBytes);

            if (FogTexture != null)
            {
                WaterEffect.Parameters["FogTexture"].SetValue(FogTexture);
            }

            if (game.Settings.RealisticWater)
            {
                WaterEffect.CurrentTechnique = WaterEffect.Techniques["Realisic"];
                WaterEffect.Parameters["ReflectionTexture"].SetValue(waterReflection);
            }
            else
            {
                WaterEffect.CurrentTechnique = WaterEffect.Techniques["Default"];
                WaterEffect.Parameters["ColorTexture"].SetValue(waterTexture);
            }

            WaterEffect.Parameters["DistortionTexture"].SetValue(waterDstortion);
            WaterEffect.Parameters["ViewInverse"].SetValue(game.ViewInverse);
            WaterEffect.Parameters["WorldViewProj"].SetValue(game.ViewProjection);
            WaterEffect.Parameters["WorldView"].SetValue(game.View);
            WaterEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.01f));

            WaterEffect.Begin();
            foreach (EffectPass pass in WaterEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                graphics.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, waterVertexCount, 0, waterPrimitiveCount);
                pass.End();
            }
            WaterEffect.End();

            graphics.RenderState.DepthBufferWriteEnable = true;
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

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        private void DisposeWater()
        {
            if (waterVertices != null)
            {
                waterVertices.Dispose();
            }

            if (waterIndices != null)
            {
                waterIndices.Dispose();
            }
        }

        #endregion
    }
    #endregion

    #region FogMask
    /// <summary>
    /// Game fog of war
    /// </summary>
    public class FogMask
    {
        #region Field
        private const int Size = 128;

        /// <summary>
        /// Gets the default glow texture for each unit
        /// </summary>
        public static Texture2D Glow
        {
            get
            {
                if (glow == null || glow.IsDisposed)
                {
                    glow = BaseGame.Singleton.ZipContent.Load<Texture2D>("Textures/Glow");
                }

                return glow;
            }
        }

        private static Texture2D glow;

        /// <summary>
        /// Gets the width of the mask
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height of the mask
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Objects are invisible when the intensity is below this value
        /// </summary>
        public const float VisibleIntensity = 0.5f;

        /// <summary>
        /// Common stuff
        /// </summary>
        private readonly GraphicsDevice graphics;
        private readonly SpriteBatch sprite;
        private Rectangle textureRectangle;

        /// <summary>
        /// Gets the result mask texture (Fog of war)
        /// </summary>
        public Texture2D Mask { get; private set; }

        public Texture2D Discovered { get; private set; }

        public Texture2D Current => current;

        private Texture2D current;
        private readonly RenderTarget2D discoveredCanvas;
        private readonly RenderTarget2D currentCanvas;
        private readonly RenderTarget2D maskCanvas;
        private readonly DepthStencilBuffer depthBuffer;

        /// <summary>
        /// Fog intensities
        /// </summary>
        private readonly bool[] visibility;

        /// <summary>
        /// Visible areas
        /// </summary>
        private struct Entry
        {
            public float Radius;
            public Vector2 Position;
        }

        private readonly List<Entry> visibleAreas = new();
        #endregion

        #region Method
        /// <summary>
        /// Creates a new fog of war mask
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
            depthBuffer = new DepthStencilBuffer(graphics, Size, Size, graphics.DepthStencilBuffer.Format);
            discoveredCanvas = new RenderTarget2D(graphics, Size, Size, 0, SurfaceFormat.Color);
            currentCanvas = new RenderTarget2D(graphics, Size, Size, 0, SurfaceFormat.Color);
            maskCanvas = new RenderTarget2D(graphics, Size, Size, 0, SurfaceFormat.Color, RenderTargetUsage.PreserveContents);
        }

        /// <summary>
        /// Gets the whether the specified point is in the fog of war
        /// </summary>
        public bool Contains(float x, float y)
        {
            return x <= 0 || y <= 0 || x >= Width || y >= Height || !visibility[Size * (int)(Size * y / Height) + (int)(Size * x / Width)];
        }

        /// <summary>
        /// Call this each frame to mark an area as visible
        /// </summary>
        /// <remarks>
        /// TODO: Custom glow texture
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
        /// Call this to refresh the fog of war texture
        /// </summary>
        public void Refresh(GameTime gameTime)
        {
            if (Mask != null && visibleAreas.Count <= 0)
            {
                return;
            }

            DepthStencilBuffer prevDepth = graphics.DepthStencilBuffer;

            graphics.DepthStencilBuffer = depthBuffer;

            // Draw current glows
            graphics.SetRenderTarget(0, currentCanvas);
            graphics.Clear(Color.Black);

            // Draw glows
            sprite.Begin(SpriteBlendMode.Additive);

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
            graphics.SetRenderTarget(0, discoveredCanvas);
            graphics.Clear(Color.Black);

            // Retrieve current glow texture
            current = currentCanvas.GetTexture();

            sprite.Begin(SpriteBlendMode.Additive);
            if (Discovered != null)
            {
                sprite.Draw(Discovered, textureRectangle, Color.White);
            }

            sprite.Draw(current, textureRectangle, Color.White);
            sprite.End();

            // Draw final mask texture
            graphics.SetRenderTarget(0, maskCanvas);
            graphics.Clear(Color.Black);

            // Retrieve discovered area
            Discovered = discoveredCanvas.GetTexture();

            sprite.Begin(SpriteBlendMode.Additive);
            sprite.Draw(Discovered, textureRectangle, new Color(128, 128, 128, 128));
            sprite.Draw(current, textureRectangle, Color.White);
            sprite.End();

            // Restore states
            graphics.SetRenderTarget(0, null);
            graphics.DepthStencilBuffer = prevDepth;

            // Retrieve final mask texture
            Mask = maskCanvas.GetTexture();

            // Update intensity map
            //mask.GetData<float>(intensity);

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
        #endregion
    }
    #endregion
}
