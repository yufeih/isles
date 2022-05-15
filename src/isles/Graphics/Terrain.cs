// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace Isles.Graphics;

public class Terrain : BaseLandscape
{
    private Effect terrainEffect;

    private int _vertexCount;
    private int _primitiveCount;
    private IndexBuffer _indexBuffer;
    private VertexBuffer _vertexBuffer;

    public Texture2D FogTexture { get; set; }
    private Texture waterTexture;
    private Texture waterDstortion;
    private RenderTarget2D reflectionRenderTarget;
    private Texture2D waterReflection;
    private int waterVertexCount;
    private int waterPrimitiveCount;
    private VertexBuffer waterVertices;
    private IndexBuffer waterIndices;

    public Effect WaterEffect { get; set; }

    public override void Load(TerrainData data, Heightmap heightmap, TextureLoader textureLoader)
    {
        base.Load(data, heightmap, textureLoader);

        waterTexture = textureLoader.LoadTexture(data.WaterTexture);
        waterDstortion = textureLoader.LoadTexture(data.WaterBumpTexture);

        Initialize(BaseGame.Singleton);
    }

    public override void Initialize(BaseGame game)
    {
        base.Initialize(game);

        // Load terrain effect
        terrainEffect = game.ShaderLoader.LoadShader("shaders/Terrain.cso");

        _vertexCount = Heightmap.Width * Heightmap.Height;
        _primitiveCount = (Heightmap.Width - 1) * (Heightmap.Height - 1) * 2;
        _vertexBuffer = new VertexBuffer(game.GraphicsDevice, TerrainVertex.VertexDeclaration, _vertexCount, BufferUsage.WriteOnly);
        _indexBuffer = new IndexBuffer(game.GraphicsDevice, typeof(ushort), _primitiveCount * 3, BufferUsage.WriteOnly);

        var vertices = ArrayPool<TerrainVertex>.Shared.Rent(_vertexCount);
        var indices = ArrayPool<ushort>.Shared.Rent(_primitiveCount * 3);

        Heightmap.Triangulate<TerrainVertex>(vertices, indices);
        FillTextureCoordinates(vertices);

        _indexBuffer.SetData(indices, 0, _primitiveCount * 3);
        _vertexBuffer.SetData(vertices, 0, _vertexCount);

        ArrayPool<TerrainVertex>.Shared.Return(vertices);
        ArrayPool<ushort>.Shared.Return(indices);

        WaterEffect = game.ShaderLoader.LoadShader("shaders/Water.cso");
        InitializeWater();

        surfaceEffect = game.ShaderLoader.LoadShader("shaders/Surface.cso");
        surfaceVertexBuffer = new DynamicVertexBuffer(game.GraphicsDevice,
            typeof(VertexPositionColorTexture), MaxSurfaceVertices, BufferUsage.WriteOnly);
        surfaceIndexBuffer = new DynamicIndexBuffer(game.GraphicsDevice,
            typeof(ushort), MaxSurfaceIndices, BufferUsage.WriteOnly);
    }

    private void FillTextureCoordinates(TerrainVertex[] vertices, int step = 32)
    {
        int w = Heightmap.Width, h = Heightmap.Height;
        for (var i = 0; i < vertices.Length; i++)
        {
            var y = Math.DivRem(i, w, out var x);

            // Texture0 is the tile texture, which only covers half patch
            vertices[i].TextureCoordinate0 = new(1.0f * x / (w - 1) / step, 1.0f * y / (h - 1) / step);

            // Texture1 is the visibility texture, which covers the entire terrain
            vertices[i].TextureCoordinate1 = new(1.0f * x / (w - 1), 1.0f * y / (h - 1));
        }
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
        surface.Position.Z = Heightmap.GetHeight(position.X, position.Y) + 6;

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

    public void PresentSurface(in ViewMatrices matrices)
    {
        if (texturedSurfaces.Count <= 0)
        {
            return;
        }

        game.GraphicsDevice.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default);

        surfaceEffect.Parameters["WorldViewProjection"].SetValue(matrices.ViewProjection);

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

    private void PresentSurface(LinkedListNode<TexturedSurface> start, LinkedListNode<TexturedSurface> end)
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

        game.GraphicsDevice.SetRenderState(depthStencilState: DepthStencilState.DepthRead);

        game.GraphicsDevice.Indices = surfaceIndexBuffer;
        game.GraphicsDevice.SetVertexBuffer(surfaceVertexBuffer);
        game.GraphicsDevice.DrawIndexedPrimitives(
            PrimitiveType.TriangleList, 0, 0, surfaceVertices.Count, 0, surfaceIndices.Count / 3);
    }

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
        var waterSize = Math.Max(Size.X, Size.Y) * 2;
        var cellSize = waterSize / CellCount;

