// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Isles.Pipeline
{
    /// <summary>
    /// Custom processor extends the builtin framework ModelProcessor class,
    /// adding animation support.
    /// </summary>
    [ContentProcessor]
    public class GameModelProcessor : ModelProcessor
    {
        // Maximum number of bone matrices we can render using shader 2.0
        // in a single pass. If you change this, update SkinnedModel.fx to match.
        private const int MaxBones = 59;

        /// <summary>
        /// The main Process method converts an intermediate format content pipeline
        /// NodeContent tree to a ModelContent object with embedded animation data.
        /// </summary>
        public override ModelContent Process(NodeContent input,
                                             ContentProcessorContext context)
        {
            ModelContent model;
            Dictionary<string, AnimationClip> animationClips;
            var dictionary = new Dictionary<string, object>();

            // Find the skeleton.
            BoneContent skeleton = MeshHelper.FindSkeleton(input);

            if (skeleton == null)
            {
                // Not a skinned mesh
                model = base.Process(input, context);
                animationClips = ProcessAnimations(input);
                if (animationClips != null && animationClips.Count > 0)
                {
                    dictionary.Add("AnimationData", animationClips);
                }

                AddSpacePartitionData(dictionary, model);
                model.Tag = dictionary;
                AddNormalTextureToTag(model);
                return model;
            }

            ValidateMesh(input, context, null);

            // We don't want to have to worry about different parts of the model being
            // in different local coordinate systems, so let's just bake everything.
            FlattenTransforms(input, skeleton);

            // Read the bind pose and skeleton hierarchy data.
            IList<BoneContent> bones = MeshHelper.FlattenSkeleton(skeleton);

            if (bones.Count > MaxBones)
            {
                throw new InvalidContentException(string.Format(
                    "Skeleton has {0} bones, but the maximum supported is {1}.",
                    bones.Count, MaxBones));
            }

            var bindPose = new List<Matrix>();
            var inverseBindPose = new List<Matrix>();
            var skeletonHierarchy = new List<int>();
            var boneNames = new List<string>();

            foreach (BoneContent bone in bones)
            {
                boneNames.Add(bone.Name);
                bindPose.Add(bone.Transform);
                inverseBindPose.Add(Matrix.Invert(bone.AbsoluteTransform));
                skeletonHierarchy.Add(bones.IndexOf(bone.Parent as BoneContent));
            }

            // Convert animation data to our runtime format.
            animationClips = ProcessAnimations(skeleton.Animations, BonesToNodes(bones));

            // Chain to the base ModelProcessor class so it can convert the model data.
            model = base.Process(input, context);

            // Store our custom animation data in the Tag property of the model.
            dictionary.Add("SkinningData", new SkinningData(bindPose, inverseBindPose,
                                                            skeletonHierarchy, boneNames));
            dictionary.Add("AnimationData", animationClips);
            AddSpacePartitionData(dictionary, model);
            model.Tag = dictionary;

            // Store normal texture
            AddNormalTextureToTag(model);

            return model;
        }

        private Dictionary<string, AnimationClip> ProcessAnimations(NodeContent input)
        {
            IList<NodeContent> bones = FlattenNodes(input);

            var animations = new Dictionary<string, AnimationClip>();

            ProcessAnimation(input, animations, bones);

            return animations;
        }

        private void ProcessAnimation(NodeContent input, Dictionary<string, AnimationClip> animations,
                                      IList<NodeContent> bones)
        {
            if (input != null)
            {
                if (input.Animations != null && input.Animations.Count > 0)
                {
                    Dictionary<string, AnimationClip> animSet = ProcessAnimations(input.Animations,
                                                                                  bones);
                    foreach (KeyValuePair<string, AnimationClip> entry in animSet)
                    {
                        if (animations.ContainsKey(entry.Key))
                        {
                            foreach (Keyframe frame in entry.Value.Keyframes)
                            {
                                animations[entry.Key].Keyframes.Add(frame);
                            }
(animations[entry.Key].Keyframes as List<Keyframe>).Sort(CompareKeyframeTimes);
                        }
                        else
                        {
                            animations.Add(entry.Key, entry.Value);
                        }
                    }
                }

                foreach (NodeContent child in input.Children)
                {
                    ProcessAnimation(child, animations, bones);
                }
            }
        }

        private IList<NodeContent> FlattenNodes(NodeContent input)
        {
            var nodes = new List<NodeContent>();

            FlattenNodes(input, nodes);

            return nodes;
        }

        private void FlattenNodes(NodeContent input, List<NodeContent> nodes)
        {
            if (input != null)
            {
                nodes.Add(input);

                foreach (NodeContent child in input.Children)
                {
                    FlattenNodes(child, nodes);
                }
            }
        }

        private IList<NodeContent> BonesToNodes(IList<BoneContent> bones)
        {
            var nodes = new List<NodeContent>(bones.Count);

            foreach (BoneContent bone in bones)
            {
                nodes.Add(bone);
            }

            return nodes;
        }

        private void AddSpacePartitionData(Dictionary<string, object> dictionary, ModelContent model)
        {
            Vector3 position;

            Matrix transform;
            var info = new ModelSpacePartitionInformation();

            info.SetBoundingBox(model);

            // Set the bit map
            foreach (ModelMeshContent mesh in model.Meshes)
            {
                transform = mesh.SourceMesh.AbsoluteTransform;

                foreach (GeometryContent geometry in mesh.SourceMesh.Geometry)
                {
                    foreach (Vector3 v in geometry.Vertices.Positions)
                    {
                        position = Vector3.Transform(v, transform);
                        info.Set(position);
                    }
                }
            }

            position.X = (info.Box.Max.X - info.Box.Min.X) / 16;
            position.Y = (info.Box.Max.Y - info.Box.Min.Y) / 16;
            position.Z = (info.Box.Max.Z - info.Box.Min.Z) / 16;
            for (var i = 2; i < 6; i++)
            {
                for (var j = 2; j < 6; j++)
                {
                    for (var k = 2; k < 6; k++)
                    {
                        info.Set(info.Box.Min +
                                  new Vector3(position.X * (2 * i + 1), position.Y * (2 * j + 1), position.Z * (2 * k + 1)));
                    }
                }
            }

            dictionary.Add("SpacePartition", info.BitMap);
        }

        private void AddNormalTextureToTag(ModelContent model)
        {
            foreach (ModelMeshContent mesh in model.Meshes)
            {
                foreach (ModelMeshPartContent part in mesh.MeshParts)
                {
                    part.Material.Textures.TryGetValue("NormalTexture", out ExternalReference<TextureContent> value);
                    part.Tag = value;
                }
            }
        }

        /// <summary>
        /// Changes all the materials to use our skinned model effect.
        /// </summary>
        protected override MaterialContent ConvertMaterial(MaterialContent material,
                                                        ContentProcessorContext context)
        {
            if (material is not BasicMaterialContent basicMaterial)
            {
                throw new InvalidContentException(string.Format(
                    "GameModelProcessor only supports BasicMaterialContent, " +
                    "but input mesh uses {0}.", material.GetType()));
            }

            // Compile the corresponding normal texture
            if (basicMaterial.Texture == null)
            {
                context.Logger.LogWarning(null, null, "No texture found");
                return basicMaterial;
            }

            var textureFilename = basicMaterial.Texture.Filename;
            var normalTextureFilename =
                Path.GetDirectoryName(textureFilename) + "\\" +
                Path.GetFileNameWithoutExtension(textureFilename) + "_n" +
                Path.GetExtension(textureFilename);

            // Checks if the normal texture exists
            if (File.Exists(normalTextureFilename))
            {
                var normalTexture =
                    new ExternalReference<Texture2DContent>(normalTextureFilename);

                // Store the normal map in the opaque data of the corresponding material
                basicMaterial.Textures.Add("NormalTexture",
                    context.BuildAsset<Texture2DContent, TextureContent>(
                        normalTexture, typeof(NormalMapTextureProcessor).Name));
            }
            else if (GenerateTangentFrames)
            {
                context.Logger.LogWarning(null, null,
                    "Missing normal texture: {0}", normalTextureFilename);
            }

            // Chain to the base ModelProcessor converter.
            return base.ConvertMaterial(basicMaterial, context);
        }

        /// <summary>
        /// Converts an intermediate format content pipeline AnimationContentDictionary
        /// object to our runtime AnimationClip format.
        /// </summary>
        private static Dictionary<string, AnimationClip> ProcessAnimations(
            AnimationContentDictionary animations, IList<NodeContent> bones)
        {
            // Build up a table mapping bone names to indices.
            var boneMap = new Dictionary<string, int>();

            for (var i = 0; i < bones.Count; i++)
            {
                var boneName = bones[i].Name;

                if (!string.IsNullOrEmpty(boneName))
                {
                    boneMap.Add(boneName, i);
                }
            }

            // Convert each animation in turn.
            Dictionary<string, AnimationClip> animationClips;
            animationClips = new Dictionary<string, AnimationClip>();

            foreach (KeyValuePair<string, AnimationContent> animation in animations)
            {
                AnimationClip processed = ProcessAnimation(animation.Value, boneMap);

                animationClips.Add(animation.Key, processed);
            }

            return animationClips.Count == 0
                ? throw new InvalidContentException(
                            "Input file does not contain any animations.")
                : animationClips;
        }

        /// <summary>
        /// Converts an intermediate format content pipeline AnimationContent
        /// object to our runtime AnimationClip format.
        /// </summary>
        private static AnimationClip ProcessAnimation(AnimationContent animation,
                                              Dictionary<string, int> boneMap)
        {
            var keyframes = new List<Keyframe>();

            // For each input animation channel.
            foreach (KeyValuePair<string, AnimationChannel> channel in
                animation.Channels)
            {
                // Look up what bone this channel is controlling.
                int boneIndex;

                if (!boneMap.TryGetValue(channel.Key, out boneIndex))
                {
                    // throw new InvalidContentException(string.Format(
                    //    "Found animation for bone '{0}', " +
                    //    "which is not part of the skeleton.", channel.Key));
                    continue;
                }

                // Convert the keyframe data.
                foreach (AnimationKeyframe keyframe in channel.Value)
                {
                    keyframes.Add(new Keyframe(boneIndex, keyframe.Time,
                                               keyframe.Transform));
                }
            }

            // Sort the merged keyframes by time.
            keyframes.Sort(CompareKeyframeTimes);

            if (keyframes.Count == 0)
            {
                throw new InvalidContentException("Animation has no keyframes.");
            }

            return animation.Duration <= TimeSpan.Zero
                ? throw new InvalidContentException("Animation has a zero duration.")
                : new AnimationClip(animation.Duration, keyframes);
        }

        /// <summary>
        /// Comparison function for sorting keyframes into ascending time order.
        /// </summary>
        private static int CompareKeyframeTimes(Keyframe a, Keyframe b)
        {
            return a.Time.CompareTo(b.Time);
        }

        /// <summary>
        /// Makes sure this mesh contains the kind of data we know how to animate.
        /// </summary>
        private static void ValidateMesh(NodeContent node, ContentProcessorContext context,
                                 string parentBoneName)
        {
            if (node is MeshContent mesh)
            {
                // Validate the mesh.
                if (parentBoneName != null)
                {
                    context.Logger.LogWarning(null, null,
                        "Mesh {0} is a child of bone {1}. SkinnedModelProcessor " +
                        "does not correctly handle meshes that are children of bones.",
                        mesh.Name, parentBoneName);
                }

                if (!MeshHasSkinning(mesh))
                {
                    // context.Logger.LogWarning(null, null,
                    //    "Mesh {0} has no skinning information, so it has been deleted.",
                    //    mesh.Name);

                    // mesh.Parent.Children.Remove(mesh);
                    return;
                }
            }
            else if (node is BoneContent)
            {
                // If this is a bone, remember that we are now looking inside it.
                parentBoneName = node.Name;
            }

            // Recurse (iterating over a copy of the child collection,
            // because validating children may delete some of them).
            foreach (NodeContent child in new List<NodeContent>(node.Children))
            {
                ValidateMesh(child, context, parentBoneName);
            }
        }

        /// <summary>
        /// Checks whether a mesh contains skininng information.
        /// </summary>
        private static bool MeshHasSkinning(MeshContent mesh)
        {
            foreach (GeometryContent geometry in mesh.Geometry)
            {
                if (!geometry.Vertices.Channels.Contains(VertexChannelNames.Weights()))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether a mesh is a skinned mesh.
        /// </summary>
        private static bool IsSkinned(NodeContent node)
        {
            if (node is MeshContent mesh && MeshHasSkinning(mesh))
            {
                return true;
            }

            // Recurse (iterating over a copy of the child collection,
            // because validating children may delete some of them).
            foreach (NodeContent child in new List<NodeContent>(node.Children))
            {
                if (IsSkinned(child))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Bakes unwanted transforms into the model geometry,
        /// so everything ends up in the same coordinate system.
        /// </summary>
        private static void FlattenTransforms(NodeContent node, BoneContent skeleton)
        {
            foreach (NodeContent child in node.Children)
            {
                // Don't process the skeleton, because that is special.
                if (child == skeleton)
                {
                    continue;
                }

                // Bake the local transform into the actual geometry.
                MeshHelper.TransformScene(child, child.Transform);

                // Having baked it, we can now set the local
                // coordinate system back to identity.
                child.Transform = Matrix.Identity;

                // Recurse.
                FlattenTransforms(child, skeleton);
            }
        }
    }

    public class ModelSpacePartitionInformation
    {
        public BoundingBox Box;

        // 585 = 1 + 8 * 8 + 8 * 8 * 8
        public bool[] BitMap = new bool[585];
        private static readonly int[] spliter = new int[5] { 0, 1, 9, 73, 585 };

        /// <summary>
        /// Test if that position is occupied.
        /// </summary>
        /// <param name="position"></param>
        public bool Occupied(Vector3 position)
        {
            var x = (int)((position.X - Box.Min.X) / ((Box.Max.X - Box.Min.X) / 8));
            var y = (int)((position.Y - Box.Min.Y) / ((Box.Max.Y - Box.Min.Y) / 8));
            var z = (int)((position.Z - Box.Min.Z) / ((Box.Max.Z - Box.Min.Z) / 8));
            if (x == 8)
            {
                x = 7;
            }

            if (y == 8)
            {
                y = 7;
            }

            if (z == 8)
            {
                z = 7;
            }

            var p = 0;
            var power = new int[3] { 1, 2, 4 };
            for (var l = 0; l < 4; l++)
            {
                if (!BitMap[p])
                {
                    return false;
                }

                if (l == 3)
                {
                    return true;
                }

                var pow = power[2 - l];
                p = SonOf(p, 4 * (x / pow) + 2 * (y / pow) + (z / pow), l);
                x %= pow;
                y %= pow;
                z %= pow;
            }

            return true;
        }

        public void SetBoundingBox(ModelContent model)
        {
            Box.Min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Box.Max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            Matrix transform;

            Vector3 position;
            // Calculate the size
            foreach (ModelMeshContent mesh in model.Meshes)
            {
                transform = mesh.SourceMesh.AbsoluteTransform;

                foreach (GeometryContent geometry in mesh.SourceMesh.Geometry)
                {
                    foreach (Vector3 v in geometry.Vertices.Positions)
                    {
                        position = Vector3.Transform(v, transform);
                        Box.Max.X = Math.Max(Box.Max.X, position.X);
                        Box.Max.Y = Math.Max(Box.Max.Y, position.Y);
                        Box.Max.Z = Math.Max(Box.Max.Z, position.Z);
                        Box.Min.X = Math.Min(Box.Min.X, position.X);
                        Box.Min.Y = Math.Min(Box.Min.Y, position.Y);
                        Box.Min.Z = Math.Min(Box.Min.Z, position.Z);
                    }
                }
            }
        }

        /// <summary>
        /// Set the position as Occupied.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="level"></param>
        public void Set(Vector3 position)
        {
            var x = (int)((position.X - Box.Min.X) / ((Box.Max.X - Box.Min.X) / 8));
            var y = (int)((position.Y - Box.Min.Y) / ((Box.Max.Y - Box.Min.Y) / 8));
            var z = (int)((position.Z - Box.Min.Z) / ((Box.Max.Z - Box.Min.Z) / 8));
            if (x == 8)
            {
                x = 7;
            }

            if (y == 8)
            {
                y = 7;
            }

            if (z == 8)
            {
                z = 7;
            }

            var p = 4 * (x / 4) + 2 * (y / 4) + (z / 4) + spliter[1];
            BitMap[0] = BitMap[p] = true;

            x %= 4;
            y %= 4;
            z %= 4;

            var tp = SonOf(p, 4 * (x / 2) + 2 * (y / 2) + (z / 2), 1);
            BitMap[tp] = true;

            x %= 2;
            y %= 2;
            z %= 2;
            BitMap[SonOf(tp, 4 * x + 2 * y + z, 2)] = true;
        }

        /// <summary>
        /// Find of position of the mth son of parent.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="m"></param>
        /// <param name="parLevel"></param>
        public int SonOf(int parent, int m, int parLevel)
        {
            return (parent - spliter[parLevel]) * 8 + spliter[parLevel + 1] + m;
        }

        public int ParentOf(int son, int level)
        {
            return spliter[level - 1] + (son - spliter[level]) / 8;
        }

        public BoundingBox IndexToBoundingBox(int index)
        {
            var rtv = new BoundingBox(Box.Min, Box.Max);
            if (index == 0)
            {
                return rtv;
            }

            bool x, y, z;
            int level;
            for (level = 0; level < 4; level++)
            {
                if (index < spliter[level + 1])
                {
                    break;
                }
            }

            var parents = new int[level + 1];
            var bias = new int[level + 1];
            bias[0] = 0;
            parents[level] = index;

            // Calculate parents in the tree, and their bias
            for (var i = level; i > 0; i--)
            {
                bias[i] = (parents[i] - spliter[i]) % 8;
                parents[i - 1] = ParentOf(parents[i], i);
            }

            for (var i = 1; i <= level; i++)
            {
                x = bias[i] > 3;
                y = bias[i] % 4 > 1;
                z = bias[i] % 2 == 1;
                if (x)
                {
                    rtv.Min.X = (rtv.Max.X + rtv.Min.X) / 2;
                }
                else
                {
                    rtv.Max.X = (rtv.Max.X + rtv.Min.X) / 2;
                }

                if (y)
                {
                    rtv.Min.Y = (rtv.Max.Y + rtv.Min.Y) / 2;
                }
                else
                {
                    rtv.Max.Y = (rtv.Max.Y + rtv.Min.Y) / 2;
                }

                if (z)
                {
                    rtv.Min.Z = (rtv.Max.Z + rtv.Min.Z) / 2;
                }
                else
                {
                    rtv.Max.Z = (rtv.Max.Z + rtv.Min.Z) / 2;
                }
            }

            return rtv;
        }
    }

    /// <summary>
    /// The NormalMapTextureProcessor takes in an encoded normal map, and outputs
    /// a texture in the NormalizedByte4 format.  Every pixel in the source texture
    /// is remapped so that values ranging from 0 to 1 will range from -1 to 1.
    /// </summary>
    [ContentProcessor]
    internal class NormalMapTextureProcessor : ContentProcessor<TextureContent, TextureContent>
    {
        /// <summary>
        /// Process converts the encoded normals to the NormalizedByte4 format and
        /// generates mipmaps.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        public override TextureContent Process(TextureContent input,
            ContentProcessorContext context)
        {
            // convert to vector4 format, so that we know what kind of data we're
            // working with.
            input.ConvertBitmapType(typeof(PixelBitmapContent<Vector4>));

            // expand the encoded normals; values ranging from 0 to 1 should be
            // expanded to range to -1 to 1.
            // NOTE: in almost all cases, the input normalmap will be a
            // Texture2DContent, and will only have one face.  just to be safe,
            // we'll do the conversion for every face in the texture.
            foreach (MipmapChain mipmapChain in input.Faces)
            {
                foreach (PixelBitmapContent<Vector4> bitmap in mipmapChain)
                {
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        for (var y = 0; y < bitmap.Height; y++)
                        {
                            Vector4 encoded = bitmap.GetPixel(x, y);
                            bitmap.SetPixel(x, y, 2 * encoded - Vector4.One);
                        }
                    }
                }
            }

            // now that the conversion to -1 to 1 ranges is finished, convert to the
            // runtime-ready format NormalizedByte4.
            // EDUCATIONAL: it is possible to perform the conversion to NormalizedByte4
            // in the inner loop above by copying to a new TextureContent.  For
            // the sake of simplicity, we do it the slower way.
            input.ConvertBitmapType(typeof(PixelBitmapContent<NormalizedByte4>));

            input.GenerateMipmaps(false);
            return input;
        }
    }

    /// <summary>
    /// An animation clip is the runtime equivalent of the
    /// Microsoft.Xna.Framework.Content.Pipeline.Graphics.AnimationContent type.
    /// It holds all the keyframes needed to describe a single animation.
    /// </summary>
    public class AnimationClip
    {
        /// <summary>
        /// Constructs a new animation clip object.
        /// </summary>
        public AnimationClip(TimeSpan duration, IList<Keyframe> keyframes)
        {
            durationValue = duration;
            Keyframes = keyframes;
        }

        /// <summary>
        /// Gets the total length of the animation.
        /// </summary>
        public TimeSpan Duration => durationValue;

        private TimeSpan durationValue;

        /// <summary>
        /// Gets a combined list containing all the keyframes for all bones,
        /// sorted by time.
        /// </summary>
        public IList<Keyframe> Keyframes { get; }
    }

    /// <summary>
    /// Describes the position of a single bone at a single point in time.
    /// </summary>
    public class Keyframe
    {
        private TimeSpan timeValue;
        private Matrix transformValue;

        /// <summary>
        /// Constructs a new keyframe object.
        /// </summary>
        public Keyframe(int bone, TimeSpan time, Matrix transform)
        {
            Bone = bone;
            timeValue = time;
            transformValue = transform;
        }

        /// <summary>
        /// Gets the index of the target bone that is animated by this keyframe.
        /// </summary>
        public int Bone { get; }

        /// <summary>
        /// Gets the time offset from the start of the animation to this keyframe.
        /// </summary>
        public TimeSpan Time => timeValue;

        /// <summary>
        /// Gets the bone transform for this keyframe.
        /// </summary>
        public Matrix Transform => transformValue;
    }

    /// <summary>
    /// Combines all the data needed to render and animate a skinned object.
    /// This is typically stored in the Tag property of the Model being animated.
    /// </summary>
    public class SkinningData
    {

        /// <summary>
        /// Constructs a new skinning data object.
        /// </summary>
        public SkinningData(IList<Matrix> bindPose, IList<Matrix> inverseBindPose,
                            IList<int> skeletonHierarchy, IList<string> boneName)
        {
            BindPose = bindPose;
            InverseBindPose = inverseBindPose;
            SkeletonHierarchy = skeletonHierarchy;
            BoneName = boneName;
        }

        /// <summary>
        /// Bindpose matrices for each bone in the skeleton,
        /// relative to the parent bone.
        /// </summary>
        public IList<Matrix> BindPose { get; }

        /// <summary>
        /// Vertex to bonespace transforms for each bone in the skeleton.
        /// </summary>
        public IList<Matrix> InverseBindPose { get; }

        /// <summary>
        /// For each bone in the skeleton, stores the index of the parent bone.
        /// </summary>
        public IList<int> SkeletonHierarchy { get; }

        /// <summary>
        /// Gets the name for each bone.
        /// </summary>
        public IList<string> BoneName { get; }
    }

    /// <summary>
    /// Writes ModelAnimation objects into compiled XNB format.
    /// </summary>
    [ContentTypeWriter]
    public class SkinningDataWriter : ContentTypeWriter<SkinningData>
    {
        protected override void Write(ContentWriter output, SkinningData value)
        {
            output.WriteObject(value.BindPose);
            output.WriteObject(value.InverseBindPose);
            output.WriteObject(value.SkeletonHierarchy);
            output.WriteObject(value.BoneName);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "Isles.Graphics.SkinningDataReader, " +
                   "Isles, Version=1.0.0.0, Culture=neutral";
        }
    }

    /// <summary>
    /// Writes AnimationClip objects into compiled XNB format.
    /// </summary>
    [ContentTypeWriter]
    public class AnimationClipWriter : ContentTypeWriter<AnimationClip>
    {
        protected override void Write(ContentWriter output, AnimationClip value)
        {
            output.WriteObject(value.Duration);
            output.WriteObject(value.Keyframes);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "Isles.Graphics.AnimationClipReader, " +
                   "Isles, Version=1.0.0.0, Culture=neutral";
        }
    }

    /// <summary>
    /// Writes Keyframe objects into compiled XNB format.
    /// </summary>
    [ContentTypeWriter]
    public class KeyframeWriter : ContentTypeWriter<Keyframe>
    {
        protected override void Write(ContentWriter output, Keyframe value)
        {
            output.WriteObject(value.Bone);
            output.WriteObject(value.Time);
            output.WriteObject(value.Transform);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "Isles.Graphics.KeyframeReader, " +
                   "Isles, Version=1.0.0.0, Culture=neutral";
        }
    }
}
