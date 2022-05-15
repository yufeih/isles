// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Graphics;

public class Water
{
    private readonly GraphicsDevice _graphicsDevice;

    private readonly Matrix _world;
    private readonly MeshDrawable _mesh;
    private readonly Effect _waterEffect;

    private Texture _texture;
    private Texture _distortionTexture;
    
    public Water(GraphicsDevice graphicsDevice, TerrainData data, ShaderLoader shaderLoader, TextureLoader textureLoader)
    {
        _graphicsDevice = graphicsDevice;

        _texture = textureLoader.LoadTexture(data.WaterTexture);
        _distortionTexture = textureLoader.LoadTexture(data.WaterBumpTexture);

        _waterEffect = shaderLoader.LoadShader("shaders/Water.cso");

        var size = data.Step * 1024;
        _mesh = MeshHelper.CreateGrid<VertexPositionTexture>(1, 1, size).ToDrawable(graphicsDevice);
        _world = Matrix.CreateTranslation(-size / 2, -size / 2, 0);
    }

    public void Draw(GameTime gameTime, in ViewMatrices matrices, Texture2D fogTexture)
    {
        _graphicsDevice.SetRenderState(BlendState.Opaque, DepthStencilState.DepthRead, RasterizerState.CullNone);

        if (fogTexture != null)
        {
            _waterEffect.Parameters["FogTexture"].SetValue(fogTexture);
        }

        _waterEffect.Parameters["ColorTexture"].SetValue(_texture);
        _waterEffect.CurrentTechnique = _waterEffect.Techniques["Default"];

        _waterEffect.Parameters["DistortionTexture"].SetValue(_distortionTexture);
        _waterEffect.Parameters["ViewInverse"].SetValue(matrices.ViewInverse);
        _waterEffect.Parameters["WorldViewProj"].SetValue(_world * matrices.ViewProjection);
        _waterEffect.Parameters["WorldView"].SetValue(_world * matrices.View);
        _waterEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.01f));

        _waterEffect.CurrentTechnique.Passes[0].Apply();

        _mesh.DrawPrimitives();
    }

    private static Vector2 MoveInCircle(GameTime gameTime, float speed)
    {
        var time = gameTime.TotalGameTime.TotalSeconds * speed;

        var x = (float)Math.Cos(time);
        var y = (float)Math.Sin(time);

        return new Vector2(x, y);
    }
}
