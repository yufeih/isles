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
    #region Material
    /// <summary>
    /// Material for every game model
    /// </summary>
    /// <remarks>
    /// GameModel Initialization
    ///     -> Fill material from game model effect settings
    ///         Skinned/Animated model, Transparency, Textures
    ///     -> Deserialize/Serialize material settings from external xml DOM
    ///         This is not a requirement
    ///     -> Programatically change material settings
    ///         By manipulating game model material property directly
    /// 
    /// GameModel:
    ///     - Retrieve model hierarchy / model part by name
    ///     - Gets or sets individual bone trasform / position
    ///     - Assign different materials to individual mesh part
    /// </remarks>
    public class Material
    {
        public Effect Effect;

        public Texture2D BaseTexture;
        public Texture2D NormalTexture;

        public Color AmbientColor;
        public Color DiffuseColor;
        public Color SpecularColor;

        public float Alpha;
        public float SpecularPower;

        public bool IsTransparent;
    }
    #endregion

    #region GameModel
    public class GameModel
    {
        #region Fields
        /// <summary>
        /// Current game
        /// </summary>
        protected BaseGame game;

        /// <summary>
        /// Model axis aligned bounding box
        /// </summary>
        BoundingBox boundingBox;
        
        /// <summary>
        /// Bounding box of the xna model. (Not always axis aligned)
        /// </summary>
        BoundingBox orientedBoundingBox;

        /// <summary>
        /// Whether we should refresh our axis aligned bounding box
        /// </summary>
        bool isBoundingBoxDirty = true;

        /// <summary>
        /// Model world transform
        /// </summary>
        Matrix transform = Matrix.Identity;

        /// <summary>
        /// XNA model class
        /// </summary>
        Model model;

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
        /// Gets or sets model world transform
        /// </summary>
        public Matrix Transform
        {
            get { return transform; }
            set { transform = value; isBoundingBoxDirty = true; }
        }

        /// <summary>
        /// Gets the axis aligned bounding box of this model
        /// </summary>
        public BoundingBox BoundingBox
        {
            get
            {
                if (isBoundingBoxDirty)
                {
                    boundingBox = AABBFromOBB(orientedBoundingBox, transform);
                    isBoundingBoxDirty = false;
                }

                return boundingBox;
            }
        }

        /// <summary>
        /// Gets whether a model contains any animation
        /// </summary>
        public bool IsAnimatedModel
        {
            get { return skin != null; }
        }

        /// <summary>
        /// Gets or sets model effect
        /// </summary>
        public Effect Effect
        {
            get { return effect; }
            set { effect = value; }
        }

        /// <summary>
        /// Gets or sets the xna model.
        /// </summary>
        public Model Model
        {
            get { return model; }
            set { model = value; Refresh(); }
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
            // Make sure this is the upper case Model
            Model = model;
        }

        public GameModel(string modelAssetname)
            : this()
        {
            // Make sure this is the upper case Model
            Model = game.Content.Load<Model>(modelAssetname);
        }

        /// <summary>
        /// Load the game model from XNB model file
        /// </summary>
        /// <param name="modelFilename"></param>
        public void Load(string modelAssetname)
        {
            // Make sure this is the upper case Model
            Model = game.Content.Load<Model>(modelAssetname);
        }

        /// <summary>
        /// Manually refresh the game model.
        /// This method currently re-compute the bounding box of the model.
        /// But you do not have to call this after calling SetModel
        /// </summary>
        public void Refresh()
        {
            if (model != null)
            {
                // Adjust bone array size
                if (bones.Length < model.Bones.Count)
                    bones = new Matrix[model.Bones.Count];

                player = null;
                skin = model.Tag as SkinningData;

                if (skin != null)
                {
                    player = new AnimationPlayer(skin);

                    // Play the first animation clip
                    PlayAnimation();
                }
            }

            // Compute model bounding box.
            orientedBoundingBox = OBBFromModel(model);
            isBoundingBoxDirty = true;
        }

        /// <summary>
        /// Compute bounding box for the specified xna model.
        /// </summary>
        protected static BoundingBox OBBFromModel(Model model)
        {
            if (null == model)
                return new BoundingBox(Vector3.Zero, Vector3.Zero);

            const float FloatMax = 1000000;

            // Compute bounding box
            Vector3 min = new Vector3(FloatMax, FloatMax, FloatMax);
            Vector3 max = new Vector3(-FloatMax, -FloatMax, -FloatMax);

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
        /// Compute the axis aligned bounding box from an oriented bounding box
        /// </summary>
        public static BoundingBox AABBFromOBB(BoundingBox box, Matrix transform)
        {
            const float FloatMax = 1000000;

            // Find the 8 corners
            Vector3[] corners = box.GetCorners();

            // Compute bounding box
            Vector3 min = new Vector3(FloatMax, FloatMax, FloatMax);
            Vector3 max = new Vector3(-FloatMax, -FloatMax, -FloatMax);

            foreach (Vector3 c in corners)
            {
                Vector3 v = Vector3.Transform(c, transform);

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

            return new BoundingBox(min, max);
        }
        

        /// <summary>
        /// Center the model
        /// </summary>
        /// <param name="centerZ">Whether Z value should be centered</param>
        public void CenterModel(bool centerZ)
        {
            // FIXME
            
            //if (model == null)
            //    return;
                        
            //// Compute offset
            //Vector3 offset = -(orientedBoundingBox.Max + orientedBoundingBox.Min) / 2;

            //if (!centerZ)
            //    offset.Z = 0;

            //// Update model transform
            //model.Root.Transform *= Matrix.CreateTranslation(offset);

            //// Reset bounding box
            //orientedBoundingBox.Max += offset;
            //orientedBoundingBox.Min += offset;
            
            //// Mark dirty
            //isBoundingBoxDirty = true;
        }

        /// <summary>
        /// Play the default animation
        /// </summary>
        /// <returns>Succeeded or not</returns>
        public bool PlayAnimation()
        {
            if (player == null)
                return false;

            IEnumerator<KeyValuePair<string, AnimationClip>> enumerator =
                skin.AnimationClips.GetEnumerator();

            if (enumerator.MoveNext())
                player.StartClip(enumerator.Current.Value);

            return true;
        }

        /// <summary>
        /// Play an animation clip
        /// </summary>
        /// <param name="clipName">
        /// Clip name.
        /// </param>
        /// <returns>Succeeded or not</returns>
        public bool PlayAnimation(string clipName)
        {
            if (player == null)
                return false;

            if (clipName == null)
                return false;

            // Play the animation clip with the specified name
            AnimationClip clip;

            if (skin.AnimationClips.TryGetValue(clipName, out clip))
            {
                player.StartClip(clip);
                return true;
            }
            
            // TODO: Stop the animation
            return false;
        }

        public virtual void Update(GameTime gameTime)
        {
            if (player != null)
                player.Update(gameTime.ElapsedGameTime, true, transform);
        }

        public virtual void Draw(GameTime gameTime)
        {
            if (model == null)
                return;

            if (player != null)
            {
                DrawSkinnedModel();
                return;
            }

            model.CopyAbsoluteBoneTransformsTo(bones);

            // Turn on alpha blending
            game.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            game.GraphicsDevice.RenderState.AlphaTestEnable = true;

            // Draw plain using model effect
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = bones[mesh.ParentBone.Index] * transform;
                    effect.View = game.View;
                    effect.Projection = game.Projection;
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
    #endregion
}
