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
    }

    public void DrawTerrain(Matrix viewProjection, bool upper)
    {
        var technique = upper ? _terrainEffect.Techniques["FastUpper"] : _terrainEffect.Techniques["FastLower"];

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
