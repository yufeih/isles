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
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Isles.Pipeline;
using Isles.Engine;


namespace Isles.Graphics
{
    #region GameModel
    public class GameModel
    {
        #region Fields
        /// <summary>
        /// Current game
        /// </summary>
        protected BaseGame game;


        /// <summary>
        /// Gets or sets model world transform
        /// </summary>
        public Matrix Transform
        {
            get { return transform; }

            set
            {
                transform = value;
                isBoundingBoxDirty = true;

                // Update model transform for static models
                if (model != null && !IsAnimated && !IsSkinned)
                {
                    model.CopyAbsoluteBoneTransformsTo(bones);

                    for (int i = 0; i < bones.Length; i++)
                    {
                        bones[i] *= transform;
                    }
                }
            }
        }

        Matrix transform = Matrix.Identity;

        /// <summary>
        /// Hold all models bone transforms
        /// </summary>
        Matrix[] bones;
        Matrix[] skinTransforms;


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
        /// Gets the oriented bounding box of this model
        /// </summary>
        public BoundingBox OrientedBoundingBox
        {
            get { return orientedBoundingBox; }
        }

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
        /// Gets whether a model contains any animation
        /// </summary>
        public bool IsAnimated
        {
            get { return Player != null; }
        }

        /// <summary>
        /// Gets whether the model is a skinned model
        /// </summary>
        public bool IsSkinned
        {
            get { return skin != null; }
        }

        /// <summary>
        /// Gets or sets the speed of model animation. 1.0f is the normal
        /// speed, the higher the value is, the faster the animation goes.
        /// </summary>
        public float AnimationSpeed
        {
            get { return animationSpeed; }
            set { animationSpeed = value; }
        }

        float animationSpeed = 1.0f;

        /// <summary>
        /// Skinned mesh animation player.
        /// We use 2 players to blend between animations.
        /// </summary>
        AnimationPlayer[] players = new AnimationPlayer[2];

        /// <summary>
        /// Gets the animation player for this game model
        /// </summary>
        public AnimationPlayer Player
        {
            get { return players[currentPlayer]; }
        }

        IDictionary<string, AnimationClip> animationClips;

        /// <summary>
        /// Points to the primary player
        /// </summary>
        int currentPlayer = 0;

        /// <summary>
        /// Whether we are blending between two animations
        /// </summary>
        bool blending = false;

        /// <summary>
        /// Time value for animation blending
        /// </summary>
        double blendStart, blendDuration;

        /// <summary>
        /// Current animation clip being played
        /// </summary>
        AnimationClip currentClip;

        /// <summary>
        /// Represent the state of current animation
        /// </summary>
        enum AnimationState
        {
            Stopped, Playing, Paused
        }

        /// <summary>
        /// Current animation state
        /// </summary>
        AnimationState animationState = AnimationState.Stopped;

        /// <summary>
        /// For skinned mesh
        /// </summary>
        SkinningData skin;

        /// <summary>
        /// Space partition information for this model
        /// </summary>
        ModelSpacePartitionInformation spacePartitionInfo;

        /// <summary>
        /// Gets or sets the xna model.
        /// </summary>
        public Model Model
        {
            get { return model; }
            set { model = value; Refresh(); }
        }

        Model model;


        /// <summary>
        /// Gets or sets the tint color of this game model
        /// </summary>
        public Vector3 Tint
        {
            get
            {
                Vector3 v;
                v.X = tint.X;
                v.Y = tint.Y;
                v.Z = tint.Z;
                return v;
            }

            set
            {
                // Update alpha first
                tint.X = value.X;
                tint.Y = value.Y;
                tint.Z = value.Z;
            }
        }

        /// <summary>
        /// Gets or sets the glow of this game model
        /// </summary>
        public Vector4 Glow
        {
            get { return glow; }
            set { glow = value; glow.W = 1.0f; }
        }

        /// <summary>
        /// Gets or sets the alpha value of model tint
        /// </summary>
        public float Alpha
        {
            get { return tint.W; }

            set
            {
                value = MathHelper.Clamp(value, 0, 1);

                if (value < 1 && tint.W == 1)
                {
                    foreach (Material material in materials)
                        material.IsTransparent = true;
                }
                else if (value == 1 && tint.W < 1)
                {
                    foreach (Material material in materials)
                        material.IsTransparent = false;
                }

                tint.W = value;
            }
        }

