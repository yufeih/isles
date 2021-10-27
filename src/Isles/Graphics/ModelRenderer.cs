// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class ModelRenderer
    {
        private readonly GraphicsDevice _graphics;

        private readonly Effect _effect;

        private readonly EffectParameter _view;
        private readonly EffectParameter _projection;
        private readonly EffectParameter _viewProjection;
        private readonly EffectParameter _viewInverse;
        private readonly EffectParameter _world;
        private readonly EffectParameter _bones;
        private readonly EffectParameter _diffuse;
        private readonly EffectParameter _light;
        private readonly EffectParameter _texture;

        private readonly EffectTechnique _defaultShader;
        private readonly EffectTechnique _defaultSkinnedShader;
        private readonly EffectTechnique _shadowShader;
        private readonly EffectTechnique _shadowSkinnedShader;

        private readonly List<DrawItem> _defaultOpaque = new();
        private readonly List<DrawItem> _defaultTransparent = new();
        private readonly List<DrawItem> _skinnedOpaque = new();
        private readonly List<DrawItem> _skinnedTransparent = new();

        public ModelRenderer(GraphicsDevice graphics, ContentManager content)
        {
            _graphics = graphics;

            _effect = content.Load<Effect>("Effects/Model");
            _view = _effect.Parameters["View"];
            _viewInverse = _effect.Parameters["ViewInverse"];
            _projection = _effect.Parameters["Projection"];
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
        }

        public void Draw(ModelMesh mesh, Matrix transform, Matrix[] skinTransforms, Vector4 tint, Vector4 glow)
        {
            var queue = tint.W < 1
                ? (skinTransforms != null ? _skinnedTransparent : _defaultTransparent)
                : (skinTransforms != null ? _skinnedOpaque : _defaultOpaque);

            queue.Add(new()
            {
                Mesh = mesh,
                Transform = transform,
                SkinTransforms = skinTransforms,
                Tint = tint,
                Glow = glow
            });
        }

        public void Present(Matrix view, Matrix projection, ShadowEffect shadow = null, bool showOpaque = true, bool showTransparent = true)
        {
            _view.SetValue(view);
            _projection.SetValue(projection);
            _viewInverse.SetValue(Matrix.Invert(view));
            _viewProjection.SetValue(view * projection);

            if (shadow != null)
            {
                _effect.Parameters["LightViewProjection"].SetValue(shadow.LightViewProjection);
            }

            if (showOpaque)
            {
                _graphics.SetRenderState(BlendState.Opaque, DepthStencilState.Default, RasterizerState.CullNone);

                Draw(shadow is null ? _defaultShader : _shadowShader, _defaultOpaque);
                Draw(shadow is null ? _defaultSkinnedShader : _shadowSkinnedShader, _skinnedOpaque);

                _defaultOpaque.Clear();
                _skinnedOpaque.Clear();
            }

            if (showTransparent)
            {
                _graphics.SetRenderState(BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullNone);

                Draw(shadow is null ? _defaultShader : _shadowShader, _defaultTransparent);
                Draw(shadow is null ? _defaultSkinnedShader : _shadowSkinnedShader, _skinnedTransparent);

                _defaultTransparent.Clear();
                _skinnedTransparent.Clear();
            }
        }

        private void Draw(EffectTechnique shader, List<DrawItem> items)
        {
            _effect.CurrentTechnique = shader;
            _effect.Begin();
            _effect.CurrentTechnique.Passes[0].Begin();

            foreach (var item in items)
            {
                _world?.SetValue(item.Transform);
                _diffuse?.SetValue(item.Tint);
                _light?.SetValue(item.Glow);

                if (item.SkinTransforms != null)
                {
                    _bones?.SetValue(item.SkinTransforms);
                }

                foreach (var part in item.Mesh.MeshParts)
                {
                    _graphics.VertexDeclaration = part.VertexDeclaration;
                    _graphics.Vertices[0].SetSource(item.Mesh.VertexBuffer, part.StreamOffset, part.VertexStride);
                    _graphics.Indices = item.Mesh.IndexBuffer;

                    _texture?.SetValue(((BasicEffect)part.Effect).Texture);
                    _effect.CommitChanges();

                    _graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.BaseVertex, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
                }
            }

            _effect.CurrentTechnique.Passes[0].End();
            _effect.End();
        }

        private struct DrawItem
        {
            public ModelMesh Mesh;
            public Matrix Transform;
            public Matrix[] SkinTransforms;
            public Vector4 Tint;
            public Vector4 Glow;
        }
    }
}