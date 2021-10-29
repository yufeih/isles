// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Isles.Engine;
using Isles.Pipeline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class GameModel
    {
        private readonly BaseGame game = BaseGame.Singleton;

        /// <summary>
        /// Gets or sets model world transform.
        /// </summary>
        public Matrix Transform
        {
            get => transform;

            set
            {
                transform = value;
                isBoundingBoxDirty = true;
            }
        }

        private Matrix transform = Matrix.Identity;

        private Matrix[] bones;
        private Matrix[] skinTransforms;

        /// <summary>
        /// Gets the axis aligned bounding box of this model.
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
        /// Model axis aligned bounding box.
        /// </summary>
        private BoundingBox boundingBox;

        /// <summary>
        /// Bounding box of the xna model. (Not always axis aligned).
        /// </summary>
        private BoundingBox orientedBoundingBox;

        /// <summary>
        /// Whether we should refresh our axis aligned bounding box.
        /// </summary>
        private bool isBoundingBoxDirty = true;

        /// <summary>
        /// Gets whether a model contains any animation.
        /// </summary>
        private bool IsAnimated => Player != null;

        /// <summary>
        /// Gets whether the model is a skinned model.
        /// </summary>
        private bool IsSkinned => skin != null;

        /// <summary>
        /// Skinned mesh animation player.
        /// We use 2 players to blend between animations.
        /// </summary>
        private AnimationPlayer[] players = new AnimationPlayer[2];

        /// <summary>
        /// Gets the animation player for this game model.
        /// </summary>
        public AnimationPlayer Player => players[currentPlayer];

        private IDictionary<string, AnimationClip> animationClips;

        /// <summary>
        /// Points to the primary player.
        /// </summary>
        private int currentPlayer;

        /// <summary>
        /// Whether we are blending between two animations.
        /// </summary>
        private bool blending;

        /// <summary>
        /// Time value for animation blending.
        /// </summary>
        private double blendStart;

        /// <summary>
        /// Time value for animation blending.
        /// </summary>
        private double blendDuration;

        /// <summary>
        /// Current animation clip being played.
        /// </summary>
        private string currentClip;

        /// <summary>
        /// Represent the state of current animation.
        /// </summary>
        private enum AnimationState
        {
            Stopped,
            Playing,
            Paused,
        }

        /// <summary>
        /// Current animation state.
        /// </summary>
        private AnimationState animationState = AnimationState.Stopped;

        /// <summary>
        /// For skinned mesh.
        /// </summary>
        private SkinningData skin;

        /// <summary>
        /// Space partition information for this model.
        /// </summary>
        private ModelSpacePartitionInformation spacePartitionInfo;

        private Model model;
        private GltfModel gltfModel;

        public Vector3 Tint { get; set; } = new(1, 1, 1);

        public Vector3 Glow { get; set; }

        public float Alpha { get; set; } = 1;

        private GameModel()
        {
        }

        public GameModel(string modelName)
        {
            // Make sure this is the upper case Model
            model = game.Content.Load<Model>(modelName);
            gltfModel = game.ModelLoader.LoadModel($"data/{modelName}.gltf");
            Init();
        }

        /// <summary>
        /// Creates a shadow copy of this game model.
        /// The new model refers to the same xna model. Transformation, animation data are copied.
        /// </summary>
        public GameModel ShadowCopy()
        {
            var copy = new GameModel
            {
                animationClips = animationClips,
                animationState = animationState,
                blendDuration = blendDuration,
                blending = blending,
                blendStart = blendStart,
                boundingBox = boundingBox,
                currentClip = currentClip,
                currentPlayer = currentPlayer,
                Tint = Tint,
                Glow = Glow,
                Alpha = Alpha,
                isBoundingBoxDirty = isBoundingBoxDirty,
                model = model,
                gltfModel = gltfModel,
                orientedBoundingBox = orientedBoundingBox,
                players = players,
                skin = skin,
                spacePartitionInfo = spacePartitionInfo,
                transform = transform,
            };

            if (skinTransforms != null)
            {
                copy.skinTransforms = new Matrix[skinTransforms.Length];
                skinTransforms.CopyTo(copy.skinTransforms, 0);
            }

            copy.bones = new Matrix[bones.Length];
            bones.CopyTo(copy.bones, 0);

            return copy;
        }

        private void Init()
        {
            if (gltfModel.Meshes[0].Joints != null)
            {
                skinTransforms = new Matrix[gltfModel.Meshes[0].Joints.Length];
            }

            if (model.Tag is Dictionary<string, object> dictionary)
            {
                if (dictionary.TryGetValue("SkinningData", out var value))
                {
                    skin = value as SkinningData;
                }

                if (dictionary.TryGetValue("AnimationData", out value))
                {
                    animationClips = value as IDictionary<string, AnimationClip>;
                }

                dictionary.TryGetValue("SpacePartition", out value);

                spacePartitionInfo = new ModelSpacePartitionInformation
                {
                    BitMap = value as bool[],
                    Box = OBBFromModel(model),
                };
            }

            if (animationClips != null)
            {
                if (IsSkinned)
                {
                    players[0] = new AnimationPlayer(gltfModel);
                    players[1] = new AnimationPlayer(gltfModel);
                }

                UpdateBoneTransform(new GameTime());

                // Play the first animation clip
                Player.Loop = true;
                Play();
            }

            // Adjust bone array size
            bones = new Matrix[gltfModel.Nodes.Length];
            model.CopyAbsoluteBoneTransformsTo(bones);

            // Compute model bounding box.
            orientedBoundingBox = OBBFromModel(model);
        }

        /// <summary>
        /// Gets the index of a bone with the specific name.
        /// </summary>
        /// <returns>
        /// Negtive if the bone not found.
        /// </returns>
        public int GetBone(string boneName)
        {
            return gltfModel.NodeNames.TryGetValue(boneName, out var node) ? node.Index : -1;
        }

        /// <summary>
        /// Gets the global transformation of the bone.
        /// NOTE: Call this after the model gets drawed.
        /// </summary>
        public Matrix GetBoneTransform(int bone)
        {
            return bones[bone] * transform;
        }

        /// <summary>
        /// Ray intersection test.
        /// </summary>
        /// <param name="ray">Target ray.</param>
        /// <returns>
        /// Distance from the intersection point to the ray starting position,
        /// Null if there's no intersection.
        /// </returns>
        public float? Intersects(Ray ray)
        {
            if (spacePartitionInfo == null)
            {
                return BoundingBox.Intersects(ray);
            }

            var m = Matrix.Invert(transform);
            var relarayEnd = Vector3.Transform(ray.Position + ray.Direction, m);
            var relarayPosition = Vector3.Transform(ray.Position, m);
            var relaray = new Ray(relarayPosition, relarayEnd - relarayPosition);
            return Intersects(relaray, 0, 0);
        }

        private float? Intersects(Ray ray, int level, int index)
        {
            BoundingBox box;
            float? dist = null;
            if (level == 3)
            {
                if (spacePartitionInfo.BitMap[index])
                {
                    box = spacePartitionInfo.IndexToBoundingBox(index);
                    dist = ray.Intersects(box);
                }

                return dist;
            }

            var minLength = float.PositiveInfinity;
            if (!spacePartitionInfo.BitMap[index])
            {
                dist = ray.Intersects(spacePartitionInfo.IndexToBoundingBox(index));
                if (!dist.HasValue)
                {
                    return dist;
                }
            }

            for (var i = 0; i < 8; i++)
            {
                var son = spacePartitionInfo.SonOf(index, i, level);
                if (spacePartitionInfo.BitMap[son])
                {
                    dist = ray.Intersects(spacePartitionInfo.IndexToBoundingBox(son));
                    if (dist.HasValue)
                    {
                        var d = Intersects(ray, level + 1, son);
                        if (d.HasValue)
                        {
                            minLength = Math.Min(d.Value, minLength);
                        }
                    }
                }
            }

            dist = minLength != float.PositiveInfinity ? minLength : (float?)null;
            return dist;
        }

        /// <summary>
        /// Gets the animation clip with the specified name.
        /// </summary>
        public AnimationClip GetAnimationClip(string clipName)
        {
            return animationClips.TryGetValue(clipName, out AnimationClip value) ? value : null;
        }

        /// <summary>
        /// Play the current (or default) animation.
        /// </summary>
        /// <returns>Succeeded or not.</returns>
        private bool Play()
        {
            if (!IsAnimated)
            {
                return false;
            }

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

        public bool Play(string clip)
        {
            if (!IsAnimated || clip == null)
            {
                return false;
            }

            // Do nothing if it's still the same animation clip
            if (clip == currentClip)
            {
                return true;
            }

            currentClip = clip;
            Player.Triggers = null;
            Player.Loop = true;
            Player.Complete = null;
            Player.StartClip(clip);
            animationState = AnimationState.Playing;
            return true;
        }

        public bool Play(string clipName, bool loop, float blendTime)
        {
            return Play(clipName, loop, blendTime, null, null);
        }

        public bool Play(string clip, bool loop, float blendTime, EventHandler OnComplete,
                         IEnumerable<KeyValuePair<TimeSpan, EventHandler>> triggers)
        {
            if (!IsAnimated || clip == null)
            {
                return false;
            }

            // Do nothing if it's still the same animation clip
            if (clip == currentClip)
            {
                return true;
            }

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

        public void Pause()
        {
            if (animationState == AnimationState.Playing)
            {
                animationState = AnimationState.Paused;
            }
        }

        public virtual void Update(GameTime gameTime)
        {
            if (!IsAnimated)
            {
                return;
            }

            // Apply animation speed
            TimeSpan time = (animationState != AnimationState.Playing) ? TimeSpan.Zero : gameTime.ElapsedGameTime;

            players[currentPlayer].Update(time, true);

            // update both players when we are blending animations
            if (blending)
            {
                players[1 - currentPlayer].Update(time, true);
            }

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
                var timeNow = gameTime.TotalGameTime.TotalSeconds;

                if (timeNow - blendStart > blendDuration)
                {
                    blending = false;
                }

                // Compute lerp amount
                var amount = (float)((timeNow - blendStart) / blendDuration);

                // Clamp lerp amount to [0..1]
                amount = MathHelper.Clamp(amount, 0.0f, 1.0f);

                // Get old transforms
                Matrix[] prevBoneTransforms = players[1 - currentPlayer].GetWorldTransforms();

                // Perform matrix lerp on all skin transforms
                for (var i = 0; i < bones.Length; i++)
                {
                    bones[i] = Matrix.Lerp(prevBoneTransforms[i], bones[i], amount);
                }
            }

            if (skin != null)
            {
                var mesh = gltfModel.Meshes[0];
                for (var i = 0; i < mesh.Joints.Length; i++)
                {
                    skinTransforms[i] = mesh.InverseBindMatrices[i] * bones[mesh.Joints[i].Index];
                }
            }
        }

        public void Draw()
        {
            var tint = new Vector4(Tint, MathHelper.Clamp(Alpha, 0, 1));
            var glow = new Vector4(Glow, 1);

            foreach (var mesh in gltfModel.Meshes)
            {
                if (IsSkinned)
                {
                    game.ModelRenderer.Draw(mesh, transform, skinTransforms, tint, glow);
                }
                else
                {
                    game.ModelRenderer.Draw(mesh, bones[mesh.Node.Index] * transform, null, tint, glow);
                }
            }
        }

        /// <summary>
        /// Compute bounding box for the specified xna model.
        /// </summary>
        protected static BoundingBox OBBFromModel(Model model)
        {
            if (null == model || model.Meshes.Count <= 0)
            {
                return new BoundingBox(Vector3.Zero, Vector3.Zero);
            }

            const float FloatMax = 1000000;

            // Compute bounding box
            var min = new Vector3(FloatMax, FloatMax, FloatMax);
            var max = new Vector3(-FloatMax, -FloatMax, -FloatMax);

            var bones = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(bones);

            foreach (ModelMesh mesh in model.Meshes)
            {
                var stride = mesh.MeshParts[0].VertexStride;
                var elementCount = mesh.VertexBuffer.SizeInBytes / stride;
                var vertices = new Vector3[elementCount];
                mesh.VertexBuffer.GetData(0, vertices, 0, elementCount, stride);

                foreach (Vector3 vertex in vertices)
                {
                    // Transform vertex
                    var v = Vector3.Transform(vertex, bones[mesh.ParentBone.Index]);

                    if (v.X < min.X)
                    {
                        min.X = v.X;
                    }

                    if (v.X > max.X)
                    {
                        max.X = v.X;
                    }

                    if (v.Y < min.Y)
                    {
                        min.Y = v.Y;
                    }

                    if (v.Y > max.Y)
                    {
                        max.Y = v.Y;
                    }

                    if (v.Z < min.Z)
                    {
                        min.Z = v.Z;
                    }

                    if (v.Z > max.Z)
                    {
                        max.Z = v.Z;
                    }
                }
            }

            return new BoundingBox(min, max);
        }

        /// <summary>
        /// Compute the axis aligned bounding box from an oriented bounding box.
        /// </summary>
        private static BoundingBox AABBFromOBB(BoundingBox box, Matrix transform)
        {
            const float FloatMax = 1000000;

            // Find the 8 corners
            Vector3[] corners = box.GetCorners();

            // Compute bounding box
            var min = new Vector3(FloatMax, FloatMax, FloatMax);
            var max = new Vector3(-FloatMax, -FloatMax, -FloatMax);

            foreach (Vector3 c in corners)
            {
                var v = Vector3.Transform(c, transform);

                if (v.X < min.X)
                {
                    min.X = v.X;
                }

                if (v.X > max.X)
                {
                    max.X = v.X;
                }

                if (v.Y < min.Y)
                {
                    min.Y = v.Y;
                }

                if (v.Y > max.Y)
                {
                    max.Y = v.Y;
                }

                if (v.Z < min.Z)
                {
                    min.Z = v.Z;
                }

                if (v.Z > max.Z)
                {
                    max.Z = v.Z;
                }
            }

            return new BoundingBox(min, max);
        }
    }

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
}
