#region File Description
//-----------------------------------------------------------------------------
// SkinnedModelProcessor.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Isles.Pipeline
{
    #region GameModelProcessor
    /// <summary>
    /// Custom processor extends the builtin framework ModelProcessor class,
    /// adding animation support.
    /// </summary>
    [ContentProcessor]
    public class GameModelProcessor : ModelProcessor
    {
        // Maximum number of bone matrices we can render using shader 2.0
        // in a single pass. If you change this, update SkinnedModel.fx to match.
        const int MaxBones = 59;


        /// <summary>
        /// The main Process method converts an intermediate format content pipeline
        /// NodeContent tree to a ModelContent object with embedded animation data.
        /// </summary>
        public override ModelContent Process(NodeContent input,
                                             ContentProcessorContext context)
        {
            ModelContent model;
            Dictionary<string, AnimationClip> animationClips;
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            // Find the skeleton.
            BoneContent skeleton = MeshHelper.FindSkeleton(input);

            //System.Diagnostics.Debugger.Launch();

            if (skeleton == null)
            {
                // Not a skinned mesh
                model = base.Process(input, context);
                animationClips = ProcessAnimations(input);
                if (animationClips != null && animationClips.Count > 0)
                    dictionary.Add("AnimationData", animationClips);
                AddSpacePartitionData(dictionary, model);
                model.Tag = dictionary;
                AddNormalTextureToTag(model);
                WriteGLTF(input, model, context);
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

            List<Matrix> bindPose = new List<Matrix>();
            List<Matrix> inverseBindPose = new List<Matrix>();
            List<int> skeletonHierarchy = new List<int>();
            List<string> boneNames = new List<string>();

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

            WriteGLTF(input, model, context);

            return model;
        }

        private static void WriteGLTF(NodeContent input, ModelContent model, ContentProcessorContext context)
        {

            Directory.CreateDirectory("D:/isles/gltf/");
            var baseName = Path.Combine("D:/isles/gltf/", Path.GetFileNameWithoutExtension(input.Identity.SourceFilename).ToLowerInvariant());
            var binName = baseName + ".bin";
            var meshes = new List<object>();
            var accessors = new List<object>();
            var bufferViews = new List<object>();
            var bytes = new List<byte>();
            var materials = new List<object>();
            var images = new List<object>();
            var textures = new List<object>();
            var nodes = new List<object>();
            var sceneNodes = new List<int>();

            foreach (var modelMesh in model.Meshes)
            {
                var imageDict = new Dictionary<string, string>();
                var primitives = new List<object>();
                var byteBase = bytes.Count;
                var buffViewBase = bufferViews.Count;

                bytes.AddRange(modelMesh.VertexBuffer.VertexData);

                var byteStride = VertexDeclaration.GetVertexStrideSize(modelMesh.MeshParts[0].GetVertexDeclaration(), 0);

                bufferViews.Add(new { buffer = 0, byteOffset = byteBase, byteLength = modelMesh.VertexBuffer.VertexData.Length, byteStride, target = 34962 });

                var indices = new int[modelMesh.IndexBuffer.Count];
                modelMesh.IndexBuffer.CopyTo(indices, 0);
                foreach (var i in indices)
                    bytes.AddRange(BitConverter.GetBytes((ushort)i));

                bufferViews.Add(new { buffer = 0, byteOffset = byteBase + modelMesh.VertexBuffer.VertexData.Length, byteLength = indices.Length * 2, target = 34963 });

                foreach (var part in modelMesh.MeshParts)
                {
                    var accessorBase = accessors.Count;

                    if (byteStride != VertexDeclaration.GetVertexStrideSize(part.GetVertexDeclaration(), 0))
                    {
                        throw new InvalidOperationException("byteStride" + byteStride);
                    }

                    var min = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
                    var max = new[] { float.MinValue, float.MinValue, float.MinValue };
                    for (var i = 0; i < part.NumVertices; i++)
                    {
                        var x = BitConverter.ToSingle(modelMesh.VertexBuffer.VertexData, part.StreamOffset + i * byteStride);
                        var y = BitConverter.ToSingle(modelMesh.VertexBuffer.VertexData, part.StreamOffset + i * byteStride + 4);
                        var z = BitConverter.ToSingle(modelMesh.VertexBuffer.VertexData, part.StreamOffset + i * byteStride + 8);
                        if (x < min[0]) min[0] = x;
                        if (x > max[0]) max[0] = x;
                        if (y < min[1]) min[1] = y;
                        if (y > max[1]) max[1] = y;
                        if (z < min[2]) min[2] = z;
                        if (z > max[2]) max[2] = z;
                    }

                    accessors.Add(new { bufferView = buffViewBase, byteOffset = part.StreamOffset, componentType = 5126, type = "VEC3", count = part.NumVertices, min, max });
                    accessors.Add(new { bufferView = buffViewBase, byteOffset = part.StreamOffset + 12, componentType = 5126, type = "VEC3", count = part.NumVertices });
                    accessors.Add(new { bufferView = buffViewBase, byteOffset = part.StreamOffset + 24, componentType = 5126, type = "VEC2", count = part.NumVertices });

                    accessors.Add(new { bufferView = buffViewBase + 1, byteOffset = part.BaseVertex, componentType = 5123, type = "SCALAR", count = part.PrimitiveCount * 3 });

                    if (part.StartIndex != 0)
                    {
                        throw new NotSupportedException();
                    }

                    primitives.Add(new
                    {
                        attributes = new { POSITION = accessorBase, NORMAL = accessorBase + 1, TEXCOORD_0 = accessorBase + 2 },
                        indices = accessorBase + 3,
                        material = materials.Count,
                        mode = 4,
                    });

                    var e = part.Material.Textures.Values.GetEnumerator();
                    e.MoveNext();
                    if (e.Current != null)
                    {
                        var src = e.Current.Filename;
                        var uri = imageDict.Count <= 0 ? baseName + ".png" : baseName + "_" + imageDict.Count + ".png";
                        if (!imageDict.ContainsKey(src))
                        {
                            imageDict.Add(src, uri);
                            var a = Path.Combine(Path.GetDirectoryName(input.Identity.SourceFilename), Path.GetFileNameWithoutExtension(src).Replace("_0", ".bmp"));
                            if (!File.Exists(a))
                                a = Path.ChangeExtension(a, ".jpg"); 
                            if (!File.Exists(a))
                                a = Path.ChangeExtension(a, ".png");
                            if (!File.Exists(a))
                                a = Path.ChangeExtension(a, ".tga");
                            if (File.Exists(a))
                                File.Copy(a, uri, true);
                            else
                                context.Logger.LogWarning("a", input.Identity, Path.GetFileNameWithoutExtension(src).Replace("_0", ".*"));
                        }

                        uri = Path.GetFileName(uri);
                        materials.Add(new { pbrMetallicRoughness = new { baseColorTexture = new { index = textures.Count } } });
                        textures.Add(new { source = images.Count });
                        images.Add(new { uri });
                    }
                }

                nodes.Add(new { mesh = meshes.Count });
                sceneNodes.Add(meshes.Count);
                meshes.Add(new { primitives });
            }

            File.WriteAllBytes(binName, bytes.ToArray());
            File.WriteAllText(baseName + ".gltf", JsonConvert.SerializeObject(new
            {
                asset = new { version = "2.0" },
                scene = 0,
                buffers = new[] { new { byteLength = bytes.Count, uri = Path.GetFileName(binName) } },
                bufferViews,
                accessors,
                meshes,
                images,
                textures,
                materials,
                scenes = new[] { new { nodes = sceneNodes }},
                nodes,
            }, Formatting.Indented));
        }

        private Dictionary<string, AnimationClip> ProcessAnimations(NodeContent input)
        {
            IList<NodeContent> bones = FlattenNodes(input);

            Dictionary<string, AnimationClip> animations = new Dictionary<string, AnimationClip>();

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
                                animations[entry.Key].Keyframes.Add(frame);
                            (animations[entry.Key].Keyframes as List<Keyframe>).Sort(CompareKeyframeTimes);
                        }
                        else
                        {
                            animations.Add(entry.Key, entry.Value);
                        }
                    }
                }

                foreach (NodeContent child in input.Children)
                    ProcessAnimation(child, animations, bones);
            }
        }

        private IList<NodeContent> FlattenNodes(NodeContent input)
        {
            List<NodeContent> nodes = new List<NodeContent>();

            FlattenNodes(input, nodes);

            return nodes;
        }

        private void FlattenNodes(NodeContent input, List<NodeContent> nodes)
        {
            if (input != null)
            {
                nodes.Add(input);

                foreach (NodeContent child in input.Children)
                    FlattenNodes(child, nodes);
            }
        }

        IList<NodeContent> BonesToNodes(IList<BoneContent> bones)
        {
            List<NodeContent> nodes = new List<NodeContent>(bones.Count);

            foreach (BoneContent bone in bones)
                nodes.Add(bone);

            return nodes;
        }

        private void AddSpacePartitionData(Dictionary<string, object> dictionary, ModelContent model)
        {
            //System.Diagnostics.Debugger.Launch();
            Vector3 position;

            Matrix transform;
            ModelSpacePartitionInformation info = new ModelSpacePartitionInformation();

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
            for (int i = 2; i < 6; i++)
                for (int j = 2; j < 6; j++)
                    for (int k = 2; k < 6; k++)
                    {
                        info.Set(info.Box.Min +
                                  new Vector3(position.X * (2 * i + 1), position.Y * (2 * j + 1), position.Z * (2 * k + 1)));
                    }

            dictionary.Add("SpacePartition", info.BitMap);


        }



        void AddNormalTextureToTag(ModelContent model)
        {
            foreach (ModelMeshContent mesh in model.Meshes)
                foreach (ModelMeshPartContent part in mesh.MeshParts)
                {
                    ExternalReference<TextureContent> value;
                    part.Material.Textures.TryGetValue("NormalTexture", out value);
                    part.Tag = value;
                }
        }

        /// <summary>
        /// Changes all the materials to use our skinned model effect.
        /// </summary>
        protected override MaterialContent ConvertMaterial(MaterialContent material,
                                                        ContentProcessorContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            BasicMaterialContent basicMaterial = material as BasicMaterialContent;

            if (basicMaterial == null)
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

            string textureFilename = basicMaterial.Texture.Filename;
            string normalTextureFilename =
                Path.GetDirectoryName(textureFilename) + "\\" +
                Path.GetFileNameWithoutExtension(textureFilename) + "_n" +
                Path.GetExtension(textureFilename);

            // Checks if the normal texture exists
            if (File.Exists(normalTextureFilename))
            {
                ExternalReference<Texture2DContent> normalTexture =
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
        static Dictionary<string, AnimationClip> ProcessAnimations(
            AnimationContentDictionary animations, IList<NodeContent> bones)
        {
            // Build up a table mapping bone names to indices.
            Dictionary<string, int> boneMap = new Dictionary<string, int>();

            for (int i = 0; i < bones.Count; i++)
            {
                string boneName = bones[i].Name;

                if (!string.IsNullOrEmpty(boneName))
                    boneMap.Add(boneName, i);
            }

            // Convert each animation in turn.
            Dictionary<string, AnimationClip> animationClips;
            animationClips = new Dictionary<string, AnimationClip>();

            foreach (KeyValuePair<string, AnimationContent> animation in animations)
            {
                AnimationClip processed = ProcessAnimation(animation.Value, boneMap);

                animationClips.Add(animation.Key, processed);
            }

            if (animationClips.Count == 0)
            {
                throw new InvalidContentException(
                            "Input file does not contain any animations.");
            }

            return animationClips;
        }


        /// <summary>
        /// Converts an intermediate format content pipeline AnimationContent
        /// object to our runtime AnimationClip format.
        /// </summary>
        static AnimationClip ProcessAnimation(AnimationContent animation,
                                              Dictionary<string, int> boneMap)
        {
            //System.Diagnostics.Debugger.Launch();
            List<Keyframe> keyframes = new List<Keyframe>();

            // For each input animation channel.
            foreach (KeyValuePair<string, AnimationChannel> channel in
                animation.Channels)
            {
                // Look up what bone this channel is controlling.
                int boneIndex;

                if (!boneMap.TryGetValue(channel.Key, out boneIndex))
                {
                    //throw new InvalidContentException(string.Format(
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
                throw new InvalidContentException("Animation has no keyframes.");

            if (animation.Duration <= TimeSpan.Zero)
                throw new InvalidContentException("Animation has a zero duration.");

            return new AnimationClip(animation.Duration, keyframes);
        }


        /// <summary>
        /// Comparison function for sorting keyframes into ascending time order.
        /// </summary>
        static int CompareKeyframeTimes(Keyframe a, Keyframe b)
        {
            return a.Time.CompareTo(b.Time);
        }


        /// <summary>
        /// Makes sure this mesh contains the kind of data we know how to animate.
        /// </summary>
        static void ValidateMesh(NodeContent node, ContentProcessorContext context,
                                 string parentBoneName)
        {
            MeshContent mesh = node as MeshContent;

            if (mesh != null)
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
                    //context.Logger.LogWarning(null, null,
                    //    "Mesh {0} has no skinning information, so it has been deleted.",
                    //    mesh.Name);

                    //mesh.Parent.Children.Remove(mesh);
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
                ValidateMesh(child, context, parentBoneName);
        }


        /// <summary>
        /// Checks whether a mesh contains skininng information.
        /// </summary>
        static bool MeshHasSkinning(MeshContent mesh)
        {
            foreach (GeometryContent geometry in mesh.Geometry)
            {
                if (!geometry.Vertices.Channels.Contains(VertexChannelNames.Weights()))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether a mesh is a skinned mesh
        /// </summary>
        static bool IsSkinned(NodeContent node)
        {
            MeshContent mesh = node as MeshContent;

            if (mesh != null && MeshHasSkinning(mesh))
                return true;

            // Recurse (iterating over a copy of the child collection,
            // because validating children may delete some of them).
            foreach (NodeContent child in new List<NodeContent>(node.Children))
                if (IsSkinned(child))
                    return true;

            return false;
        }

        /// <summary>
        /// Bakes unwanted transforms into the model geometry,
        /// so everything ends up in the same coordinate system.
        /// </summary>
        static void FlattenTransforms(NodeContent node, BoneContent skeleton)
        {
            foreach (NodeContent child in node.Children)
            {
                // Don't process the skeleton, because that is special.
                if (child == skeleton)
                    continue;

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

    #endregion

    #region Space Partition
    public class ModelSpacePartitionInformation
    {
        public BoundingBox Box;

        // 585 = 1 + 8 * 8 + 8 * 8 * 8
        public bool[] BitMap = new bool[585];

        readonly static int[] spliter = new int[5] { 0, 1, 9, 73, 585 };

        /// <summary>
        /// Test if that position is occupied
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool Occupied(Vector3 position)
        {
            int x = (int)((position.X - Box.Min.X) / ((Box.Max.X - Box.Min.X) / 8));
            int y = (int)((position.Y - Box.Min.Y) / ((Box.Max.Y - Box.Min.Y) / 8));
            int z = (int)((position.Z - Box.Min.Z) / ((Box.Max.Z - Box.Min.Z) / 8));
            if (x == 8) x = 7;
            if (y == 8) y = 7;
            if (z == 8) z = 7;
            int p = 0;
            int[] power = new int[3] { 1, 2, 4 };
            for (int l = 0; l < 4; l++)
            {
                if (!BitMap[p])
                    return false;
                if (l == 3)
                    return true;
                int pow = power[2 - l];
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
        /// Set the position as Occupied
        /// </summary>
        /// <param name="position"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public void Set(Vector3 position)
        {
            int x = (int)((position.X - Box.Min.X) / ((Box.Max.X - Box.Min.X) / 8));
            int y = (int)((position.Y - Box.Min.Y) / ((Box.Max.Y - Box.Min.Y) / 8));
            int z = (int)((position.Z - Box.Min.Z) / ((Box.Max.Z - Box.Min.Z) / 8));
            if (x == 8) x = 7;
            if (y == 8) y = 7;
            if (z == 8) z = 7;
            int p = 4 * (x / 4) + 2 * (y / 4) + (z / 4) + spliter[1];
            BitMap[0] = BitMap[p] = true;

            x %= 4;
            y %= 4;
            z %= 4;

            int tp = SonOf(p, 4 * (x / 2) + 2 * (y / 2) + (z / 2), 1);
            BitMap[tp] = true;

            x %= 2;
            y %= 2;
            z %= 2;
            BitMap[SonOf(tp, 4 * x + 2 * y + z, 2)] = true;
        }

        /// <summary>
        /// Find of position of the mth son of parent
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="m"></param>
        /// <param name="parLevel"></param>
        /// <returns></returns>
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
            BoundingBox rtv = new BoundingBox(Box.Min, Box.Max);
            if (index == 0)
            {
                return rtv;
            }

            bool x, y, z;
            int level;
            for (level = 0; level < 4; level++)
            {
                if (index < spliter[level + 1])
                    break;
            }

            int[] parents = new int[level + 1];
            int[] bias = new int[level + 1];
            bias[0] = 0;
            parents[level] = index;

            // Calculate parents in the tree, and their bias
            for (int i = level; i > 0; i--)
            {
                bias[i] = (parents[i] - spliter[i]) % 8;
                parents[i - 1] = ParentOf(parents[i], i);
            }

            for (int i = 1; i <= level; i++)
            {
                x = (bias[i] > 3);
                y = (bias[i] % 4 > 1);
                z = (bias[i] % 2 == 1);
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
    #endregion

    #region NormalMapTextureProcessor
    /// <summary>
    /// The NormalMapTextureProcessor takes in an encoded normal map, and outputs
    /// a texture in the NormalizedByte4 format.  Every pixel in the source texture
    /// is remapped so that values ranging from 0 to 1 will range from -1 to 1.
    /// </summary>
    [ContentProcessor]
    class NormalMapTextureProcessor : ContentProcessor<TextureContent, TextureContent>
    {
        /// <summary>
        /// Process converts the encoded normals to the NormalizedByte4 format and 
        /// generates mipmaps.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
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
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
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
    #endregion

    #region AnimationClip
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
            keyframesValue = keyframes;
        }


        /// <summary>
        /// Gets the total length of the animation.
        /// </summary>
        public TimeSpan Duration
        {
            get { return durationValue; }
        }

        TimeSpan durationValue;


        /// <summary>
        /// Gets a combined list containing all the keyframes for all bones,
        /// sorted by time.
        /// </summary>
        public IList<Keyframe> Keyframes
        {
            get { return keyframesValue; }
        }

        IList<Keyframe> keyframesValue;
    }
    #endregion

    #region Keyframe
    /// <summary>
    /// Describes the position of a single bone at a single point in time.
    /// </summary>
    public class Keyframe
    {
        #region Fields

        int boneValue;
        TimeSpan timeValue;
        Matrix transformValue;

        #endregion


        /// <summary>
        /// Constructs a new keyframe object.
        /// </summary>
        public Keyframe(int bone, TimeSpan time, Matrix transform)
        {
            boneValue = bone;
            timeValue = time;
            transformValue = transform;
        }


        /// <summary>
        /// Gets the index of the target bone that is animated by this keyframe.
        /// </summary>
        public int Bone
        {
            get { return boneValue; }
        }


        /// <summary>
        /// Gets the time offset from the start of the animation to this keyframe.
        /// </summary>
        public TimeSpan Time
        {
            get { return timeValue; }
        }


        /// <summary>
        /// Gets the bone transform for this keyframe.
        /// </summary>
        public Matrix Transform
        {
            get { return transformValue; }
        }
    }
    #endregion

    #region SkinningData
    /// <summary>
    /// Combines all the data needed to render and animate a skinned object.
    /// This is typically stored in the Tag property of the Model being animated.
    /// </summary>
    public class SkinningData
    {
        #region Fields

        IList<Matrix> bindPoseValue;
        IList<Matrix> inverseBindPoseValue;
        IList<int> skeletonHierarchyValue;
        IList<string> boneNameValue;
        #endregion


        /// <summary>
        /// Constructs a new skinning data object.
        /// </summary>
        public SkinningData(IList<Matrix> bindPose, IList<Matrix> inverseBindPose,
                            IList<int> skeletonHierarchy, IList<string> boneName)
        {
            bindPoseValue = bindPose;
            inverseBindPoseValue = inverseBindPose;
            skeletonHierarchyValue = skeletonHierarchy;
            boneNameValue = boneName;
        }


        /// <summary>
        /// Bindpose matrices for each bone in the skeleton,
        /// relative to the parent bone.
        /// </summary>
        public IList<Matrix> BindPose
        {
            get { return bindPoseValue; }
        }


        /// <summary>
        /// Vertex to bonespace transforms for each bone in the skeleton.
        /// </summary>
        public IList<Matrix> InverseBindPose
        {
            get { return inverseBindPoseValue; }
        }


        /// <summary>
        /// For each bone in the skeleton, stores the index of the parent bone.
        /// </summary>
        public IList<int> SkeletonHierarchy
        {
            get { return skeletonHierarchyValue; }
        }

        /// <summary>
        /// Gets the name for each bone
        /// </summary>
        public IList<string> BoneName
        {
            get { return boneNameValue; }
        }
    }
    #endregion

    #region TypeWriters
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
                   "Isles.Engine, Version=1.0.0.0, Culture=neutral";
        }
    }


    /// <summary>
    /// Writes AnimationClip objects into compiled XNB format
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
                   "Isles.Engine, Version=1.0.0.0, Culture=neutral";
        }
    }


    /// <summary>
    /// Writes Keyframe objects into compiled XNB format
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
                   "Isles.Engine, Version=1.0.0.0, Culture=neutral";
        }
    }
    #endregion
}
