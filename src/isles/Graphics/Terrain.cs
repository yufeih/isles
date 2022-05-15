// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Graphics;

public class Terrain
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Heightmap _heightmap;
    private readonly Effect _terrainEffect;

    private readonly MeshDrawable _mesh;

    public Vector2 Size => _heightmap.Size;

    public Texture2D FogTexture { get; set; }

    private readonly List<(Texture2D color, Texture2D alpha)> _layers = new();

    public Terrain(GraphicsDevice graphicsDevice, TerrainData data, Heightmap heightmap, ShaderLoader shaderLoader, TextureLoader textureLoader)
    {
        _graphicsDevice = graphicsDevice;
        _heightmap = heightmap;

        foreach (var layer in data.Layers)
        {
            _layers.Add((textureLoader.LoadTexture(layer.ColorTexture), textureLoader.LoadTexture(layer.AlphaTexture)));
        }

        // Load terrain effect
        _terrainEffect = shaderLoader.LoadShader("shaders/Terrain.cso");

        _mesh = MeshHelper.CreateHeightmap<VertexPositionTexture2>(heightmap, 32, SetTerrainVertex).ToDrawable(graphicsDevice);

        void SetTerrainVertex(int x, int y, ref VertexPositionTexture2 vertex)
        {
            vertex.TextureCoordinate1 = new(1.0f * x / (heightmap.Width - 1), 1.0f * y / (heightmap.Height - 1));
        }

        surfaceEffect = shaderLoader.LoadShader("shaders/Surface.cso");
        surfaceVertexBuffer = new DynamicVertexBuffer(_graphicsDevice,
            typeof(VertexPositionColorTexture), MaxSurfaceVertices, BufferUsage.WriteOnly);
        surfaceIndexBuffer = new DynamicIndexBuffer(_graphicsDevice,
            typeof(ushort), MaxSurfaceIndices, BufferUsage.WriteOnly);
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

        _terrainEffect.CurrentTechnique = technique;
        _terrainEffect.CurrentTechnique.Passes[0].Apply();

        foreach (var (colorTexture, alphaTexture) in _layers)
        {
            _terrainEffect.Parameters["ColorTexture"].SetValue(colorTexture);
            _terrainEffect.Parameters["AlphaTexture"].SetValue(alphaTexture);
            _terrainEffect.CurrentTechnique.Passes[0].Apply();

            _mesh.DrawPrimitives();
        }
    }

    private void DrawTerrainShadow(ShadowEffect shadowEffect)
    {
        _graphicsDevice.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default, RasterizerState.CullNone);

        _terrainEffect.Parameters["ShadowMap"].SetValue(shadowEffect.ShadowMap);
        _terrainEffect.Parameters["LightViewProjection"].SetValue(shadowEffect.LightViewProjection);
        _terrainEffect.CurrentTechnique = _terrainEffect.Techniques["ShadowMapping"];

        _terrainEffect.CurrentTechnique.Passes[0].Apply();

        _mesh.DrawPrimitives();
    }
}
