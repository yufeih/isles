// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
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
            return ModelManager.ModelEffect == null ? throw new InvalidOperationException() : ModelManager.ModelEffect.Techniques[techniqueName];
        }
    }

    /// <summary>
    /// Class for managing model rendering
    ///
    /// Transparency -> Technique -> Texture -> Color & Transform.
    /// </summary>
    public class ModelManager
    {
        public class Renderable
        {
            private readonly BaseGame game = BaseGame.Singleton;
            private readonly Effect effect = ModelManager.ModelEffect;
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
                this.Mesh = mesh;
                this.MeshPart = part;
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

                game.GraphicsDevice.SetDepthStencilState(DepthStencilState.Default);
                game.GraphicsDevice.SetBlendState(isTransparent ? BlendState.AlphaBlend : BlendState.Opaque);
                game.GraphicsDevice.SetRasterizerStateState(RasterizerState.CullNone);

                // Set index buffer
                if (MeshPart.IndexBuffer != CachedIndexBuffer)
                {
                    CachedIndexBuffer = MeshPart.IndexBuffer;
                    game.GraphicsDevice.Indices = CachedIndexBuffer;
                }

                // Set vertex buffer
                if (MeshPart.VertexBuffer != CachedVertexBuffer)
                {
                    CachedVertexBuffer = MeshPart.VertexBuffer;
                    game.GraphicsDevice.SetVertexBuffer(CachedVertexBuffer);
                }

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

                    effect.CurrentTechnique.Passes[0].Apply();

                    game.GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, 0, 0,
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

                    effect.CurrentTechnique.Passes[0].Apply();

                    game.GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, 0, 0,
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
            private readonly Effect effect = ModelManager.ModelEffect;

            private readonly EffectParameter basicTexture;
            private readonly EffectParameter normalTexture;
            private readonly List<Renderable> renderables = new();

            public Material Material { get; }

            public RenderablePerMaterial(Material material)
            {
                this.Material = material;

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
                this.Technique = technique;
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

                effect.CurrentTechnique.Passes[0].Apply();

                foreach (RenderablePerMaterial r in renderables)
                {
                    r.Draw();
                }
            }
        }

        /// <summary>
        /// Our base game.
        /// </summary>
        private readonly BaseGame game = BaseGame.Singleton;

        /// <summary>
        /// Effect parameters.
        /// </summary>
        private EffectParameter view;
        private EffectParameter projection;
        private EffectParameter viewProjection;
        private EffectParameter viewInverse;

        public Matrix ViewProjectionMatrix;

        /// <summary>
        /// Gets the model effect used by the model manager.
        /// </summary>
        public static Effect ModelEffect { get; private set; }

        /// <summary>
        /// A list storing all opaque renderables, drawed first.
        /// </summary>
        private readonly List<RenderablePerTechnique> opaque = new();
        private readonly List<RenderablePerTechnique> transparent = new();

        /// <summary>
        /// Creates a new model manager.
        /// </summary>
        public ModelManager()
        {
            CreateEffect();

            // Build an effect technique hierarchy
            foreach (EffectTechnique technique in ModelEffect.Techniques)
            {
                opaque.Add(new RenderablePerTechnique(technique));
                transparent.Add(new RenderablePerTechnique(technique));
            }
        }

        private void CreateEffect()
        {
            ModelEffect = game.Content.Load<Effect>("Effects/Model");
            view = ModelEffect.Parameters["View"];
            viewInverse = ModelEffect.Parameters["ViewInverse"];
            projection = ModelEffect.Parameters["Projection"];
            viewProjection = ModelEffect.Parameters["ViewProjection"];
        }

        /// <summary>
        /// Gets the corrensponding renderable from a specific material and model mesh part.
        /// </summary>
        public Renderable GetRenderable(
            ModelMesh mesh, ModelMeshPart part, Material material)
        {
            // Assign a default technique
            if (material.Technique == null)
            {
                material.Technique = ModelEffect.Techniques[0];
            }

            if (material.IsTransparent)
            {
                foreach (RenderablePerTechnique r in transparent)
                {
                    if (r.Technique == material.Technique)
                    {
                        return r.GetRenderable(mesh, part, material);
                    }
                }
            }
            else
            {
                foreach (RenderablePerTechnique r in opaque)
                {
                    if (r.Technique == material.Technique)
                    {
                        return r.GetRenderable(mesh, part, material);
                    }
                }
            }

            return null;
        }

        public Matrix LightProjection;

        public void Present()
        {
            Present(game.View, game.Projection, null);
        }

        public void Present(ShadowEffect shadow)
        {
            Present(game.View, game.Projection, shadow);
        }

        public void Present(Matrix view, Matrix projection)
        {
            Present(view, projection, null);
        }

        public void Present(Matrix v, Matrix p, ShadowEffect shadow)
        {
            Present(v, p, shadow, true, true);
        }

        /// <summary>
        /// Draw all the models registered this frame onto the screen.
        /// </summary>
        public void Present(Matrix v, Matrix p, ShadowEffect shadow, bool showOpaque, bool showTransparent)
        {
            if (opaque.Count <= 0 || (!showOpaque && !showTransparent))
            {
                return;
            }

            // Setup render state
            game.GraphicsDevice.SetBlendState(BlendState.Opaque);
            game.GraphicsDevice.SetDepthStencilState(DepthStencilState.Default);
            game.GraphicsDevice.SetRasterizerStateState(RasterizerState.CullNone);

            // Clear cached renderable variable each frame
            Renderable.CachedIndexBuffer = null;
            Renderable.CachedVertexBuffer = null;

            ViewProjectionMatrix = v * p;

            if (view != null)
            {
                view.SetValue(v);
            }

            if (projection != null)
            {
                projection.SetValue(p);
            }

            if (viewInverse != null)
            {
                viewInverse.SetValue(Matrix.Invert(v));
            }

            if (viewProjection != null)
            {
                viewProjection.SetValue(ViewProjectionMatrix);
            }

            if (shadow != null)
            {
                ModelEffect.Parameters["LightViewProjection"].SetValue(shadow.ViewProjection);
            }

            if (showOpaque)
            {
                foreach (RenderablePerTechnique r in opaque)
                {
                    r.Draw(ModelEffect);
                }
            }

            game.GraphicsDevice.SetBlendState(BlendState.AlphaBlend);

            if (showTransparent)
            {
                foreach (RenderablePerTechnique r in transparent)
                {
                    r.Draw(ModelEffect);
                }
            }
        }
    }
}