        //Vector4 tint = new Vector4(154.0f / 255, 164.0f / 255, 1.0f, 1.0f);
        Vector4 tint = new Vector4(1, 1, 1, 1);
        Vector4 glow = new Vector4(0, 0, 0, 1);

        /// <summary>
        /// Internal list storing all model mesh parts
        /// </summary>
        List<ModelMeshPart> meshParts = new List<ModelMeshPart>();

        /// <summary>
        /// Internal list storing all materials corresponding to each mesh part
        /// </summary>
        List<Material> materials = new List<Material>();

        /// <summary>
        /// Gets game model material
        /// </summary>
        public IList<Material> Materials
        {
            get { return materials; }
        }

        /// <summary>
        /// Internal list storing all renderables corresponding to each mesh part
        /// </summary>
        List<ModelManager.Renderable> renderables = new List<ModelManager.Renderable>();
        List<ModelManager.Renderable> shadowMapRenderables = new List<ModelManager.Renderable>();
        #endregion

        #region Methods
        public GameModel()
        {
            this.game = BaseGame.Singleton;
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
            Model = game.ZipContent.Load<Model>(modelAssetname);
        }

        /// <summary>
        /// Load the game model from XNB model file
        /// </summary>
        /// <param name="modelFilename"></param>
        public void Load(string modelAssetname)
        {
            // Make sure this is the upper case Model
            Model = game.ZipContent.Load<Model>(modelAssetname);
        }

        /// <summary>
        /// Creates a shadow copy of this game model.
        /// The new model refers to the same xna model. Transformation, animation data are copied.
        /// </summary>
        public GameModel ShadowCopy()
        {
            GameModel copy = new GameModel();

            copy.animationClips = animationClips;
            copy.animationSpeed = animationSpeed;
            copy.animationState = animationState;
            copy.blendDuration = blendDuration;
            copy.blending = blending;
            copy.blendStart = blendStart;
            copy.boundingBox = boundingBox;
            copy.currentClip = currentClip;
            copy.currentPlayer = currentPlayer;
            copy.game = game;
            copy.glow = glow;
            copy.isBoundingBoxDirty = isBoundingBoxDirty;
            copy.materials = materials;
            copy.meshParts = meshParts;
            copy.model = model;
            copy.orientedBoundingBox = orientedBoundingBox;
            copy.players = players;
            copy.renderables = renderables;
            copy.shadowMapRenderables = shadowMapRenderables;
            copy.skin = skin;
            copy.spacePartitionInfo = spacePartitionInfo;
            copy.tint = tint;
            copy.transform = transform;

            if (skinTransforms != null)
            {
                copy.skinTransforms = new Matrix[skinTransforms.Length];
                skinTransforms.CopyTo(copy.skinTransforms, 0);
            }

            if (bones != null)
            {
                copy.bones = new Matrix[bones.Length];
                bones.CopyTo(copy.bones, 0);
            }

            return copy;
        }

        /// <summary>
        /// Manually refresh the game model.
        /// This method currently re-compute the bounding box of the model.
        /// </summary>
        public void Refresh()
        {
            if (model != null)
            {
                currentClip = null;
                players[0] = players[1] = null;

                if (model.Tag is Dictionary<string, object>)
                {
                    Dictionary<string, object> dictionary = model.Tag as Dictionary<string, object>;

                    object value;
                    if (dictionary.TryGetValue("SkinningData", out value))
                        skin = value as SkinningData;

                    if (dictionary.TryGetValue("AnimationData", out value))
                        animationClips = value as IDictionary<string, AnimationClip>;
                    dictionary.TryGetValue("SpacePartition", out value);

                    spacePartitionInfo = new ModelSpacePartitionInformation();
                    spacePartitionInfo.BitMap = value as bool[];

                    spacePartitionInfo.Box = OBBFromModel(model);
                }

                if (animationClips != null)
                {
                    if (IsSkinned)
                    {
                        players[0] = new AnimationPlayer(skin);
                        players[1] = new AnimationPlayer(skin);

                        UpdateSkinTransform(new GameTime());
                    }
                    else
                    {
                        players[0] = new AnimationPlayer(model);
                        players[1] = new AnimationPlayer(model);

                        UpdateBoneTransform(new GameTime());
                    }

                    // Play the first animation clip
                    Player.Loop = true;
                    Play();
                }
                else if (bones == null || bones.Length < model.Bones.Count)
                {
                    // Adjust bone array size
                    bones = new Matrix[model.Bones.Count];
                }
                
                // Initialize mesh parts and renderables
                meshParts.Clear();
                materials.Clear();
                renderables.Clear();

                foreach (ModelMesh mesh in model.Meshes)
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        meshParts.Add(part);

                        // TODO: What if the effect cannot be casted to BasicEffect?
                        BasicEffect effect = part.Effect as BasicEffect;

                        Material material = new Material(effect);
                        
                        // Read normal texture from mesh part tag
                        material.NormalTexture = part.Tag as Texture2D;

                        materials.Add(material);

                        // Just add a dummy renderable. After setting the effect to
                        // Default, the materials of the game model are marked dirty,
                        // the renderable will be refreshed during draw call.
                        renderables.Add(null);
                        shadowMapRenderables.Add(null);

                        // Store ModelMesh in the tag of ModelMeshPart
                        part.Tag = mesh;
                    }

                SetEffect("Default");
                //Alpha = 0.15f;
            }

