//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;


namespace Isles.Graphics
{
    public class GameModel
    {
        #region Fields
        /// <summary>
        /// Current game
        /// </summary>
        protected BaseGame game;

        /// <summary>
        /// Model bounding box
        /// </summary>
        protected BoundingBox boundingBox;

        /// <summary>
        /// Model world transform
        /// </summary>
        protected Matrix transform = Matrix.Identity;

        /// <summary>
        /// XNA model class
        /// </summary>
        protected Model model;

        /// <summary>
        /// Hold all models bones
        /// </summary>
        protected static Matrix[] bones = new Matrix[16];

        /// <summary>
        /// Effect used to draw the model
        /// </summary>
        protected Effect effect;

        /// <summary>
        /// For animated models
        /// </summary>
        protected AnimationPlayer player;

        /// <summary>
        /// For skinned mesh
        /// </summary>
        protected SkinningData skin;

        /// <summary>
        /// Setup model basic effect
        /// </summary>
        public delegate void BasicEffectSettings(BasicEffect effect);

        /// <summary>
        /// Gets or sets model world transform
        /// </summary>
        public Matrix Transform
        {
            get { return transform; }
            set { transform = value; }
        }

        /// <summary>
        /// Gets game model bounding box
        /// </summary>
        public BoundingBox BoundingBox
        {
            get { return boundingBox; }
        }

        /// <summary>
        /// Gets XNA model
        /// </summary>
        public Model Model
        {
            get { return model; }
        }

        /// <summary>
        /// Gets whether a model contains any animation
        /// </summary>
        public bool IsAnimatedModel
        {
            get { return Animation != null; }
        }

        /// <summary>
        /// Gets model animation
        /// </summary>
        public AnimationPlayer Animation
        {
            get { return player; }
        }

        /// <summary>
        /// Gets model skinning data
        /// </summary>
        public SkinningData SkinningData
        {
            get { return skin; }
        }

        /// <summary>
        /// Gets or sets model effect
        /// </summary>
        public Effect Effect
        {
            get { return effect; }
            set { effect = value; }
        }
        #endregion

        #region Methods

        public GameModel()
        {
            this.game = BaseGame.Singleton;
            this.effect = game.Content.Load<Effect>("Effects/Model");
        }

        public GameModel(Model model)
            : this()
        {
            SetModel(model);
        }

        public GameModel(string modelAssetname)
            : this()
        {
            SetModel(game.Content.Load<Model>(modelAssetname));
        }

        /// <summary>
        /// Load the game model from XNB model file
        /// </summary>
        /// <param name="modelFilename"></param>
        public void Load(string modelAssetname)
        {
            SetModel(game.Content.Load<Model>(modelAssetname));
        }

        public void SetModel(Model model)
        {
            this.model = model;

            if (model != null)
            {
                // Adjust bone array size
                if (bones.Length < model.Bones.Count)
                    bones = new Matrix[model.Bones.Count];

                skin = model.Tag as SkinningData;

                if (skin != null)
                {
                    player = new AnimationPlayer(skin);

                    // Play the first animation clip
                    IEnumerator<KeyValuePair<string, AnimationClip>> enumerator =
                        skin.AnimationClips.GetEnumerator();

                    if (enumerator.MoveNext())
                        player.StartClip(enumerator.Current.Value);
                }
            }

            // Compute model bounding box.
            boundingBox = ComputeBoundingBox(model);
        }

        /// <summary>
        /// Manually refresh the game model.
        /// This method currently re-compute the bounding box of the model.
        /// But you do not have to call this after calling SetModel
        /// </summary>
        public void Refresh()
        {
            boundingBox = ComputeBoundingBox(model);
        }

        /// <summary>
        /// Compute bounding box for the model.
        /// NOTE: This method probably DO NOT work for animated mesh
        /// </summary>
        public static BoundingBox ComputeBoundingBox(Model model)
        {
            if (null == model)
                return new BoundingBox(Vector3.Zero, Vector3.Zero);

            // Compute bounding box
            Vector3 min = new Vector3(1000, 1000, 1000);
            Vector3 max = new Vector3(-1000, -1000, -1000);

            Matrix[] bones = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(bones);

            foreach (ModelMesh mesh in model.Meshes)
            {
                int stride = mesh.MeshParts[0].VertexStride;
                int elementCount = mesh.VertexBuffer.SizeInBytes / stride;
                Vector3[] vertices = new Vector3[elementCount];
                mesh.VertexBuffer.GetData<Vector3>(0, vertices, 0, elementCount, stride);

                foreach (Vector3 vertex in vertices)
                {
                    // Transform vertex
                    Vector3 v = Vector3.Transform(vertex, bones[mesh.ParentBone.Index]);

                    if (v.X < min.X)
                        min.X = v.X;
                    if (v.X > max.X)
                        max.X = v.X;

                    if (v.Y < min.Y)
                        min.Y = v.Y;
                    if (v.Y > max.Y)
                        max.Y = v.Y;

                    if (v.Z < min.Z)
                        min.Z = v.Z;
                    if (v.Z > max.Z)
                        max.Z = v.Z;
                }
            }

            return new BoundingBox(min, max);
        }

        /// <summary>
        /// Center the model
        /// </summary>
        /// <param name="centerZ">Whether Z value should be centered</param>
        public void CenterModel(bool centerZ)
        {
            // Compute offset
            Vector3 offset = -(boundingBox.Max + boundingBox.Min) / 2;

            if (!centerZ)
                offset.Z = 0;

            // Update model transform
            model.Root.Transform *= Matrix.CreateTranslation(offset);

            // Reset bounding box
            boundingBox.Max += offset;
            boundingBox.Min += offset;
        }

        public virtual void Update(GameTime gameTime)
        {
            if (player != null)
                player.Update(gameTime.ElapsedGameTime, true, transform);
        }

        public void Draw(GameTime gameTime)
        {
            Draw(gameTime, null);
        }

        public virtual void Draw(GameTime gameTime, BasicEffectSettings setupBasicEffect)
        {
            if (player != null)
            {
                DrawSkinnedModel();
                return;
            }

            model.CopyAbsoluteBoneTransformsTo(bones);

            // Draw plain mesh
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = bones[mesh.ParentBone.Index] * transform;
                    effect.View = game.View;
                    effect.Projection = game.Projection;

                    if (setupBasicEffect != null)
                        setupBasicEffect(effect);
                }

                mesh.Draw();
            }
        }

        public void DrawShadowMapping(GameTime gameTime, ShadowMapEffect shadow)
        {
        }

        protected void DrawSkinnedModel()
        {
            Matrix[] bones = player.GetSkinTransforms();

            // Render the skinned mesh.
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["Bones"].SetValue(bones);
                    effect.Parameters["View"].SetValue(game.View);
                    effect.Parameters["Projection"].SetValue(game.Projection);
                }

                mesh.Draw();
            }
        }

        /// <summary>
        /// Ray intersection test
        /// </summary>
        /// <param name="ray">Target ray</param>
        /// <returns>
        /// Distance from the intersection point to the ray starting position,
        /// Null if there's no intersection.
        /// </returns>
        public Nullable<float> Intersects(Ray ray)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
