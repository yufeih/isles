// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    /// <summary>
    /// Material for every game model.
    /// </summary>
    /// <remarks>
    /// You may wonder where is diffuse color. Since diffuse color
    /// differs too much between game models, we considered it a
    /// special property for every game model.
    /// </remarks>
    public class Material
    {
        /// <summary>
        /// Gets or sets whether the material has changed.
        /// </summary>
        public bool IsDirty { get; set; } = true;

        /// <summary>
        /// The effect technique for the material.
        /// </summary>
        public EffectTechnique Technique
        {
            get => technique;
            set
            {
                technique = value;
                IsDirty = true;
            }
        }

        private EffectTechnique technique;

        /// <summary>
        /// Base model texture.
        /// </summary>
        public Texture2D Texture
        {
            get => texture;
            set
            {
                texture = value;
                IsDirty = true;
            }
        }

        private Texture2D texture;

        /// <summary>
        /// Model normal texture.
        /// </summary>
        public Texture2D NormalTexture
        {
            get => normalTexture;
            set
            {
                normalTexture = value;
                IsDirty = true;
            }
        }

        private Texture2D normalTexture;

        /// <summary>
        /// Ambient color.
        /// </summary>
        public Vector4 AmbientColor
        {
            get => ambientColor;
            set
            {
                ambientColor = value;
                IsDirty = true;
            }
        }

        private Vector4 ambientColor = new(0.2f, 0.2f, 0.25f, 1.0f);

        /// <summary>
        /// Diffuse color.
        /// </summary>
        public Vector4 EmissiveColor
        {
            get => emissiveColor;
            set
            {
                emissiveColor = value;
                IsDirty = true;
            }
        }

        private Vector4 emissiveColor = Vector4.One;

        /// <summary>
        /// Specular color.
        /// </summary>
        public Vector4 SpecularColor
        {
            get => specularColor;
            set
            {
                specularColor = value;
                IsDirty = true;
            }
        }

        private Vector4 specularColor = Vector4.One;

        /// <summary>
        /// Specular power.
        /// </summary>
        public float SpecularPower
        {
            get => specularPower;
            set
            {
                specularPower = value;
                IsDirty = true;
            }
        }

        private float specularPower = 12;

        public bool IsTransparent
        {
            get => isTransparent;
            set
            {
                isTransparent = value;
                IsDirty = true;
            }
        }

        private bool isTransparent;

        /// <summary>
        /// Creates a new material.
        /// </summary>
        public Material() { }

        /// <summary>
        /// Creates a new material.
        /// </summary>
        public Material(string techniqueName)
        {
            Technique = GetTechnique(techniqueName);
        }

        /// <summary>
        /// Creates a new material from basic effect.
        /// </summary>
        /// <param name="basicEffect"></param>
        public Material(BasicEffect basicEffect)
        {
            if (basicEffect != null)
            {
                IsTransparent = false;
                AmbientColor = new Vector4(basicEffect.AmbientLightColor, 1);
                EmissiveColor = new Vector4(basicEffect.EmissiveColor, 1);
                SpecularColor = new Vector4(basicEffect.SpecularColor, 1);
                SpecularPower = basicEffect.SpecularPower;
                Texture = basicEffect.Texture;
                NormalTexture = null;
            }
        }

        /// <summary>
        /// An empty material.
        /// </summary>
        public static Material Default = new();

        /// <summary>
        /// The shadow map material.
        /// </summary>
        public static Material ShadowMap = new("ShadowMapping");
        public static Material ShadowMapSkinned = new("ShadowMappingSkinned");

        /// <summary>
        /// Gets the effect technique with the speicific name.
        /// </summary>
        public static EffectTechnique GetTechnique(string techniqueName)
        {
            return ModelManager._effect == null ? throw new InvalidOperationException() : ModelManager._effect.Techniques[techniqueName];
        }
    }

    public class ModelManager
    {
        public class Renderable
        {
            private readonly BaseGame game = BaseGame.Singleton;
            private readonly Effect effect = ModelManager._effect;
            private readonly EffectParameter world;
            private readonly EffectParameter bones;
            private readonly EffectParameter diffuse;
            private readonly EffectParameter light;
            private readonly EffectParameter worldInverse;
            private readonly EffectParameter worldInverseTranspose;
            private readonly bool isTransparent;

            public bool IsTransparent => isTransparent;

            public ModelMesh Mesh { get; }

            public ModelMeshPart MeshPart { get; }

            private readonly List<Matrix> worldTransforms = new();
            private readonly List<Matrix[]> skinTransforms = new();
            private readonly List<Vector4> staticColors = new();
            private readonly List<Vector4> skinnedColors = new();
            private readonly List<Vector4> staticLights = new();
            private readonly List<Vector4> skinnedLights = new();

            /// <summary>
            /// Store vertex / index buffers previously set. Set this
            /// to null each frame.
            /// </summary>
            public static VertexBuffer CachedVertexBuffer;
            public static IndexBuffer CachedIndexBuffer;

            public Renderable(ModelMesh mesh, ModelMeshPart part, bool isTransparent)
            {
                Mesh = mesh;
                MeshPart = part;
                this.isTransparent = isTransparent;

                world = effect.Parameters["World"];
                bones = effect.Parameters["Bones"];
                diffuse = effect.Parameters["Diffuse"];
                light = effect.Parameters["LightColor"];
                worldInverse = effect.Parameters["WorldInverse"];
                worldInverseTranspose = effect.Parameters["WorldInverseTranspose"];
            }

            /// <summary>
            /// Add a new static model for drawing this frame.
            /// </summary>
            public void Add(Matrix transform, Vector4 diffuse, Vector4 light)
            {
                worldTransforms.Add(transform);
                staticColors.Add(diffuse);
                staticLights.Add(new Vector4(0.5f, 0.5f, 0.5f, 1.0f) + light / 0.5f);
            }

            /// <summary>
            /// Add a new skinned model for drawing this frame.
            /// </summary>
            public void Add(Matrix[] bones, Vector4 diffuse, Vector4 light)
            {
                if (bones == null)
                {
                    throw new ArgumentNullException();
                }

                skinTransforms.Add(bones);
                skinnedColors.Add(diffuse);
                skinnedLights.Add(new Vector4(0.5f, 0.5f, 0.5f, 1.0f) + light / 0.5f);
            }

            public void Draw()
            {
                if (worldTransforms.Count <= 0 && skinTransforms.Count <= 0)
                {
                    return;
                }

                if (MeshPart.PrimitiveCount <= 0)
                {
                    return;
                }

                game.GraphicsDevice.SetRenderState(
                    isTransparent ? BlendState.AlphaBlend : BlendState.Opaque,
                    rasterizerState: RasterizerState.CullNone);

                // Set index buffer
                if (Mesh.IndexBuffer != CachedIndexBuffer)
                {
                    CachedIndexBuffer = Mesh.IndexBuffer;
                    game.GraphicsDevice.Indices = CachedIndexBuffer;
                }

                // Set vertex buffer
                if (Mesh.VertexBuffer != CachedVertexBuffer)
                {
                    CachedVertexBuffer = Mesh.VertexBuffer;
                    game.GraphicsDevice.Vertices[0].SetSource(
                        CachedVertexBuffer, MeshPart.StreamOffset, MeshPart.VertexStride);
                }

                // Set vertex declaraction
                game.GraphicsDevice.VertexDeclaration = MeshPart.VertexDeclaration;

                // Draw static renderables
                for (var i = 0; i < worldTransforms.Count; i++)
                {
                    var worldInvert = Matrix.Invert(worldTransforms[i]);

                    if (world != null)
                    {
                        world.SetValue(worldTransforms[i]);
                    }

                    if (worldInverse != null)
                    {
                        worldInverse.SetValue(worldInvert);
                    }

                    if (diffuse != null)
                    {
                        diffuse.SetValue(staticColors[i]);
                    }

                    if (light != null)
                    {
                        light.SetValue(staticLights[i]);
                    }

                    if (worldInverseTranspose != null)
                    {
                        worldInverseTranspose.SetValue(Matrix.Transpose(worldInvert));
                    }

                    effect.CommitChanges();

                    game.GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, MeshPart.BaseVertex, 0,
                        MeshPart.NumVertices, MeshPart.StartIndex, MeshPart.PrimitiveCount);
                }

                // Draw skinned renderables
                for (var i = 0; i < skinTransforms.Count; i++)
                {
                    if (bones != null)
                    {
                        bones.SetValue(skinTransforms[i]);
                    }

                    if (diffuse != null)
                    {
                        diffuse.SetValue(skinnedColors[i]);
                    }

                    if (light != null)
                    {
                        light.SetValue(skinnedLights[i]);
                    }

                    effect.CommitChanges();

                    game.GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, MeshPart.BaseVertex, 0,
                        MeshPart.NumVertices, MeshPart.StartIndex, MeshPart.PrimitiveCount);
                }

                // Clear everything after they are drawed
                worldTransforms.Clear();
                skinTransforms.Clear();
                staticColors.Clear();
                skinnedColors.Clear();
                staticLights.Clear();
                skinnedLights.Clear();
            }
        }

        public class RenderablePerMaterial
        {
            private readonly Effect effect = ModelManager._effect;

            private readonly EffectParameter basicTexture;
            private readonly EffectParameter normalTexture;
            private readonly List<Renderable> renderables = new();

            public Material Material { get; }

            public RenderablePerMaterial(Material material)
            {
                Material = material;

                // We'll be adjusting these colors in our .fx file
                // ambient = effect.Parameters["Ambient"];
                // emissive = effect.Parameters["Emissive"];
                // specular = effect.Parameters["Specular"];
                // specularPower = effect.Parameters["SpecularPower"];
                basicTexture = effect.Parameters["BasicTexture"];
                normalTexture = effect.Parameters["NormalTexture"];
            }

            public void Draw()
            {
                if (renderables.Count <= 0)
                {
                    return;
                }

                if (basicTexture != null)
                {
                    basicTexture.SetValue(Material.Texture);
                }

                if (normalTexture != null)
                {
                    normalTexture.SetValue(Material.NormalTexture);
                }

                foreach (Renderable r in renderables)
                {
                    r.Draw();
                }
            }

            public Renderable GetRenderable(ModelMesh mesh, ModelMeshPart part, Material material)
            {
                // Search for all renderables
                foreach (Renderable r in renderables)
                {
                    if (r.MeshPart == part &&
                        r.IsTransparent == material.IsTransparent)
                    {
                        return r;
                    }
                }

                // If it is a new model mesh part, create a new renderable
                var newRenderable = new Renderable(mesh, part, material.IsTransparent);
                renderables.Add(newRenderable);
                return newRenderable;
            }
        }

        public class RenderablePerTechnique
        {
            public EffectTechnique Technique { get; }

            private readonly List<RenderablePerMaterial> renderables = new();

            public RenderablePerTechnique(EffectTechnique technique)
            {
                Technique = technique;
            }

            public Renderable GetRenderable(ModelMesh mesh, ModelMeshPart part, Material material)
            {
                foreach (RenderablePerMaterial r in renderables)
                {
                    // FIXME: Only test texture...
                    if (r.Material.Texture == material.Texture)
                    {
                        return r.GetRenderable(mesh, part, material);
                    }
                }

                // Add a new material
                var newRenderable = new RenderablePerMaterial(material);
                renderables.Add(newRenderable);
                return newRenderable.GetRenderable(mesh, part, material);
            }

            public void Draw(Effect effect)
            {
                if (renderables.Count <= 0)
                {
                    return;
                }

                effect.CurrentTechnique = Technique;

                effect.Begin();
                effect.CurrentTechnique.Passes[0].Begin();

                foreach (RenderablePerMaterial r in renderables)
                {
                    r.Draw();
                }

                effect.CurrentTechnique.Passes[0].End();
                effect.End();
            }
        }

        private readonly GraphicsDevice _graphics;

        internal static Effect _effect;

        private readonly EffectParameter _view;
        private readonly EffectParameter _projection;
        private readonly EffectParameter _viewProjection;
        private readonly EffectParameter _viewInverse;

        /// <summary>
        /// A list storing all opaque renderables, drawed first.
        /// </summary>
        private readonly List<RenderablePerTechnique> _opaque = new();
        private readonly List<RenderablePerTechnique> _transparent = new();

        public ModelManager(GraphicsDevice graphics, ContentManager content)
        {
            _graphics = graphics;
            _effect = content.Load<Effect>("Effects/Model");
            _view = _effect.Parameters["View"];
            _viewInverse = _effect.Parameters["ViewInverse"];
            _projection = _effect.Parameters["Projection"];
            _viewProjection = _effect.Parameters["ViewProjection"];

            // Build an effect technique hierarchy
            foreach (EffectTechnique technique in _effect.Techniques)
            {
                _opaque.Add(new RenderablePerTechnique(technique));
                _transparent.Add(new RenderablePerTechnique(technique));
            }
        }

        public Renderable GetRenderable(ModelMesh mesh, ModelMeshPart part, Material material)
        {
            // Assign a default technique
            if (material.Technique == null)
            {
                material.Technique = _effect.Techniques[0];
            }

            if (material.IsTransparent)
            {
                foreach (RenderablePerTechnique r in _transparent)
                {
                    if (r.Technique == material.Technique)
                    {
                        return r.GetRenderable(mesh, part, material);
                    }
                }
            }
            else
            {
                foreach (RenderablePerTechnique r in _opaque)
                {
                    if (r.Technique == material.Technique)
                    {
                        return r.GetRenderable(mesh, part, material);
                    }
                }
            }

            return null;
        }

        public void Present(Matrix view, Matrix projection, ShadowEffect shadow = null, bool showOpaque = true, bool showTransparent = true)
        {
            if (_opaque.Count <= 0 || (!showOpaque && !showTransparent))
            {
                return;
            }

            // Setup render state
            _graphics.SetRenderState(BlendState.Opaque, DepthStencilState.Default, RasterizerState.CullNone);

            // Clear cached renderable variable each frame
            Renderable.CachedIndexBuffer = null;
            Renderable.CachedVertexBuffer = null;

            _view?.SetValue(view);
            _projection?.SetValue(projection);
            _viewInverse?.SetValue(Matrix.Invert(view));
            _viewProjection?.SetValue(view * projection);

            if (shadow != null)
            {
                _effect.Parameters["LightViewProjection"].SetValue(shadow.LightViewProjection);
            }

            if (showOpaque)
            {
                foreach (RenderablePerTechnique r in _opaque)
                {
                    r.Draw(_effect);
                }
            }

            _graphics.SetRenderState(BlendState.AlphaBlend, DepthStencilState.Default, RasterizerState.CullNone);

            if (showTransparent)
            {
                foreach (RenderablePerTechnique r in _transparent)
                {
                    r.Draw(_effect);
                }
            }
        }
    }
}