            // Compute model bounding box.
            orientedBoundingBox = OBBFromModel(model);
            isBoundingBoxDirty = true;
        }

        /// <summary>
        /// Sets the material of the game model
        /// </summary>
        /// <param name="name"></param>
        public void SetEffect(string name)
        {
            if (IsSkinned)
                name += "Skinned";

            EffectTechnique technique = Material.GetTechnique(name);

            foreach (Material m in materials)
                m.Technique = technique;
        }

        /// <summary>
        /// Gets the index of a bone with the specific name
        /// </summary>
        /// <returns>
        /// Negtive if the bone not found
        /// </returns>
        public int GetBone(string boneName)
        {
            if (IsSkinned)
            {
                for (int i = 0; i < skin.BoneName.Count; i++)
                    if (skin.BoneName[i].Equals(boneName))
                        return i;
            }
            else
            {
                ModelBone bone;
                if (model.Bones.TryGetValue(boneName, out bone))
                    return bone.Index;
            }

            return -1;
        }

        /// <summary>
        /// Gets the global transformation of the bone.
        /// NOTE: Call this after the model gets drawed.
        /// </summary>
        public Matrix GetBoneTransform(int bone)
        {
            return bones[bone];
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
            if (spacePartitionInfo == null)
            {
                return this.BoundingBox.Intersects(ray);
            }
            Matrix m = Matrix.Invert(Transform);
            Vector3 relarayEnd = Vector3.Transform(ray.Position + ray.Direction, m);
            Vector3 relarayPosition = Vector3.Transform(ray.Position, m);
            Ray relaray = new Ray(relarayPosition, relarayEnd - relarayPosition);
            return Intersects(relaray, 0, 0);
        }

        float? Intersects(Ray ray, int level, int index)
        {
            BoundingBox box;
            float? dist = null;
            if(level == 3)
            {
                if(spacePartitionInfo.BitMap[index])
                {
                    box = spacePartitionInfo.IndexToBoundingBox(index);
                    dist = ray.Intersects(box);
                }
                return dist;
            }

            float minLength = float.PositiveInfinity;
            if(!spacePartitionInfo.BitMap[index])
            {
                dist = ray.Intersects(spacePartitionInfo.IndexToBoundingBox(index));
                if(!dist.HasValue)
                {
                    return dist;
                }
            }
            for (int i = 0; i < 8; i++)
            {
                int son = spacePartitionInfo.SonOf(index, i, level);
                if (spacePartitionInfo.BitMap[son])
                {
                    dist = ray.Intersects(spacePartitionInfo.IndexToBoundingBox(son));
                    if (dist.HasValue)
                    {
                        float? d = Intersects(ray, level + 1, son);
                        if (d.HasValue)
                            minLength = Math.Min(d.Value, minLength);
                    }
                }
            }
            if (minLength != float.PositiveInfinity)
            {
                dist = minLength;
            }
            else
            {
                dist = null;
            }
            return dist;
        }

        #endregion Method

        #region Animation
        /// <summary>
        /// Gets the default (first) animation clip of this game model
        /// </summary>
        /// <returns></returns>
        public AnimationClip GetDefaultAnimationClip()
        {
            if (!IsAnimated)
                return null;

            // This is a little bit tricky
            IEnumerator<KeyValuePair<string, AnimationClip>> enumerator =
                animationClips.GetEnumerator();

            if (enumerator.MoveNext())
                return enumerator.Current.Value;

            return null;
        }

