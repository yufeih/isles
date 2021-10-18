// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Pipeline
{
    /// <summary>
    /// Isles game landscape.
    /// </summary>
    [Serializable]
    public partial class Landscape
    {
        private string heightmap;
        private string waterTexture;
        private string waterBumpTexture;
        private float earthRadius;
        private string skyBox;
        private List<Layer> layers = new();
        private List<Vegetation> vegetations = new();
        private float width;
        private float depth;
        private float height;
        private float baseHeight;
        private int xPatchCount;
        private int yPatchCount;
        private int heightFieldWidth;
        private int heightFieldHeight;
        private float[,] heightData;
        private Vector3[,] normalData;
        private Vector3[,] tangentData;
        private List<Vector3> boundingBoxes = new List<Vector3>();
        private List<int>[] patchGroups;
        private ExternalReference<TextureContent> waterColorTexture;
        private ExternalReference<TextureContent> waterBumpTextureContent;
        private ExternalReference<TextureCubeContent> skyBoxCubeTexture;

        [NonSerialized]
        public string SourceFilename;
        private string directory;

        public string Heightmap
        {
            get => heightmap;
            set => heightmap = value;
        }

        public string WaterTexture
        {
            get => waterTexture;
            set => waterTexture = value;
        }

        public string WaterBumpTexture
        {
            get => waterBumpTexture;
            set => waterBumpTexture = value;
        }

        public float EarthRadius
        {
            get => earthRadius;
            set => earthRadius = value;
        }

        public string SkyBox
        {
            get => skyBox;
            set => skyBox = value;
        }

        public List<Layer> LayerCollection
        {
            get => layers;
            set => layers = value;
        }

        public List<Vegetation> VegetationCollection
        {
            get => vegetations;
            set => vegetations = value;
        }

        /// <summary>
        /// Size in x axis.
        /// </summary>
        public float Width
        {
            get => width;
            set => width = value;
        }

        /// <summary>
        /// Size in y axis.
        /// </summary>
        public float Depth
        {
            get => depth;
            set => depth = value;
        }

        /// <summary>
        /// Size in z axis.
        /// </summary>
        public float Height
        {
            get => height;
            set => height = value;
        }

        public float BaseHeight
        {
            get => baseHeight;
            set => baseHeight = value;
        }

        /// <summary>
        /// Represent a texture layer in the terrain.
        /// </summary>
        [Serializable]
        public class Layer
        {
            private string colorTexture = "";
            private string alphaTexture = "";
            private string normalTexture = "";
            private string technique = "";
            private ExternalReference<TextureContent> colorTextureContent;
            private ExternalReference<TextureContent> alphaTextureContent;
            private ExternalReference<TextureContent> normalTextureContent;

            public int PatchGroup;
            public bool[] TargetPatches;

            public Layer()
            {
            }

            /// <summary>
            /// Effect technqiue.
            /// </summary>
            public string Technique
            {
                get => technique;
                set => technique = value;
            }

            /// <summary>
            /// Color texture.
            /// </summary>
            public string ColorTexture
            {
                get => colorTexture;
                set => colorTexture = value;
            }

            /// <summary>
            /// Alpha texture.
            /// </summary>
            public string AlphaTexture
            {
                get => alphaTexture;
                set => alphaTexture = value;
            }

            /// <summary>
            /// Normal texture.
            /// </summary>
            public string NormalTexture
            {
                get => normalTexture;
                set => normalTexture = value;
            }

            public void Process(ContentProcessorContext context,
                string directory, int xPatchCount, int yPatchCount)
            {
                // Build color texture
                colorTextureContent = context.BuildAsset<TextureContent, TextureContent>
                    (new ExternalReference<TextureContent>(Path.Combine(directory, colorTexture)), null);

                // Build alpha texture
                alphaTextureContent = context.BuildAsset<TextureContent, TextureContent>
                    (new ExternalReference<TextureContent>(Path.Combine(directory, alphaTexture)), null);

                // Build normal texture
                var normalTextureFile = Path.Combine(directory, normalTexture);
                if (File.Exists(normalTextureFile))
                {
                    normalTextureContent = context.BuildAsset<TextureContent, TextureContent>
                        (new ExternalReference<TextureContent>(Path.Combine(directory, normalTexture)), null);
                }

                // Process alpha texture to get patches that this layer will finally render to
                var visibility = (Texture2DContent)
                    context.BuildAndLoadAsset<TextureContent, TextureContent>
                        (new ExternalReference<TextureContent>(Path.Combine(directory, alphaTexture)),
                        "TextureProcessor");

                // Convert to pixel bitmap, but we only care about alpha channel
                visibility.ConvertBitmapType(typeof(PixelBitmapContent<Color>));
                var pixmap = (PixelBitmapContent<Color>)visibility.Mipmaps[0];

                TargetPatches = new bool[xPatchCount * yPatchCount];

                for (var y = 0; y < yPatchCount; y++)
                {
                    for (var x = 0; x < xPatchCount; x++)
                    {
                        var render = false;
                        for (var yy = 0; yy < pixmap.Height / yPatchCount; yy++)
                        {
                            for (var xx = 0; xx < pixmap.Width / xPatchCount; xx++)
                            {
                                if (pixmap.GetPixel(
                                    x * pixmap.Width / xPatchCount + xx,
                                    y * pixmap.Height / yPatchCount + yy).A != 0)
                                {
                                    render = true;
                                    goto FinishPatch;
                                }
                            }
                        }

                    FinishPatch:
                        TargetPatches[y * xPatchCount + x] = render;
                    }
                }
            }

            public void Write(ContentWriter output)
            {
                output.Write(PatchGroup);
                output.Write(technique);
                output.WriteExternalReference<TextureContent>(colorTextureContent);
                output.WriteExternalReference<TextureContent>(alphaTextureContent);
                output.WriteExternalReference<TextureContent>(normalTextureContent);
            }
        }

        public Landscape()
        {
        }

        public void Process(ContentProcessorContext context)
        {
            directory = Path.GetDirectoryName(SourceFilename);

            // Heightmap
            var map = (Texture2DContent)
                context.BuildAndLoadAsset<TextureContent, TextureContent>(
                new ExternalReference<TextureContent>(Path.Combine(directory, heightmap)),
                "TextureProcessor");

            map.ConvertBitmapType(typeof(PixelBitmapContent<float>));
            var heightfield = (PixelBitmapContent<float>)map.Mipmaps[0];

            // Validate heightmap
            if ((heightfield.Width != 129 && heightfield.Width != 257 &&
                 heightfield.Width != 513 && heightfield.Width != 1025) ||
                (heightfield.Height != 129 && heightfield.Height != 257 &&
                 heightfield.Height != 513 && heightfield.Height != 1025))
            {
                throw new Exception("Height map size must be 129, 257, 513 or 1025");
            }

            heightFieldWidth = heightfield.Width;
            heightFieldHeight = heightfield.Height;
            heightData = new float[heightFieldWidth, heightFieldHeight];
            for (var y = 0; y < heightFieldHeight; y++)
            {
                for (var x = 0; x < heightFieldWidth; x++)
                {
                    // Negate Y value
                    heightData[x, y] = baseHeight + height * heightfield.GetPixel(x, y);
                }
            }

            CalculateNormalsAndTangents();

            // Number of patches
            xPatchCount = heightfield.Width / 16;
            yPatchCount = heightfield.Height / 16;

            // Generate bounding box for each patch
            float min, max;
            boundingBoxes = new List<Vector3>(2 * xPatchCount * yPatchCount);
            for (var y = 0; y < yPatchCount; y++)
            {
                for (var x = 0; x < xPatchCount; x++)
                {
                    min = height;
                    max = 0;

                    for (var yy = 0; yy <= 16; yy++)
                    {
                        for (var xx = 0; xx <= 16; xx++)
                        {
                            var h = heightData
                                [x * 16 + xx, y * 16 + yy];

                            if (h < min)
                            {
                                min = h;
                            }

                            if (h > max)
                            {
                                max = h;
                            }
                        }
                    }

                    // Write bounding boxes (min, max point)
                    boundingBoxes.Add(new Vector3
                        (x * width / xPatchCount, y * depth / yPatchCount, min));
                    boundingBoxes.Add(new Vector3
                        ((x + 1) * width / xPatchCount, (y + 1) * depth / yPatchCount, max));
                }
            }

            foreach (Layer layer in layers)
            {
                layer.Process(context, directory, xPatchCount, yPatchCount);
            }

            // Generate patch groups
            // A patch group is a set of patches that would be drawed within
            // one draw call. If a layer won't be drawed to a certain patch,
            // that patch won't be rendered when drawing that layer.
            // So first we get all the patches that a layer will render to,
            // find those layers that share the same patches and group them
            // together to form a patch group.
            var patchGroupCount = 0;
            var unionFindIndex = new int[layers.Count];
            for (var i = 0; i < unionFindIndex.Length; i++)
            {
                unionFindIndex[i] = -1; // -1 represents root
                var equal = true;
                if (i > 0)
                {
                    for (var k = 0; k < layers[i].TargetPatches.Length; k++)
                    {
                        if (layers[i].TargetPatches[k] != layers[i - 1].TargetPatches[k])
                        {
                            equal = false;
                        }
                    }

                    if (equal)
                    {
                        unionFindIndex[i] = i - 1;
                    }
                }

                // A new patch group is found
                if (unionFindIndex[i] == -1)
                {
                    patchGroupCount++;
                }
            }

            // Create patch groups, set index to patch groups on each layer
            var iPatchGroup = 0;
            patchGroups = new List<int>[patchGroupCount];
            for (var i = 0; i < unionFindIndex.Length; i++)
            {
                if (unionFindIndex[i] == -1)
                {
                    // A a new patch group
                    patchGroups[iPatchGroup] = new List<int>();
                    for (var k = 0; k < layers[i].TargetPatches.Length; k++)
                    {
                        if (layers[i].TargetPatches[k])
                        {
                            patchGroups[iPatchGroup].Add(k);
                        }
                    }

                    // Set layer patch group pointer
                    layers[i].PatchGroup = iPatchGroup;

                    // Increase patch group counter
                    iPatchGroup++;
                }
                else
                {
                    // Find the root
                    var k = i;
                    while (unionFindIndex[k] != -1)
                    {
                        k = unionFindIndex[k];
                    }

                    layers[i].PatchGroup = layers[k].PatchGroup;
                }
            }

            // Water
            waterColorTexture = context.BuildAsset<TextureContent, TextureContent>(
                new ExternalReference<TextureContent>(
                Path.Combine(directory, waterTexture)), null);

            waterBumpTextureContent = context.BuildAsset<TextureContent, TextureContent>(
                new ExternalReference<TextureContent>(
                Path.Combine(directory, waterBumpTexture)), null);

            // Skybox cube texture
            skyBoxCubeTexture = context.BuildAsset<TextureCubeContent, TextureCubeContent>(
                new ExternalReference<TextureCubeContent>(
                Path.Combine(directory, skyBox)), null);

            // Vegetations
            ProcessVegetation(context);
        }

        public void Write(ContentWriter output)
        {
            output.Write(width);
            output.Write(depth);
            output.Write(height);

            // heightfield
            output.Write(heightFieldWidth);
            output.Write(heightFieldHeight);
            for (var y = 0; y < heightFieldHeight; y++)
            {
                for (var x = 0; x < heightFieldWidth; x++)
                {
                    output.Write(heightData[x, y]);
                }
            }

            // Normals
            for (var y = 0; y < heightFieldHeight; y++)
            {
                for (var x = 0; x < heightFieldWidth; x++)
                {
                    output.Write(normalData[x, y]);
                }
            }

            // Tangents
            for (var y = 0; y < heightFieldHeight; y++)
            {
                for (var x = 0; x < heightFieldWidth; x++)
                {
                    output.Write(tangentData[x, y]);
                }
            }

            // bounding boxes
            output.Write(xPatchCount);
            output.Write(yPatchCount);
            for (var i = 0; i < boundingBoxes.Count; i++)
            {
                output.Write(boundingBoxes[i]);
            }

            // patch groups
            output.Write(patchGroups.Length);
            for (var i = 0; i < patchGroups.Length; i++)
            {
                output.Write(patchGroups[i].Count);
                for (var k = 0; k < patchGroups[i].Count; k++)
                {
                    output.Write(patchGroups[i][k]);
                }
            }

            // layers
            output.Write(layers.Count);
            for (var i = 0; i < layers.Count; i++)
            {
                layers[i].Write(output);
            }

            // water model
            output.Write(earthRadius);
            output.WriteExternalReference<TextureContent>(waterColorTexture);
            output.WriteExternalReference<TextureContent>(waterBumpTextureContent);
            output.WriteExternalReference<TextureCubeContent>(skyBoxCubeTexture);

            // write vegetation
            WriteVegetation(output);
        }

        private Vector3 CalcLandscapePos(int x, int y)
        {
            // Make sure we stay on the valid map data
            var mapX = x < 0 ? 0 : x >= heightFieldWidth ? heightFieldWidth - 1 : x;
            var mapY = y < 0 ? 0 : y >= heightFieldHeight ? heightFieldHeight - 1 : y;

            return new Vector3(
                x * width / (heightFieldWidth - 1),
                y * depth / (heightFieldHeight - 1), heightData[mapX, mapY]);
        }

        /// <summary>
        /// Calculate normals from height data.
        /// </summary>
        private void CalculateNormalsAndTangents()
        {
            // Code grabbed from racing game :)
            normalData = new Vector3[heightFieldWidth, heightFieldHeight];
            tangentData = new Vector3[heightFieldWidth, heightFieldHeight];

            // Build our tangent vertices
            for (var x = 0; x < heightFieldWidth; x++)
            {
                for (var y = 0; y < heightFieldHeight; y++)
                {
                    // Step 1: Calculate position
                    Vector3 pos = CalcLandscapePos(x, y);// texData);

                    // Step 2: Calculate all edge vectors (for normals and tangents)
                    // This involves quite complicated optimizations and mathematics,
                    // hard to explain with just a comment. Read my book :D
                    Vector3 edge1 = pos - CalcLandscapePos(x, y + 1);
                    Vector3 edge2 = pos - CalcLandscapePos(x + 1, y);
                    Vector3 edge3 = pos - CalcLandscapePos(x - 1, y + 1);
                    Vector3 edge4 = pos - CalcLandscapePos(x + 1, y + 1);
                    Vector3 edge5 = pos - CalcLandscapePos(x - 1, y - 1);

                    // Step 3: Calculate normal based on the edges (interpolate
                    // from 3 cross products we build from our edges).
                    normalData[x, y] = Vector3.Normalize(
                        Vector3.Cross(edge2, edge1) +
                        Vector3.Cross(edge4, edge3) +
                        Vector3.Cross(edge3, edge5));

                    // Step 4: Set tangent data, just use edge1
                    tangentData[x, y] = Vector3.Normalize(edge1);
                }
            }

            // Smooth all normals, first copy them over, then smooth everything
            var normalsForSmoothing = new Vector3[heightFieldWidth, heightFieldHeight];
            for (var x = 0; x < heightFieldWidth; x++)
            {
                for (var y = 0; y < heightFieldHeight; y++)
                {
                    normalsForSmoothing[x, y] = normalData[x, y];
                }
            }

            // Time to smooth to normals we just saved
            for (var x = 1; x < heightFieldWidth - 1; x++)
            {
                for (var y = 1; y < heightFieldHeight - 1; y++)
                {
                    // Smooth 3x3 normals, but still use old normal to 40% (5 of 13)
                    Vector3 normal = normalData[x, y] * 4;
                    for (var xAdd = -1; xAdd <= 1; xAdd++)
                    {
                        for (var yAdd = -1; yAdd <= 1; yAdd++)
                        {
                            normal += normalsForSmoothing[x + xAdd, y + yAdd];
                        }
                    }

                    normalData[x, y] = Vector3.Normalize(normal);

                    // Also recalculate tangent to let it stay 90 degrees on the normal
                    var helperVector = Vector3.Cross(normalData[x, y], tangentData[x, y]);
                    tangentData[x, y] = Vector3.Cross(helperVector, normalData[x, y]);
                }
            }
        }
    }
}
