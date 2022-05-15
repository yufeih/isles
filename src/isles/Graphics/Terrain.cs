// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace Isles.Graphics;

public class Terrain
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ModelRenderer _modelRenderer;
    private readonly Heightmap _heightmap;
    private readonly Effect _terrainEffect;

    private readonly int _vertexCount;
    private readonly int _primitiveCount;
    private readonly IndexBuffer _indexBuffer;
    private readonly VertexBuffer _vertexBuffer;

    private Texture waterTexture;
    private Texture waterDstortion;
    private RenderTarget2D reflectionRenderTarget;
    private Texture2D waterReflection;
    private int waterVertexCount;
    private int waterPrimitiveCount;
    private VertexBuffer waterVertices;
    private IndexBuffer waterIndices;

    private readonly Effect _waterEffect;

    public Vector2 Size => _heightmap.Size;

    public Texture2D FogTexture { get; set; }

    private readonly List<(Texture2D color, Texture2D alpha)> _layers = new();

    public Terrain(GraphicsDevice graphicsDevice, TerrainData data, Heightmap heightnmap, ModelRenderer modelRenderer, ShaderLoader shaderLoader, TextureLoader textureLoader)
    {
        _graphicsDevice = graphicsDevice;
        _heightmap = heightnmap;
        _modelRenderer = modelRenderer;

        foreach (var layer in data.Layers)
        {
            _layers.Add((textureLoader.LoadTexture(layer.ColorTexture), textureLoader.LoadTexture(layer.AlphaTexture)));
        }

        waterTexture = textureLoader.LoadTexture(data.WaterTexture);
        waterDstortion = textureLoader.LoadTexture(data.WaterBumpTexture);

        // Load terrain effect
        _terrainEffect = shaderLoader.LoadShader("shaders/Terrain.cso");

        _vertexCount = _heightmap.Width * _heightmap.Height;
        _primitiveCount = (_heightmap.Width - 1) * (_heightmap.Height - 1) * 2;
        _vertexBuffer = new VertexBuffer(graphicsDevice, TerrainVertex.VertexDeclaration, _vertexCount, BufferUsage.WriteOnly);
        _indexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort), _primitiveCount * 3, BufferUsage.WriteOnly);

        var vertices = ArrayPool<TerrainVertex>.Shared.Rent(_vertexCount);
        var indices = ArrayPool<ushort>.Shared.Rent(_primitiveCount * 3);

        Triangulate(_heightmap, vertices, indices);

        _indexBuffer.SetData(indices, 0, _primitiveCount * 3);
        _vertexBuffer.SetData(vertices, 0, _vertexCount);

        ArrayPool<TerrainVertex>.Shared.Return(vertices);
        ArrayPool<ushort>.Shared.Return(indices);

        _waterEffect = shaderLoader.LoadShader("shaders/Water.cso");
        InitializeWater();

        surfaceEffect = shaderLoader.LoadShader("shaders/Surface.cso");
        surfaceVertexBuffer = new DynamicVertexBuffer(_graphicsDevice,
            typeof(VertexPositionColorTexture), MaxSurfaceVertices, BufferUsage.WriteOnly);
        surfaceIndexBuffer = new DynamicIndexBuffer(_graphicsDevice,
            typeof(ushort), MaxSurfaceIndices, BufferUsage.WriteOnly);
    }

    private static void Triangulate(Heightmap heightmap, TerrainVertex[] vertices, ushort[] indices)
    {
        var (w, h) = (heightmap.Width, heightmap.Height);

        for (var i = 0; i < heightmap.Heights.Length; i++)
        {
            var y = Math.DivRem(i, w, out var x);
            var z = (float)heightmap.Heights[i];

            vertices[i].Position = new(x * heightmap.Step, y * heightmap.Step, z);
            vertices[i].TextureCoordinate0 = new(1.0f * x / (w - 1) * heightmap.Step, 1.0f * y / (h - 1) * heightmap.Step);
            vertices[i].TextureCoordinate1 = new(1.0f * x / (w - 1), 1.0f * y / (h - 1));
        }

        var index = 0;
        for (var y = 0; y < h - 1; y++)
        {
            for (var x = 0; x < w - 1; x++)
            {
                var v0 = (ushort)(x + y * w);
                var v1 = (ushort)(v0 + 1);
                var v2 = (ushort)(x + (y + 1) * w);
                var v3 = (ushort)(v2 + 1);

                indices[index++] = v0;
                indices[index++] = v1;
                indices[index++] = v3;

                indices[index++] = v0;
                indices[index++] = v3;
                indices[index++] = v2;
            }
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
        surface.Position.Z = _heightmap.GetHeight(position.X, position.Y) + 6;

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

        _graphicsDevice.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default);

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

        _graphicsDevice.SetRenderState(depthStencilState: DepthStencilState.DepthRead);

        _graphicsDevice.Indices = surfaceIndexBuffer;
        _graphicsDevice.SetVertexBuffer(surfaceVertexBuffer);
        _graphicsDevice.DrawIndexedPrimitives(
            PrimitiveType.TriangleList, 0, 0, surfaceVertices.Count, 0, surfaceIndices.Count / 3);
    }

    private void InitializeWater()
    {
        // Reflection & Refraction textures
        reflectionRenderTarget = new RenderTarget2D(
            _graphicsDevice, 1024, 1024, true, SurfaceFormat.Color, _graphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.DiscardContents);

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
            _graphicsDevice, typeof(VertexPositionTexture), waterVertexCount, BufferUsage.WriteOnly);

        waterVertices.SetData(vertexData);

        waterIndices = new IndexBuffer(
            _graphicsDevice, typeof(short), waterPrimitiveCount * 3, BufferUsage.WriteOnly);

        waterIndices.SetData(indexData);
    }

    public void UpdateWaterReflectionAndRefraction(in ViewMatrices matrices)
    {
        _graphicsDevice.PushRenderTarget(reflectionRenderTarget);

        _graphicsDevice.Clear(Color.Black);

        // Create a reflection view matrix
        var viewReflect = Matrix.Multiply(
            Matrix.CreateReflection(new Plane(Vector3.UnitZ, 0)), matrices.View);

        DrawTerrain(viewReflect * matrices.Projection, true);

        // Present the model manager to draw those models
        _modelRenderer.Draw(viewReflect * matrices.Projection);

        // Draw refraction onto the reflection texture
        DrawTerrain(matrices.ViewProjection, false);

        _graphicsDevice.PopRenderTarget();

        // Retrieve refraction texture
        waterReflection = reflectionRenderTarget;
    }

    public void DrawWater(GameTime gameTime, in ViewMatrices matrices)
    {
        _graphicsDevice.SetRenderState(BlendState.Opaque, DepthStencilState.DepthRead, RasterizerState.CullNone);

        // Draw water mesh
        _graphicsDevice.Indices = waterIndices;
        _graphicsDevice.SetVertexBuffer(waterVertices);

        if (FogTexture != null)
        {
            _waterEffect.Parameters["FogTexture"].SetValue(FogTexture);
        }

        _waterEffect.Parameters["ColorTexture"].SetValue(waterTexture);

        if (waterReflection != null)
        {
            _waterEffect.CurrentTechnique = _waterEffect.Techniques["Realisic"];
            _waterEffect.Parameters["ReflectionTexture"].SetValue(waterReflection);
        }
        else
        {
            _waterEffect.CurrentTechnique = _waterEffect.Techniques["Default"];
        }

        _waterEffect.Parameters["DistortionTexture"].SetValue(waterDstortion);
        _waterEffect.Parameters["ViewInverse"].SetValue(matrices.ViewInverse);
        _waterEffect.Parameters["WorldViewProj"].SetValue(matrices.ViewProjection);
        _waterEffect.Parameters["WorldView"].SetValue(matrices.View);
        _waterEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.01f));

        _waterEffect.CurrentTechnique.Passes[0].Apply();

        _graphicsDevice.DrawIndexedPrimitives(
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
            _terrainEffect.Techniques["FastUpper"] : _terrainEffect.Techniques["FastLower"];

        DrawTerrain(viewProjection, technique);
    }

    public void DrawTerrain(ShadowEffect shadowEffect, in ViewMatrices matrices)
    {
        DrawTerrain(matrices.ViewProjection, _terrainEffect.Techniques["Default"]);

        if (shadowEffect != null)
        {
            DrawTerrainShadow(shadowEffect);
        }
    }

    private void DrawTerrain(Matrix viewProjection, EffectTechnique technique)
    {
        _graphicsDevice.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default, RasterizerState.CullNone);

        // Set parameters
        if (FogTexture != null)
        {
            _terrainEffect.Parameters["FogTexture"].SetValue(FogTexture);
        }

        _terrainEffect.Parameters["WorldViewProjection"].SetValue(viewProjection);

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        _terrainEffect.CurrentTechnique = technique;
        _terrainEffect.CurrentTechnique.Passes[0].Apply();

        foreach (var (colorTexture, alphaTexture) in _layers)
        {
            _terrainEffect.Parameters["ColorTexture"].SetValue(colorTexture);
            _terrainEffect.Parameters["AlphaTexture"].SetValue(alphaTexture);
            _terrainEffect.CurrentTechnique.Passes[0].Apply();

            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _primitiveCount);
        }
    }

    private void DrawTerrainShadow(ShadowEffect shadowEffect)
    {
        _graphicsDevice.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default, RasterizerState.CullNone);

        _terrainEffect.Parameters["ShadowMap"].SetValue(shadowEffect.ShadowMap);
        _terrainEffect.Parameters["LightViewProjection"].SetValue(shadowEffect.LightViewProjection);
        _terrainEffect.CurrentTechnique = _terrainEffect.Techniques["ShadowMapping"];

        _terrainEffect.CurrentTechnique.Passes[0].Apply();

        _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexCount, 0, _primitiveCount);
    }

    struct TerrainVertex : IVertexType
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