        /// <summary>
        /// Gets the animation clip with the specified name
        /// </summary>
        public AnimationClip GetAnimationClip(string clipName)
        {
            AnimationClip value;
            
            if (animationClips.TryGetValue(clipName, out value))
                return value;

            return null;
        }

        /// <summary>
        /// Play the current (or default) animation.
        /// </summary>
        /// <returns>Succeeded or not</returns>
        public bool Play()
        {
            if (!IsAnimated)
                return false;

            if (currentClip == null)
                currentClip = GetDefaultAnimationClip();

            // Play current animation if it is paused
            if (animationState == AnimationState.Paused)
            {
                animationState = AnimationState.Playing;
            }
            else if (animationState == AnimationState.Stopped)
            {
                players[currentPlayer].StartClip(currentClip);
                animationState = AnimationState.Playing;
            }

            return true;
        }

        /// <summary>
        /// Play an animation clip
        /// </summary>
        /// <param name="clipName">
        /// Clip name.
        /// </param>
        /// <returns>Succeeded or not</returns>
        public bool Play(string clipName)
        {
            if (!IsAnimated || clipName == null)
                return false;

            // Play the animation clip with the specified name
            AnimationClip clip;

            if (animationClips.TryGetValue(clipName, out clip))
            {
                // Do nothing if it's still the same animation clip
                if (clip == currentClip)
                    return true;

                currentClip = clip;
                Player.Triggers = null;
                Player.Loop = true;
                Player.Complete = null;
                Player.StartClip(clip);
                animationState = AnimationState.Playing;
                return true;
            }
            
            // Stop if the clip is invalid
            Stop();
            return false;
        }

        public bool Play(string clipName, bool loop, float blendTime)
        {
            return Play(clipName, loop, blendTime, null, null);
        }

        /// <summary>
        /// Play an animation clip. Blend the new animation with the
        /// old one to provide smooth transition.
        /// </summary>
        /// <param name="clipName"></param>
        /// <param name="blendTime"></param>
        /// <returns>Succeeded or not</returns>
        public bool Play(string clipName, bool loop, float blendTime, EventHandler OnComplete,
                         IEnumerable<KeyValuePair<TimeSpan, EventHandler>> triggers)
        {
            if (!IsAnimated || clipName == null)
                return false;

            // Play the animation clip with the specified name
            AnimationClip clip;

            if (animationClips.TryGetValue(clipName, out clip))
            {
                // Do nothing if it's still the same animation clip
                if (clip == currentClip)
                    return true;

                // No blend occurs if there's no blend duration
                if (blendTime > 0)
                {
                    // Start the new animation clip on the other player
                    currentPlayer = 1 - currentPlayer;
                    blending = true;
                    blendStart = game.CurrentGameTime.TotalGameTime.TotalSeconds;
                    blendDuration = blendTime;
                }

                Player.StartClip(clip);
                Player.Loop = loop;
                Player.Complete = OnComplete;
                Player.Triggers = triggers;
                animationState = AnimationState.Playing;
                currentClip = clip;
                return true;
            }

            // Stop if the clip is invalid
            Stop();
            return false;
        }

        /// <summary>
        /// Pause the current animation
        /// </summary>
        public void Pause()
        {
            if (animationState == AnimationState.Playing)
                animationState = AnimationState.Paused;
        }

        /// <summary>
        /// Stop the current animation (Reset current animation to the first frame).
        /// </summary>
        public void Stop()
        {
            if (IsAnimated)
            {
                // Reset current animation
                players[currentPlayer].Update(TimeSpan.Zero, false, Transform);

                // Change animation state
                animationState = AnimationState.Stopped;

                // Stop blending for whatever reason
                blending = false;
            }
        }
        #endregion

        #region Update & Draw
        public virtual void Update(GameTime gameTime)
        {
            if (!IsAnimated)
                return;

            // Apply animation speed
            TimeSpan time = (animationState != AnimationState.Playing) ? TimeSpan.Zero :
                new TimeSpan((long)(gameTime.ElapsedGameTime.Ticks * animationSpeed));

            players[currentPlayer].Update(time, true, Transform);

            // update both players when we are blending animations
            if (blending)
                players[1 - currentPlayer].Update(time, true, Transform);

            if (IsSkinned)
                UpdateSkinTransform(gameTime);
            else
                UpdateBoneTransform(gameTime);
        }
        
