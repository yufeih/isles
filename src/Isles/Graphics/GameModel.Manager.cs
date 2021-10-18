//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    #region Material
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
        /// Gets or sets whether the material has changed
        /// </summary>
        public bool IsDirty
        {
            get => isDirty;
            set => isDirty = value;
        }

        private bool isDirty = true;

        /// <summary>
        /// The effect technique for the material
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
        /// Base model texture
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
        /// Model normal texture
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
        /// Ambient color
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
        /// Diffuse color
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
        /// Specular color
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
        /// Specular power
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

        /// <summary>
        /// Trick: Enable AlphaTest for trees
        /// 
        /// Opaque:
        /// Default -   Tint
        /// Tree    -   AlphaTestEnabled
        /// 
        /// Transparent:
        /// Default Transparent
        /// Tree Transparent
        /// </summary>
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
        /// Creates a new material
        /// </summary>
        public Material() { }

        /// <summary>
        /// Creates a new material
        /// </summary>
        public Material(string techniqueName)
        {
            Technique = GetTechnique(techniqueName);
        }

        /// <summary>
        /// Creates a new material from basic effect
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
        /// An empty material
        /// </summary>
        public static Material Default = new();

        /// <summary>
        /// The shadow map material
        /// </summary>
        public static Material ShadowMap = new("ShadowMapping");
        public static Material ShadowMapSkinned = new("ShadowMappingSkinned");

        /// <summary>
        /// Gets the effect technique with the speicific name
        /// </summary>
        public static EffectTechnique GetTechnique(string techniqueName)
        {
            return ModelManager.ModelEffect == null ? throw new InvalidOperationException() : ModelManager.ModelEffect.Techniques[techniqueName];
        }
    }
    #endregion

    #region ModelManager
    /// <summary>
    /// Class for managing model rendering
    /// 
    /// Transparency -> Technique -> Texture -> Color & Transform
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
            private readonly ModelMesh mesh;
            private readonly ModelMeshPart part;
            private readonly ModelManager manager;
            private readonly bool isTransparent;

            public bool IsTransparent => isTransparent;

            public ModelMesh Mesh => mesh;

            public ModelMeshPart MeshPart => part;

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

            public Renderable(ModelManager manager, ModelMesh mesh, ModelMeshPart part, bool isTransparent)
            {
                this.manager = manager;
                this.mesh = mesh;
                this.part = part;
                this.isTransparent = isTransparent;

                world = effect.Parameters["World"];
                bones = effect.Parameters["Bones"];
                diffuse = effect.Parameters["Diffuse"];
                light = effect.Parameters["LightColor"];
                worldInverse = effect.Parameters["WorldInverse"];
                worldInverseTranspose = effect.Parameters["WorldInverseTranspose"];
            }

            /// <summary>
            /// Add a new static model for drawing this frame
            /// </summary>
            public void Add(Matrix transform, Vector4 diffuse, Vector4 light)
            {
                worldTransforms.Add(transform);
                staticColors.Add(diffuse);
                staticLights.Add(new Vector4(0.5f, 0.5f, 0.5f, 1.0f) + light / 0.5f);
            }

            /// <summary>
            /// Add a new skinned model for drawing this frame
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

            public void Draw(GameTime gameTime)
            {
                if (worldTransforms.Count <= 0 && skinTransforms.Count <= 0)
                {
                    return;
                }

                if (part.PrimitiveCount <= 0)
                {
                    return;
                }

                // Set index buffer
                if (mesh.IndexBuffer != CachedIndexBuffer)
                {
                    CachedIndexBuffer = mesh.IndexBuffer;
                    game.GraphicsDevice.Indices = CachedIndexBuffer;
                }

                // Set vertex buffer
                if (mesh.VertexBuffer != CachedVertexBuffer)
                {
                    CachedVertexBuffer = mesh.VertexBuffer;
                    game.GraphicsDevice.Vertices[0].SetSource(
                        CachedVertexBuffer, part.StreamOffset, part.VertexStride);
                }

                // Set vertex declaraction
                game.GraphicsDevice.VertexDeclaration = part.VertexDeclaration;

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

                    ResolveAlphaIssues(staticColors[i].W);

                    game.GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, part.BaseVertex, 0,
                        part.NumVertices, part.StartIndex, part.PrimitiveCount);
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

                    ResolveAlphaIssues(skinnedColors[i].W);

                    game.GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, part.BaseVertex, 0,
                        part.NumVertices, part.StartIndex, part.PrimitiveCount);
                }

                // Clear everything after they are drawed
                worldTransforms.Clear();
                skinTransforms.Clear();
                staticColors.Clear();
                skinnedColors.Clear();
                staticLights.Clear();
                skinnedLights.Clear();
            }

            //public static bool IsLastRenderableTransparent = false;

            private void ResolveAlphaIssues(float alpha)
            {
                const float ReferenceAlpha = 128;

                if (isTransparent)
                {
                    game.GraphicsDevice.RenderState.AlphaTestEnable = true;
                    game.GraphicsDevice.RenderState.ReferenceAlpha = (int)(ReferenceAlpha * alpha);
                    game.GraphicsDevice.RenderState.AlphaBlendEnable = true;
                    game.GraphicsDevice.RenderState.AlphaSourceBlend = Blend.SourceAlpha;

                    // Setting destination blend here seems not working?
                    // Probably a bug in XNA. I'll have to change the destination blend in the effect file
                    game.GraphicsDevice.RenderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                }
                else
                {
                    game.GraphicsDevice.RenderState.AlphaBlendEnable = false;
                    game.GraphicsDevice.RenderState.AlphaTestEnable = true;
                    game.GraphicsDevice.RenderState.ReferenceAlpha = (int)(ReferenceAlpha);
                }
            }
        }

        public class RenderablePerMaterial
        {
            private readonly Effect effect = ModelManager.ModelEffect;

            //EffectParameter ambient;
            //EffectParameter emissive;
            //EffectParameter specular;
            //EffectParameter specularPower;
            private readonly EffectParameter basicTexture;
            private readonly EffectParameter normalTexture;
            private readonly Material material;
            private readonly List<Renderable> renderables = new();

            public Material Material => material;

            public RenderablePerMaterial(Material material)
            {
                this.material = material;

                // We'll be adjusting these colors in our .fx file
                //ambient = effect.Parameters["Ambient"];
                //emissive = effect.Parameters["Emissive"];
                //specular = effect.Parameters["Specular"];
                //specularPower = effect.Parameters["SpecularPower"];
                basicTexture = effect.Parameters["BasicTexture"];
                normalTexture = effect.Parameters["NormalTexture"];
            }

            public void Draw(GameTime gameTime)
            {
                if (renderables.Count <= 0)
                {
                    return;
                }

                //if (ambient != null)
                //    ambient.SetValue(material.AmbientColor);
                //if (emissive != null)
                //    emissive.SetValue(material.EmissiveColor);
                //if (specular != null)
                //    specular.SetValue(material.SpecularColor);
                //if (specularPower != null)
                //    specularPower.SetValue(material.SpecularPower);

                if (basicTexture != null)
                {
                    basicTexture.SetValue(material.Texture);
                }

                if (normalTexture != null)
                {
                    normalTexture.SetValue(material.NormalTexture);
                }

                if (material.IsTransparent)
                {
                    BaseGame.Singleton.GraphicsDevice.RenderState.AlphaTestEnable = true;
                    BaseGame.Singleton.GraphicsDevice.RenderState.ReferenceAlpha = 128;
                }

                foreach (Renderable r in renderables)
                {
                    r.Draw(gameTime);
                }
            }

            public Renderable GetRenderable(
                ModelManager manager, ModelMesh mesh, ModelMeshPart part, Material material)
            {
                // Search for all renderables
                foreach (Renderable r in renderables)
                {
                    if (r.MeshPart == part &&
                        r.IsTransparent == material.IsTransparent)
                    {
                        System.Diagnostics.Debug.Assert(mesh == r.Mesh);
                        return r;
                    }
                }

                // If it is a new model mesh part, create a new renderable
                var newRenderable = new Renderable(manager, mesh,
                                                          part, material.IsTransparent);
                renderables.Add(newRenderable);
                return newRenderable;
            }
        }

        public class RenderablePerTechnique
        {
            public EffectTechnique Technique => technique;

            private readonly EffectTechnique technique;
            private readonly List<RenderablePerMaterial> renderables = new();

            public RenderablePerTechnique(EffectTechnique technique)
            {
                this.technique = technique;
            }

            public Renderable GetRenderable(
                ModelManager manager, ModelMesh mesh, ModelMeshPart part, Material material)
            {
                foreach (RenderablePerMaterial r in renderables)
                {
                    // FIXME: Only test texture...
                    if (r.Material.Texture == material.Texture)
                    {
                        return r.GetRenderable(manager, mesh, part, material);
                    }
                }

                // Add a new material
                var newRenderable = new RenderablePerMaterial(material);
                renderables.Add(newRenderable);
                return newRenderable.GetRenderable(manager, mesh, part, material);
            }

            public void Draw(Effect effect, GameTime gameTime)
            {
                if (renderables.Count <= 0)
                {
                    return;
                }

                effect.CurrentTechnique = technique;

                effect.Begin();

                foreach (EffectPass pass in technique.Passes)
                {
                    pass.Begin();

                    foreach (RenderablePerMaterial r in renderables)
                    {
                        r.Draw(gameTime);
                    }

                    pass.End();
                }

                effect.End();
            }
        }

        /// <summary>
        /// Our base game
        /// </summary>
        private readonly BaseGame game = BaseGame.Singleton;

        /// <summary>
        /// Effect used to draw everything
        /// </summary>
        private static Effect effect;

        /// <summary>
        /// Effect parameters
        /// </summary>
        private EffectParameter view;
        private EffectParameter projection;
        private EffectParameter viewProjection;
        private EffectParameter viewInverse;

        public Matrix ViewProjectionMatrix;

        /// <summary>
        /// Gets the model effect used by the model manager
        /// </summary>
        public static Effect ModelEffect => effect;

        /// <summary>
        /// A list storing all opaque renderables, drawed first.
        /// </summary>
        private readonly List<RenderablePerTechnique> opaque = new();
        private readonly List<RenderablePerTechnique> transparent = new();

        /// <summary>
        /// Creates a new model manager
        /// </summary>
        public ModelManager()
        {
            CreateEffect();

            // Build an effect technique hierarchy
            foreach (EffectTechnique technique in effect.Techniques)
            {
                opaque.Add(new RenderablePerTechnique(technique));
                transparent.Add(new RenderablePerTechnique(technique));
            }
        }

        private void CreateEffect()
        {
            effect = game.ZipContent.Load<Effect>("Effects/Model");
            view = effect.Parameters["View"];
            viewInverse = effect.Parameters["ViewInverse"];
            projection = effect.Parameters["Projection"];
            viewProjection = effect.Parameters["ViewProjection"];
        }

        /// <summary>
        /// Gets the corrensponding renderable from a specific material and model mesh part
        /// </summary>
        public Renderable GetRenderable(
            ModelMesh mesh, ModelMeshPart part, Material material)
        {
            // Assign a default technique
            if (material.Technique == null)
            {
                material.Technique = effect.Techniques[0];
            }

            if (material.IsTransparent)
            {
                foreach (RenderablePerTechnique r in transparent)
                {
                    if (r.Technique == material.Technique)
                    {
                        return r.GetRenderable(this, mesh, part, material);
                    }
                }
            }
            else
            {
                foreach (RenderablePerTechnique r in opaque)
                {
                    if (r.Technique == material.Technique)
                    {
                        return r.GetRenderable(this, mesh, part, material);
                    }
                }
            }

            return null;
        }

        public Matrix LightProjection;

        public void Present(GameTime gameTime)
        {
            Present(gameTime, game.View, game.Projection, null);
        }

        public void Present(GameTime gameTime, ShadowEffect shadow)
        {
            Present(gameTime, game.View, game.Projection, shadow);
        }

        public void Present(GameTime gameTime, Matrix view, Matrix projection)
        {
            Present(gameTime, view, projection, null);
        }

        public void Present(GameTime gameTime, Matrix v, Matrix p, ShadowEffect shadow)
        {
            Present(gameTime, v, p, shadow, true, true);
        }

        /// <summary>
        /// Draw all the models registered this frame onto the screen
        /// </summary>
        public void Present(GameTime gameTime, Matrix v, Matrix p,
                            ShadowEffect shadow, bool showOpaque, bool showTransparent)
        {
            if (opaque.Count <= 0 || (!showOpaque && !showTransparent))
            {
                return;
            }

            // Setup render state
            game.GraphicsDevice.RenderState.DepthBufferEnable = true;
            game.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            game.GraphicsDevice.RenderState.CullMode = CullMode.None;
            game.GraphicsDevice.RenderState.AlphaBlendEnable = false;

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
                effect.Parameters["LightViewProjection"].SetValue(shadow.ViewProjection);
            }

            if (showOpaque)
            {
                foreach (RenderablePerTechnique r in opaque)
                {
                    r.Draw(effect, gameTime);
                }
            }

            game.GraphicsDevice.RenderState.AlphaBlendEnable = true;

            if (showTransparent)
            {
                foreach (RenderablePerTechnique r in transparent)
                {
                    r.Draw(effect, gameTime);
                }
            }

            game.GraphicsDevice.RenderState.AlphaBlendEnable = false;
        }
    }
    #endregion
}