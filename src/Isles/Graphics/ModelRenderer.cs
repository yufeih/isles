// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class ModelRenderer
    {
        private readonly GraphicsDevice _graphics;

        private readonly Effect _effect;

        private readonly EffectParameter _viewProjection;
        private readonly EffectParameter _world;
        private readonly EffectParameter _bones;
        private readonly EffectParameter _diffuse;
        private readonly EffectParameter _light;
        private readonly EffectParameter _texture;

        private readonly EffectTechnique _defaultShader;
        private readonly EffectTechnique _defaultSkinnedShader;
        private readonly EffectTechnique _shadowShader;
        private readonly EffectTechnique _shadowSkinnedShader;
        private readonly EffectTechnique _pickShader;
        private readonly EffectTechnique _pickSkinnedShader;

        private readonly List<DrawableItem> _defaultOpaque = new();
        private readonly List<DrawableItem> _defaultTransparent = new();
        private readonly List<DrawableItem> _skinnedOpaque = new();
        private readonly List<DrawableItem> _skinnedTransparent = new();

        public ModelRenderer(GraphicsDevice graphics, ShaderLoader shaderLoader)
        {
            _graphics = graphics;

            _effect = shaderLoader.LoadShader("data/shaders/Model.fx");
            _viewProjection = _effect.Parameters["ViewProjection"];
            _texture = _effect.Parameters["BasicTexture"];
            _world = _effect.Parameters["World"];
            _bones = _effect.Parameters["Bones"];
            _diffuse = _effect.Parameters["Diffuse"];
            _light = _effect.Parameters["LightColor"];

            _defaultShader = _effect.Techniques["Default"];
            _defaultSkinnedShader = _effect.Techniques["DefaultSkinned"];
            _shadowShader = _effect.Techniques["ShadowMapping"];
            _shadowSkinnedShader = _effect.Techniques["ShadowMappingSkinned"];
            _pickShader = _effect.Techniques["Pick"];
            _pickSkinnedShader = _effect.Techniques["PickSkinned"];
        }

        public void Clear()
        {
            _defaultOpaque.Clear();
            _skinnedOpaque.Clear();
            _defaultTransparent.Clear();
            _skinnedTransparent.Clear();
        }

        public void AddDrawable(Model.Mesh mesh, Matrix transform, Matrix[] boneTransforms, Vector4 tint, Vector4 glow)
        {
            var queue = tint.W < 1
                ? (boneTransforms != null ? _skinnedTransparent : _defaultTransparent)
                : (boneTransforms != null ? _skinnedOpaque : _defaultOpaque);

            queue.Add(new()
            {
                Mesh = mesh,
                Transform = transform,
                Bones = boneTransforms,
                Tint = tint,
                Glow = glow
            });
        }

        public void Draw(Matrix viewProjection, bool showOpaque = true, bool showTransparent = true)
        {
            _viewProjection.SetValue(viewProjection);

            if (showOpaque)
            {
                _graphics.SetRenderState(BlendState.Opaque, DepthStencilState.Default, RasterizerState.CullNone);

                Draw(_defaultShader, _defaultOpaque);
                Draw(_defaultSkinnedShader, _skinnedOpaque);
            }

            if (showTransparent)
            {
                _graphics.SetRenderState(BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullNone);

                Draw(_defaultShader, _defaultTransparent);
                Draw(_defaultSkinnedShader, _skinnedTransparent);
            }
        }

        public void DrawShadowMap(ShadowEffect shadow)
        {
            _viewProjection.SetValue(shadow.LightViewProjection);

            _graphics.SetRenderState(BlendState.Opaque, DepthStencilState.Default, RasterizerState.CullNone);

            Draw(_shadowShader, _defaultOpaque);
            Draw(_shadowSkinnedShader, _skinnedOpaque);
            Draw(_shadowShader, _defaultTransparent);
            Draw(_shadowSkinnedShader, _skinnedTransparent);
        }

        public void DrawObjectMap(Matrix viewProjection)
        {
            _viewProjection.SetValue(viewProjection);

            _graphics.SetRenderState(BlendState.Opaque, DepthStencilState.Default, RasterizerState.CullNone);

            Draw(_pickShader, _defaultOpaque);
            Draw(_pickSkinnedShader, _skinnedOpaque);
            Draw(_pickShader, _defaultTransparent);
            Draw(_pickSkinnedShader, _skinnedTransparent);
        }

        private void Draw(EffectTechnique shader, List<DrawableItem> items)
        {
            _effect.CurrentTechnique = shader;
            _effect.CurrentTechnique.Passes[0].Apply();

            foreach (var item in items)
            {
                _world?.SetValue(item.Transform);
                _diffuse?.SetValue(item.Tint);
                _light?.SetValue(item.Glow);

                if (item.Bones != null)
                {
                    _bones?.SetValue(item.Bones);
                }

                foreach (var part in item.Mesh.Primitives)
                {
                    _graphics.SetVertexBuffer(part.VertexBuffer);
                    _graphics.Indices = part.IndexBuffer;

                    _texture?.SetValue(part.Texture);
                    _effect.CurrentTechnique.Passes[0].Apply();

                    _graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, part.NumVertices, 0, part.PrimitiveCount);
                }
            }
        }

        private struct DrawableItem
        {
            public Model.Mesh Mesh;
            public Matrix Transform;
            public Matrix[] Bones;
            public Vector4 Tint;
            public Vector4 Glow;
        }
    }
}