        private void UpdateBoneTransform(GameTime gameTime)
        {
            // Update skin transforms (stored in bones)
            bones = players[currentPlayer].GetWorldTransforms();

            // Lerp transforms when we are blending between animations
            if (blending)
            {
                // End blend if time exceeds
                double timeNow = gameTime.TotalGameTime.TotalSeconds;

                if (timeNow - blendStart > blendDuration)
                    blending = false;

                // Compute lerp amount
                float amount = (float)((timeNow - blendStart) / blendDuration);

                // Clamp lerp amount to [0..1]
                amount = MathHelper.Clamp(amount, 0.0f, 1.0f);

                // Get old transforms
                Matrix[] prevBoneTransforms = players[1 - currentPlayer].GetWorldTransforms();

                // Perform matrix lerp on all skin transforms
                for (int i = 0; i < bones.Length; i++)
                    bones[i] = Matrix.Lerp(
                        prevBoneTransforms[i], bones[i], amount);
            }
        }

        private void UpdateSkinTransform(GameTime gameTime)
        {
            // Update skin transforms (stored in bones)
            bones = players[currentPlayer].GetWorldTransforms();
            skinTransforms = players[currentPlayer].GetSkinTransforms();

            // Lerp transforms when we are blending between animations
            if (blending)
            {
                // End blend if time exceeds
                double timeNow = gameTime.TotalGameTime.TotalSeconds;

                if (timeNow - blendStart > blendDuration)
                    blending = false;

                // Compute lerp amount
                float amount = (float)((timeNow - blendStart) / blendDuration);

                // Clamp lerp amount to [0..1]
                amount = MathHelper.Clamp(amount, 0.0f, 1.0f);

                // Get old transforms
                Matrix[] prevWorldTransforms = players[1 - currentPlayer].GetWorldTransforms();
                Matrix[] prevSkinTransforms = players[1 - currentPlayer].GetSkinTransforms();

                // Perform matrix lerp on all skin transforms
                for (int i = 0; i < bones.Length; i++)
                {
                    bones[i] = Matrix.Lerp(
                        prevWorldTransforms[i], bones[i], amount);
                    skinTransforms[i] = Matrix.Lerp(
                        prevSkinTransforms[i], skinTransforms[i], amount);
                }
            }
        }

        public virtual void Draw(GameTime gameTime)
        {
            if (model == null || meshParts.Count <= 0)
                return;

            // Draw all mesh parts
            for (int i = 0; i < meshParts.Count; i++)
            {
                ModelMeshPart part = meshParts[i];
                ModelMesh mesh = part.Tag as ModelMesh;

                if (mesh == null)
                    throw new Exception("ModelMesh not attached to ModelMeshPart");

                // Update renderable if the material has changed
                if (materials[i].IsDirty || renderables[i] == null)
                {
                    materials[i].IsDirty = false;

                    renderables[i] =    // Refresh renderable
                        game.ModelManager.GetRenderable(mesh, part, materials[i]);
                }

                // Add a new instance to the renderable

                if (IsSkinned)
                    renderables[i].Add(skinTransforms, tint, glow);
                else
                    renderables[i].Add(bones[mesh.ParentBone.Index], tint, glow);
            }
        }

        /// <summary>
        /// Draw the game model onto a shadow map
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="shadow"></param>
        public void DrawShadowMap(GameTime gameTime, ShadowEffect shadow)
        {
            if (model == null || meshParts.Count <= 0)
                return;

            // Draw all mesh parts
            for (int i = 0; i < meshParts.Count; i++)
            {
                ModelMeshPart part = meshParts[i];
                ModelMesh mesh = part.Tag as ModelMesh;

                if (mesh == null)
                    throw new Exception("ModelMesh not attached to ModelMeshPart");

                // Don't care about materials.
                // All game models use the same shadow map effect.
                if (shadowMapRenderables[i] == null)
                {
                    // First time initialize
                    if (IsSkinned)
                        shadowMapRenderables[i] =
                            game.ModelManager.GetRenderable(mesh, part, Material.ShadowMapSkinned);
                    else
                        shadowMapRenderables[i] =
                            game.ModelManager.GetRenderable(mesh, part, Material.ShadowMap);
                }

                // Add a new instance to the renderable
                if (IsSkinned)
                    shadowMapRenderables[i].Add(skinTransforms, tint, glow);
                else
                    shadowMapRenderables[i].Add(bones[mesh.ParentBone.Index], tint, glow);
            }
        }
        #endregion