        var i = 0;
        Vector2 pos;

        for (var y = 0; y <= CellCount; y++)
        {
            for (var x = 0; x <= CellCount; x++)
            {
                pos.X = (Size.X - waterSize) / 2 + cellSize * x;
                pos.Y = (Size.Y - waterSize) / 2 + cellSize * y;

                vertexData[i].Position.X = pos.X;
                vertexData[i].Position.Y = pos.Y;

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

    public void UpdateWaterReflectionAndRefraction(in ViewMatrices matrices)
    {
        graphics.PushRenderTarget(reflectionRenderTarget);

        graphics.Clear(Color.Black);

        // Create a reflection view matrix
        var viewReflect = Matrix.Multiply(
            Matrix.CreateReflection(new Plane(Vector3.UnitZ, 0)), matrices.View);

        DrawTerrain(viewReflect * matrices.Projection, true);

        // Present the model manager to draw those models
        game.ModelRenderer.Draw(viewReflect * matrices.Projection);

        // Draw refraction onto the reflection texture
        DrawTerrain(matrices.ViewProjection, false);

        graphics.PopRenderTarget();

        // Retrieve refraction texture
        waterReflection = reflectionRenderTarget;
    }

    public void DrawWater(GameTime gameTime, in ViewMatrices matrices)
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
        WaterEffect.Parameters["ViewInverse"].SetValue(matrices.ViewInverse);
        WaterEffect.Parameters["WorldViewProj"].SetValue(matrices.ViewProjection);
        WaterEffect.Parameters["WorldView"].SetValue(matrices.View);
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

    public void DrawTerrain(Matrix viewProjection, bool upper)
    {
        EffectTechnique technique = upper ?
            terrainEffect.Techniques["FastUpper"] : terrainEffect.Techniques["FastLower"];

        DrawTerrain(viewProjection, technique);
    }

    public void DrawTerrain(ShadowEffect shadowEffect, in ViewMatrices matrices)
    {
        DrawTerrain(matrices.ViewProjection, terrainEffect.Techniques["Default"]);

        if (shadowEffect != null)
        {
            DrawTerrainShadow(shadowEffect);
        }
    }

    private void DrawTerrain(Matrix viewProjection, EffectTechnique technique)
    {
        graphics.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default, RasterizerState.CullNone);

        // Set parameters
        if (FogTexture != null)
        {
            terrainEffect.Parameters["FogTexture"].SetValue(FogTexture);
        }

        terrainEffect.Parameters["WorldViewProjection"].SetValue(viewProjection);

        game.GraphicsDevice.SetVertexBuffer(_vertexBuffer);
        game.GraphicsDevice.Indices = _indexBuffer;

        terrainEffect.CurrentTechnique = technique;
        terrainEffect.CurrentTechnique.Passes[0].Apply();

        foreach (Layer layer in Layers)
        {
            terrainEffect.Parameters["ColorTexture"].SetValue(layer.ColorTexture);
            terrainEffect.Parameters["AlphaTexture"].SetValue(layer.AlphaTexture);
            terrainEffect.CurrentTechnique.Passes[0].Apply();

            game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _primitiveCount);
        }
    }

    private void DrawTerrainShadow(ShadowEffect shadowEffect)
    {
        graphics.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default, RasterizerState.CullNone);

        terrainEffect.Parameters["ShadowMap"].SetValue(shadowEffect.ShadowMap);
        terrainEffect.Parameters["LightViewProjection"].SetValue(shadowEffect.LightViewProjection);
        terrainEffect.CurrentTechnique = terrainEffect.Techniques["ShadowMapping"];

        terrainEffect.CurrentTechnique.Passes[0].Apply();

        game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _primitiveCount);
    }

    public struct TerrainVertex : IVertexType, IVertexPosition
    {
        public Vector3 Position { get; set; }
        public Vector2 TextureCoordinate0 { get; set; }
        public Vector2 TextureCoordinate1 { get; set; }

        public static readonly VertexDeclaration VertexDeclaration = new(new VertexElement[]
        {
                new(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
        });

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