        #region BoundingBox
        /// <summary>
        /// Compute bounding box for the specified xna model.
        /// </summary>
        protected static BoundingBox OBBFromModel(Model model)
        {
            if (null == model || model.Meshes.Count <= 0)
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
        #endregion
    }
    #endregion

    #region AnimationPlayer
    /// <summary>
    /// The animation player is in charge of decoding bone position
    /// matrices from an animation clip.
    /// </summary>
    public class AnimationPlayer
    {
        #region Fields

        Model model;

        // Information about the currently playing animation clip.
        AnimationClip currentClipValue;
        TimeSpan currentTimeValue;
        int currentKeyframe;


        // Current animation transform matrices.
        Matrix[] boneTransforms;
        Matrix[] worldTransforms;
        Matrix[] skinTransforms;


        // Backlink to the bind pose and skeleton hierarchy data.
        SkinningData skinningDataValue;

        public IEnumerable<KeyValuePair<TimeSpan, EventHandler>> Triggers;
        public EventHandler Complete;
        public bool Loop;

        #endregion


        /// <summary>
        /// Constructs a new animation player.
        /// </summary>
        public AnimationPlayer(SkinningData skinningData)
        {
            if (skinningData == null)
                throw new ArgumentNullException();

            skinningDataValue = skinningData;

            boneTransforms = new Matrix[skinningData.BindPose.Count];
            worldTransforms = new Matrix[skinningData.BindPose.Count];
            skinTransforms = new Matrix[skinningData.BindPose.Count];
        }

        public AnimationPlayer(Model model)
        {
            if (model == null)
                throw new ArgumentNullException();

            this.model = model;
            this.boneTransforms = new Matrix[model.Bones.Count];
            this.worldTransforms = new Matrix[model.Bones.Count];
        }


        /// <summary>
        /// Starts decoding the specified animation clip.
        /// </summary>
        public void StartClip(AnimationClip clip)
        {
            if (clip == null)
                throw new ArgumentNullException("clip");

            currentClipValue = clip;
            currentTimeValue = TimeSpan.Zero;
            currentKeyframe = 0;

            // Initialize bone transforms to the bind pose.
            if (skinningDataValue != null)
                skinningDataValue.BindPose.CopyTo(boneTransforms, 0);
            else if (model != null)
                model.CopyBoneTransformsTo(boneTransforms);
        }

        /// <summary>
        /// Advances the current animation position.
        /// </summary>
        public void Update(TimeSpan time, bool relativeToCurrentTime, Matrix rootTransform)
        {
            UpdateBoneTransforms(time, relativeToCurrentTime);
            UpdateWorldTransforms(rootTransform);

            if (skinningDataValue != null)
                UpdateSkinTransforms();
        }


        /// <summary>
        /// Helper used by the Update method to refresh the BoneTransforms data.
        /// </summary>
        public void UpdateBoneTransforms(TimeSpan time, bool relativeToCurrentTime)
        {
            if (currentClipValue == null)
                throw new InvalidOperationException(
                            "AnimationPlayer.Update was called before StartClip");

            if (currentTimeValue >= currentClipValue.Duration && !Loop)
                return;
            
            // Update triggers
            if (Triggers != null)
            {
                foreach (KeyValuePair<TimeSpan, EventHandler> trigger in Triggers)
                {
                    if (currentTimeValue < trigger.Key &&
                        currentTimeValue + time > trigger.Key)
                    {
                        trigger.Value(this, null);
                    }
                }
            }

            // Update the animation position.
            if (relativeToCurrentTime)
            {
                time += currentTimeValue;

                // If we reached the end, loop back to the start.
                while (time >= currentClipValue.Duration)
                {
                    // Trigger complete event
                    if (Complete != null)
                    {
                        Complete(null, EventArgs.Empty);
                    }

                    if (Loop)
                    {
                        time -= currentClipValue.Duration;
                    }
                    else
                    {
                        currentTimeValue = currentClipValue.Duration;
                        time = currentTimeValue;
                        break;
                    }
                }
            }

            if ((time < TimeSpan.Zero) || (time > currentClipValue.Duration))
                throw new ArgumentOutOfRangeException("time");

            // If the position moved backwards, reset the keyframe index.
            if (time < currentTimeValue)
            {
                currentKeyframe = 0;
                if (skinningDataValue != null)
                    skinningDataValue.BindPose.CopyTo(boneTransforms, 0);
                else if (model != null)
                    model.CopyBoneTransformsTo(boneTransforms);
            }

            currentTimeValue = time;

            // Read keyframe matrices.
            IList<Keyframe> keyframes = currentClipValue.Keyframes;

            while (currentKeyframe < keyframes.Count)
            {
                Keyframe keyframe = keyframes[currentKeyframe];

                // Stop when we've read up to the current time position.
                if (keyframe.Time > currentTimeValue)
                    break;

                // Use this keyframe.
                boneTransforms[keyframe.Bone] = keyframe.Transform;

                currentKeyframe++;
            }
        }


        /// <summary>
        /// Helper used by the Update method to refresh the WorldTransforms data.
        /// </summary>
        public void UpdateWorldTransforms(Matrix rootTransform)
        {
            // Root bone.
            worldTransforms[0] = boneTransforms[0] * rootTransform;

            // Child bones.
            for (int bone = 1; bone < worldTransforms.Length; bone++)
            {
                int parentBone = -1;

                if (skinningDataValue != null)
                    parentBone = skinningDataValue.SkeletonHierarchy[bone];
                else if (model != null)
                    parentBone = model.Bones[bone].Parent.Index;

                worldTransforms[bone] = boneTransforms[bone] * worldTransforms[parentBone];
            }
        }


        /// <summary>
        /// Helper used by the Update method to refresh the SkinTransforms data.
        /// </summary>
        public void UpdateSkinTransforms()
        {
            for (int bone = 0; bone < skinTransforms.Length; bone++)
            {
                skinTransforms[bone] = skinningDataValue.InverseBindPose[bone] *
                                            worldTransforms[bone];
            }
        }


        /// <summary>
        /// Gets the current bone transform matrices, relative to their parent bones.
        /// </summary>
        public Matrix[] GetBoneTransforms()
        {
            return boneTransforms;
        }


        /// <summary>
        /// Gets the current bone transform matrices, in absolute format.
        /// </summary>
        public Matrix[] GetWorldTransforms()
        {
            return worldTransforms;
        }


        /// <summary>
        /// Gets the current bone transform matrices,
        /// relative to the skinning bind pose.
        /// </summary>
        public Matrix[] GetSkinTransforms()
        {
            return skinTransforms;
        }


        /// <summary>
        /// Gets the clip currently being decoded.
        /// </summary>
        public AnimationClip CurrentClip
        {
            get { return currentClipValue; }
        }


        /// <summary>
        /// Gets the current play position.
        /// </summary>
        public TimeSpan CurrentTime
        {
            get { return currentTimeValue; }
        }
    }
    #endregion

    #region TypeReaders
    /// <summary>
    /// Loads SkinningData objects from compiled XNB format.
    /// </summary>
    public class SkinningDataReader : ContentTypeReader<SkinningData>
    {
        protected override SkinningData Read(ContentReader input,
                                             SkinningData existingInstance)
        {
            IList<Matrix> bindPose, inverseBindPose;
            IList<int> skeletonHierarchy;
            IList<string> boneName;

            bindPose = input.ReadObject<IList<Matrix>>();
            inverseBindPose = input.ReadObject<IList<Matrix>>();
            skeletonHierarchy = input.ReadObject<IList<int>>();
            boneName = input.ReadObject<IList<string>>();

            return new SkinningData(bindPose, inverseBindPose,
                                    skeletonHierarchy, boneName);
        }
    }


    /// <summary>
    /// Loads AnimationClip objects from compiled XNB format.
    /// </summary>
    public class AnimationClipReader : ContentTypeReader<AnimationClip>
    {
        protected override AnimationClip Read(ContentReader input,
                                              AnimationClip existingInstance)
        {
            TimeSpan duration = input.ReadObject<TimeSpan>();
            IList<Keyframe> keyframes = input.ReadObject<IList<Keyframe>>();

            return new AnimationClip(duration, keyframes);
        }
    }


    /// <summary>
    /// Loads Keyframe objects from compiled XNB format.
    /// </summary>
    public class KeyframeReader : ContentTypeReader<Keyframe>
    {
        protected override Keyframe Read(ContentReader input,
                                         Keyframe existingInstance)
        {
            int bone = input.ReadObject<int>();
            TimeSpan time = input.ReadObject<TimeSpan>();
            Matrix transform = input.ReadObject<Matrix>();

            return new Keyframe(bone, time, transform);
        }
    }
    #endregion
}
