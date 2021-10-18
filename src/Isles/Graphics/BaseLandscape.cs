//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    #region LandscapeReader
    public class LandscapeReader : ContentTypeReader<BaseLandscape>
    {
        /// <summary>
        /// Content loader for loading Landscape object from XNB file
        /// </summary>
        protected override BaseLandscape Read(ContentReader input, BaseLandscape existingInstance)
        {
            //Landscape landscape = new MipmappedLandscape();
            BaseLandscape landscape = new TiledLandscape();

            landscape.ReadContent(input);
            landscape.Initialize(BaseGame.Singleton);

            return landscape;
        }
    }
    #endregion

    #region BaseLandscape
    public abstract class BaseLandscape : ILandscape
    {
        #region Layer
        /// <summary>
        /// Represents a texture layer on the terrain
        /// </summary>
        public class Layer : IDisposable
        {
            private readonly int patchGroup;
            private string technology;
            private Texture2D colorTexture;
            private Texture2D alphaTexture;
            private Texture2D normalTexture;

            /// <summary>
            /// Gets Which patch group the layer is in
            /// </summary>
            public int PatchGroup => patchGroup;

            /// <summary>
            /// Gets or sets the technology used to render this layer
            /// </summary>
            public string Technology
            {
                get => technology;
                set => technology = value;
            }

            /// <summary>
            /// Gets or sets the color texture of this layer
            /// </summary>
            public Texture2D ColorTexture
            {
                get => colorTexture;
                set => colorTexture = value;
            }

            /// <summary>
            /// Gets the alpha texture of this layer
            /// </summary>
            /// <remarks>
            /// Path group is not refreshed so setting alpha texture
            /// might cause some unexpected rendering errors
            /// </remarks>
            public Texture2D AlphaTexture
            {
                get => alphaTexture;
                set => alphaTexture = value;
            }

            /// <summary>
            /// Gets or sets the normal texture of this layer
            /// </summary>
            public Texture2D NormalTexture
            {
                get => normalTexture;
                set => normalTexture = value;
            }

            /// <summary>
            /// Creates a new layer
            /// </summary>
            public Layer() { }

            /// <summary>
            /// Create a layer from a content input
            /// </summary>
            /// <param name="input"></param>
            public Layer(ContentReader input)
            {
                patchGroup = input.ReadInt32();
                technology = input.ReadString();
                colorTexture = input.ReadExternalReference<Texture2D>();
                alphaTexture = input.ReadExternalReference<Texture2D>();
                normalTexture = input.ReadExternalReference<Texture2D>();
            }

            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose
            /// </summary>
            /// <param name="disposing">Disposing</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (colorTexture != null)
                    {
                        colorTexture.Dispose();
                    }

                    if (alphaTexture != null)
                    {
                        alphaTexture.Dispose();
                    }

                    if (normalTexture != null)
                    {
                        normalTexture.Dispose();
                    }
                }
            }
        }
        #endregion
        
        #region Patch
        /// <summary>
        /// Represents the smallest unit of the terrain
        /// </summary>
        public class Patch
        {
            #region Misc

            private int lod = 0;    // 0, 1, 2, 3, 4
            private readonly int index, xIndex, yIndex;
            private bool visible = true;
            private BoundingBox boundingBox;
            private readonly BaseLandscape landscape;
            private Vector3 center;

            /// <summary>
            /// Gets or sets starting vertex index when filling patch indices
            /// </summary>
#if SHORTINDEX
            public UInt16 StartingVertex = 0;
#else
            public UInt32 StartingVertex = 0;
#endif

            private static int i = 0;
            /// <summary>
            /// Contruct a patch from content input
            /// </summary>
            /// <param name="input"></param>
            /// <param name="index"></param>
            public Patch(ContentReader input, int index, BaseLandscape landscape)
            {
                xIndex = index % landscape.PatchCountOnXAxis;
                yIndex = index / landscape.PatchCountOnYAxis;

                lod = (i++) % 5;
                this.index = index;
                this.landscape = landscape;

                boundingBox = new BoundingBox(input.ReadVector3(), input.ReadVector3());

                center = (boundingBox.Max + boundingBox.Max) / 2;
            }

            /// <summary>
            /// Update patch LOD
            /// </summary>
            /// <returns>Whether LOD has changed</returns>
            public bool UpdateLOD(Vector3 eye, float errorRatio)
            {
                // Compute distance
                var distance = Vector3.Distance(eye, center);
                var newLOD = 4 - (int)(distance * errorRatio);

                // Clamp LOD value
                if (newLOD < 0)
                {
                    newLOD = 0;
                }
                else if (newLOD > 4)
                {
                    newLOD = 4;
                }

                if (newLOD != lod)
                {
                    lod = newLOD;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Gets patch center position
            /// </summary>
            public Vector3 Center => center;

            /// <summary>
            /// Gets or sets patch visibility
            /// </summary>
            public bool Visible
            {
                get => visible;
                set => visible = value;
            }

            public const int HighestLOD = 4;
            public const int LowestLOD = 0;

            /// <summary>
            /// Gets patch level of detail. 0 is the lowest, 4 is the highest
            /// </summary>
            public int LevelOfDetail
            {
                get => lod;
                set => lod = value;
            }

            /// <summary>
            /// Gets index in all terrain patches
            /// </summary>
            public int Index => index;

            /// <summary>
            /// Gets the patch index on the x axis
            /// </summary>
            public int IndexOnXAxis => xIndex;

            /// <summary>
            /// Gets the patch index on the y axis
            /// </summary>
            public int IndexOnYAxis => yIndex;

            /// <summary>
            /// Gets patch bounding box
            /// </summary>
            public BoundingBox BoundingBox => boundingBox;

            #endregion

            #region Fill Patch Vertices and Indices

            public delegate Vector3 GetVertexPosition(int x, int y);
            public delegate void SetVertexPosition(uint index, Vector3 position);
            public delegate void SetVertex(uint index, int x, int y);

            /// <summary>
            /// Fill the vertex list with the vertices on this patch based on current LOD
            /// </summary>
            /// <param name="vertices"></param>
            /// <returns>Number of vertices added to the list</returns>
            public uint FillVertices(uint baseIndex, GetVertexPosition get,
                                                     SetVertexPosition set,
                                                     SetVertex setVertex)
            {
                int x, y, xPatch, yPatch, lowerLOD, t, k;
                for (var i = 0; i < MagicVertices[lod].Length; i++)
                {
                    xPatch = (int)MagicVertices[lod][i] % (MaxPatchResolution + 1);
                    yPatch = (int)MagicVertices[lod][i] / (MaxPatchResolution + 1);

                    x = xIndex * MaxPatchResolution;
                    y = yIndex * MaxPatchResolution;

                    Vector3 position = get(x + xPatch, y + yPatch);
                    setVertex(baseIndex, x + xPatch, y + yPatch);

                    // FIX patch tearing caused by different LOD
                    // Perform fixing on 4 edges

                    // Fix left edge
                    if (xPatch == 0 && yPatch != MaxPatchResolution && xIndex > 0 && lod >
                       (lowerLOD = landscape.GetPatch(xIndex - 1, yIndex).LevelOfDetail))
                    {
                        k = MagicLength[lowerLOD];
                        t = (yPatch / k) * k;
                        position.Z = MathHelper.Lerp(get(x + xPatch, y + t).Z,
                                                     get(x + xPatch, y + t + k).Z,
                                                     (float)(yPatch % k) / k);
                        set(baseIndex, position);
                    }
                    // Fix right edge
                    else if (xPatch == MaxPatchResolution && yPatch != MaxPatchResolution &&
                             xIndex < landscape.PatchCountOnXAxis - 1 && lod >
                            (lowerLOD = landscape.GetPatch(xIndex + 1, yIndex).LevelOfDetail))
                    {
                        k = MagicLength[lowerLOD];
                        t = (yPatch / k) * k;
                        position.Z = MathHelper.Lerp(get(x + xPatch, y + t).Z,
                                                     get(x + xPatch, y + t + k).Z,
                                                     (float)(yPatch % k) / k);
                        set(baseIndex, position);
                    }
                    // Fix bottom edge
                    else if (yPatch == 0 && xPatch != MaxPatchResolution && yIndex > 0 && lod >
                            (lowerLOD = landscape.GetPatch(xIndex, yIndex - 1).LevelOfDetail))
                    {
                        k = MagicLength[lowerLOD];
                        t = (xPatch / k) * k;
                        position.Z = MathHelper.Lerp(get(x + t, y + yPatch).Z,
                                                     get(x + t + k, y + yPatch).Z,
                                                     (float)(xPatch % k) / k);
                        set(baseIndex, position);
                    }
                    // Fix top edge
                    else if (yPatch == MaxPatchResolution && xPatch != MaxPatchResolution &&
                             yIndex < landscape.PatchCountOnYAxis - 1 && lod >
                            (lowerLOD = landscape.GetPatch(xIndex, yIndex + 1).LevelOfDetail))
                    {
                        k = MagicLength[lowerLOD];
                        t = (xPatch / k) * k;
                        position.Z = MathHelper.Lerp(get(x + t, y + yPatch).Z,
                                                     get(x + t + k, y + yPatch).Z,
                                                     (float)(xPatch % k) / k);
                        set(baseIndex, position);
                    }

                    // Increment vertex counter
                    baseIndex++;
                }

                return (uint)MagicVertices[lod].Length;
            }

            /// <summary>
            /// Fill the vertex list with the indices on this patch based on current LOD
            /// </summary>
            /// <param name="indices"></param>
            /// <returns>Number of indices added to the list</returns>
            public UInt16 FillIndices16(ref UInt16[] indices, uint baseIndex)
            {
                for (var i = 0; i < MagicIndices[lod].Length; i++)
                {
                    indices[baseIndex++] = (UInt16)(MagicIndices[lod][i] + StartingVertex);
                }

                return (UInt16)MagicIndices[lod].Length;
            }

            public UInt32 FillIndices32(ref UInt32[] indices, uint baseIndex)
            {
                for (var i = 0; i < MagicIndices[lod].Length; i++)
                {
                    indices[baseIndex++] = (UInt32)(MagicIndices[lod][i] + StartingVertex);
                }

                return (UInt32)MagicIndices[lod].Length;
            }

            #endregion

            #region Magic Data :)

            /// <summary>
            /// At maximun resolution, a patch has 16 * 16 grids
            /// </summary>
            public const int MaxPatchResolution = 16;

            // Vertices
            private static readonly uint[] Vertices0 = new uint[] { 0, 16, 272, 288 };
            private static readonly uint[] Vertices1 = new uint[] { 0, 8, 16, 136, 144, 152, 272, 280, 288 };
            private static readonly uint[] Vertices2 = new uint[]
            {
                0, 4, 8, 12, 16, 68, 72, 76, 80, 84, 136, 140, 144, 148, 152,
                204, 208, 212, 216, 220, 272, 276, 280, 284, 288
            };
            private static readonly uint[] Vertices3 = new uint[]
            { 
                0, 2, 4, 6, 8, 10, 12, 14, 16, 34, 36, 38, 40, 42, 44, 46, 48, 50, 68, 70, 72,
                74, 76, 78, 80, 82, 84, 102, 104, 106, 108, 110, 112, 114, 116, 118, 136, 138,
                140, 142, 144, 146, 148, 150, 152, 170, 172, 174, 176, 178, 180, 182, 184, 186,
                204, 206, 208, 210, 212, 214, 216, 218, 220, 238, 240, 242, 244, 246, 248, 250,
                252, 254, 272, 274, 276, 278, 280, 282, 284, 286, 288
            };
            private static readonly uint[] Vertices4 = new uint[]
            { 
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22
                , 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42
                , 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62
                , 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82
                , 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101,
                102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117,
                118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133,
                134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149,
                150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165,
                166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181,
                182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197,
                198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213,
                214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229,
                230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245,
                246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261,
                262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277,
                278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 288
            };

            // Indices
            private static readonly UInt32[] Indices0 = new UInt32[] { 0, 1, 3, 0, 3, 2 };
            private static readonly UInt32[] Indices1 = new UInt32[]
            {
                0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 3, 4, 7, 3, 7, 6, 4, 5, 8, 4, 8, 7
            };
            private static readonly UInt32[] Indices2 = new UInt32[]
            {
                0, 1, 6, 0, 6, 5, 1, 2, 7, 1, 7, 6, 2, 3, 8, 2, 8, 7, 3, 4, 9, 3, 9, 8, 5, 6, 11
                , 5, 11, 10, 6, 7, 12, 6, 12, 11, 7, 8, 13, 7, 13, 12, 8, 9, 14, 8, 14, 13, 10,
                11, 16, 10, 16, 15, 11, 12, 17, 11, 17, 16, 12, 13, 18, 12, 18, 17, 13, 14, 19,
                13, 19, 18, 15, 16, 21, 15, 21, 20, 16, 17, 22, 16, 22, 21, 17, 18, 23, 17, 23,
                22, 18, 19, 24, 18, 24, 23
            };
            private static readonly UInt32[] Indices3 = new UInt32[]
            { 
                0, 1, 10, 0, 10, 9, 1, 2, 11, 1, 11, 10, 2, 3, 12, 2, 12, 11, 3, 4, 13, 3, 13, 
                12, 4, 5, 14, 4, 14, 13, 5, 6, 15, 5, 15, 14, 6, 7, 16, 6, 16, 15, 7, 8, 17, 7, 
                17, 16, 9, 10, 19, 9, 19, 18, 10, 11, 20, 10, 20, 19, 11, 12, 21, 11, 21, 20, 12,
                13, 22, 12, 22, 21, 13, 14, 23, 13, 23, 22, 14, 15, 24, 14, 24, 23, 15, 16, 25,
                15, 25, 24, 16, 17, 26, 16, 26, 25, 18, 19, 28, 18, 28, 27, 19, 20, 29, 19, 29,
                28, 20, 21, 30, 20, 30, 29, 21, 22, 31, 21, 31, 30, 22, 23, 32, 22, 32, 31, 23,
                24, 33, 23, 33, 32, 24, 25, 34, 24, 34, 33, 25, 26, 35, 25, 35, 34, 27, 28, 37,
                27, 37, 36, 28, 29, 38, 28, 38, 37, 29, 30, 39, 29, 39, 38, 30, 31, 40, 30, 40,
                39, 31, 32, 41, 31, 41, 40, 32, 33, 42, 32, 42, 41, 33, 34, 43, 33, 43, 42, 34,
                35, 44, 34, 44, 43, 36, 37, 46, 36, 46, 45, 37, 38, 47, 37, 47, 46, 38, 39, 48,
                38, 48, 47, 39, 40, 49, 39, 49, 48, 40, 41, 50, 40, 50, 49, 41, 42, 51, 41, 51,
                50, 42, 43, 52, 42, 52, 51, 43, 44, 53, 43, 53, 52, 45, 46, 55, 45, 55, 54, 46,
                47, 56, 46, 56, 55, 47, 48, 57, 47, 57, 56, 48, 49, 58, 48, 58, 57, 49, 50, 59,
                49, 59, 58, 50, 51, 60, 50, 60, 59, 51, 52, 61, 51, 61, 60, 52, 53, 62, 52, 62,
                61, 54, 55, 64, 54, 64, 63, 55, 56, 65, 55, 65, 64, 56, 57, 66, 56, 66, 65, 57,
                58, 67, 57, 67, 66, 58, 59, 68, 58, 68, 67, 59, 60, 69, 59, 69, 68, 60, 61, 70,
                60, 70, 69, 61, 62, 71, 61, 71, 70, 63, 64, 73, 63, 73, 72, 64, 65, 74, 64, 74,
                73, 65, 66, 75, 65, 75, 74, 66, 67, 76, 66, 76, 75, 67, 68, 77, 67, 77, 76, 68,
                69, 78, 68, 78, 77, 69, 70, 79, 69, 79, 78, 70, 71, 80, 70, 80, 79
            };
            private static readonly UInt32[] Indices4 = new UInt32[]
            { 
                0, 1, 18, 0, 18, 17, 1, 2, 19, 1, 19, 18, 2, 3, 20, 2, 20, 19, 3, 4, 21, 3, 21,
                20, 4, 5, 22, 4, 22, 21, 5, 6, 23, 5, 23, 22, 6, 7, 24, 6, 24, 23, 7, 8, 25, 7,
                25, 24, 8, 9, 26, 8, 26, 25, 9, 10, 27, 9, 27, 26, 10, 11, 28, 10, 28, 27, 11,
                12, 29, 11, 29, 28, 12, 13, 30, 12, 30, 29, 13, 14, 31, 13, 31, 30, 14, 15, 32,
                14, 32, 31, 15, 16, 33, 15, 33, 32, 17, 18, 35, 17, 35, 34, 18, 19, 36, 18, 36,
                35, 19, 20, 37, 19, 37, 36, 20, 21, 38, 20, 38, 37, 21, 22, 39, 21, 39, 38, 22,
                23, 40, 22, 40, 39, 23, 24, 41, 23, 41, 40, 24, 25, 42, 24, 42, 41, 25, 26, 43, 
                25, 43, 42, 26, 27, 44, 26, 44, 43, 27, 28, 45, 27, 45, 44, 28, 29, 46, 28, 46,
                45, 29, 30, 47, 29, 47, 46, 30, 31, 48, 30, 48, 47, 31, 32, 49, 31, 49, 48, 32,
                33, 50, 32, 50, 49, 34, 35, 52, 34, 52, 51, 35, 36, 53, 35, 53, 52, 36, 37, 54,
                36, 54, 53, 37, 38, 55, 37, 55, 54, 38, 39, 56, 38, 56, 55, 39, 40, 57, 39, 57, 
                56, 40, 41, 58, 40, 58, 57, 41, 42, 59, 41, 59, 58, 42, 43, 60, 42, 60, 59, 43, 
                44, 61, 43, 61, 60, 44, 45, 62, 44, 62, 61, 45, 46, 63, 45, 63, 62, 46, 47, 64, 
                46, 64, 63, 47, 48, 65, 47, 65, 64, 48, 49, 66, 48, 66, 65, 49, 50, 67, 49, 67, 
                66, 51, 52, 69, 51, 69, 68, 52, 53, 70, 52, 70, 69, 53, 54, 71, 53, 71, 70, 54, 
                55, 72, 54, 72, 71, 55, 56, 73, 55, 73, 72, 56, 57, 74, 56, 74, 73, 57, 58, 75, 
                57, 75, 74, 58, 59, 76, 58, 76, 75, 59, 60, 77, 59, 77, 76, 60, 61, 78, 60, 78,
                77, 61, 62, 79, 61, 79, 78, 62, 63, 80, 62, 80, 79, 63, 64, 81, 63, 81, 80, 64,
                65, 82, 64, 82, 81, 65, 66, 83, 65, 83, 82, 66, 67, 84, 66, 84, 83, 68, 69, 86, 
                68, 86, 85, 69, 70, 87, 69, 87, 86, 70, 71, 88, 70, 88, 87, 71, 72, 89, 71, 89,
                88, 72, 73, 90, 72, 90, 89, 73, 74, 91, 73, 91, 90, 74, 75, 92, 74, 92, 91, 75, 
                76, 93, 75, 93, 92, 76, 77, 94, 76, 94, 93, 77, 78, 95, 77, 95, 94, 78, 79, 96, 
                78, 96, 95, 79, 80, 97, 79, 97, 96, 80, 81, 98, 80, 98, 97, 81, 82, 99, 81, 99, 
                98, 82, 83, 100, 82, 100, 99, 83, 84, 101, 83, 101, 100, 85, 86, 103, 85, 103, 
                102, 86, 87, 104, 86, 104, 103, 87, 88, 105, 87, 105, 104, 88, 89, 106, 88, 106,
                105, 89, 90, 107, 89, 107, 106, 90, 91, 108, 90, 108, 107, 91, 92, 109, 91, 109,
                108, 92, 93, 110, 92, 110, 109, 93, 94, 111, 93, 111, 110, 94, 95, 112, 94, 112,
                111, 95, 96, 113, 95, 113, 112, 96, 97, 114, 96, 114, 113, 97, 98, 115, 97, 115
                , 114, 98, 99, 116, 98, 116, 115, 99, 100, 117, 99, 117, 116, 100, 101, 118, 100
                , 118, 117, 102, 103, 120, 102, 120, 119, 103, 104, 121, 103, 121, 120, 104, 105
                , 122, 104, 122, 121, 105, 106, 123, 105, 123, 122, 106, 107, 124, 106, 124, 123
                , 107, 108, 125, 107, 125, 124, 108, 109, 126, 108, 126, 125, 109, 110, 127, 109
                , 127, 126, 110, 111, 128, 110, 128, 127, 111, 112, 129, 111, 129, 128, 112, 113
                , 130, 112, 130, 129, 113, 114, 131, 113, 131, 130, 114, 115, 132, 114, 132, 131
                , 115, 116, 133, 115, 133, 132, 116, 117, 134, 116, 134, 133, 117, 118, 135, 117
                , 135, 134, 119, 120, 137, 119, 137, 136, 120, 121, 138, 120, 138, 137, 121, 122
                , 139, 121, 139, 138, 122, 123, 140, 122, 140, 139, 123, 124, 141, 123, 141, 140
                , 124, 125, 142, 124, 142, 141, 125, 126, 143, 125, 143, 142, 126, 127, 144, 126
                , 144, 143, 127, 128, 145, 127, 145, 144, 128, 129, 146, 128, 146, 145, 129, 130
                , 147, 129, 147, 146, 130, 131, 148, 130, 148, 147, 131, 132, 149, 131, 149, 148
                , 132, 133, 150, 132, 150, 149, 133, 134, 151, 133, 151, 150, 134, 135, 152, 134
                , 152, 151, 136, 137, 154, 136, 154, 153, 137, 138, 155, 137, 155, 154, 138, 139
                , 156, 138, 156, 155, 139, 140, 157, 139, 157, 156, 140, 141, 158, 140, 158, 157
                , 141, 142, 159, 141, 159, 158, 142, 143, 160, 142, 160, 159, 143, 144, 161, 143
                , 161, 160, 144, 145, 162, 144, 162, 161, 145, 146, 163, 145, 163, 162, 146, 147
                , 164, 146, 164, 163, 147, 148, 165, 147, 165, 164, 148, 149, 166, 148, 166, 165
                , 149, 150, 167, 149, 167, 166, 150, 151, 168, 150, 168, 167, 151, 152, 169, 151
                , 169, 168, 153, 154, 171, 153, 171, 170, 154, 155, 172, 154, 172, 171, 155, 156
                , 173, 155, 173, 172, 156, 157, 174, 156, 174, 173, 157, 158, 175, 157, 175, 174
                , 158, 159, 176, 158, 176, 175, 159, 160, 177, 159, 177, 176, 160, 161, 178, 160
                , 178, 177, 161, 162, 179, 161, 179, 178, 162, 163, 180, 162, 180, 179, 163, 164
                , 181, 163, 181, 180, 164, 165, 182, 164, 182, 181, 165, 166, 183, 165, 183, 182
                , 166, 167, 184, 166, 184, 183, 167, 168, 185, 167, 185, 184, 168, 169, 186, 168
                , 186, 185, 170, 171, 188, 170, 188, 187, 171, 172, 189, 171, 189, 188, 172, 173
                , 190, 172, 190, 189, 173, 174, 191, 173, 191, 190, 174, 175, 192, 174, 192, 191
                , 175, 176, 193, 175, 193, 192, 176, 177, 194, 176, 194, 193, 177, 178, 195, 177
                , 195, 194, 178, 179, 196, 178, 196, 195, 179, 180, 197, 179, 197, 196, 180, 181
                , 198, 180, 198, 197, 181, 182, 199, 181, 199, 198, 182, 183, 200, 182, 200, 199
                , 183, 184, 201, 183, 201, 200, 184, 185, 202, 184, 202, 201, 185, 186, 203, 185
                , 203, 202, 187, 188, 205, 187, 205, 204, 188, 189, 206, 188, 206, 205, 189, 190
                , 207, 189, 207, 206, 190, 191, 208, 190, 208, 207, 191, 192, 209, 191, 209, 208
                , 192, 193, 210, 192, 210, 209, 193, 194, 211, 193, 211, 210, 194, 195, 212, 194
                , 212, 211, 195, 196, 213, 195, 213, 212, 196, 197, 214, 196, 214, 213, 197, 198
                , 215, 197, 215, 214, 198, 199, 216, 198, 216, 215, 199, 200, 217, 199, 217, 216
                , 200, 201, 218, 200, 218, 217, 201, 202, 219, 201, 219, 218, 202, 203, 220, 202
                , 220, 219, 204, 205, 222, 204, 222, 221, 205, 206, 223, 205, 223, 222, 206, 207
                , 224, 206, 224, 223, 207, 208, 225, 207, 225, 224, 208, 209, 226, 208, 226, 225
                , 209, 210, 227, 209, 227, 226, 210, 211, 228, 210, 228, 227, 211, 212, 229, 211
                , 229, 228, 212, 213, 230, 212, 230, 229, 213, 214, 231, 213, 231, 230, 214, 215
                , 232, 214, 232, 231, 215, 216, 233, 215, 233, 232, 216, 217, 234, 216, 234, 233
                , 217, 218, 235, 217, 235, 234, 218, 219, 236, 218, 236, 235, 219, 220, 237, 219
                , 237, 236, 221, 222, 239, 221, 239, 238, 222, 223, 240, 222, 240, 239, 223, 224
                , 241, 223, 241, 240, 224, 225, 242, 224, 242, 241, 225, 226, 243, 225, 243, 242
                , 226, 227, 244, 226, 244, 243, 227, 228, 245, 227, 245, 244, 228, 229, 246, 228
                , 246, 245, 229, 230, 247, 229, 247, 246, 230, 231, 248, 230, 248, 247, 231, 232
                , 249, 231, 249, 248, 232, 233, 250, 232, 250, 249, 233, 234, 251, 233, 251, 250
                , 234, 235, 252, 234, 252, 251, 235, 236, 253, 235, 253, 252, 236, 237, 254, 236
                , 254, 253, 238, 239, 256, 238, 256, 255, 239, 240, 257, 239, 257, 256, 240, 241
                , 258, 240, 258, 257, 241, 242, 259, 241, 259, 258, 242, 243, 260, 242, 260, 259
                , 243, 244, 261, 243, 261, 260, 244, 245, 262, 244, 262, 261, 245, 246, 263, 245
                , 263, 262, 246, 247, 264, 246, 264, 263, 247, 248, 265, 247, 265, 264, 248, 249
                , 266, 248, 266, 265, 249, 250, 267, 249, 267, 266, 250, 251, 268, 250, 268, 267
                , 251, 252, 269, 251, 269, 268, 252, 253, 270, 252, 270, 269, 253, 254, 271, 253
                , 271, 270, 255, 256, 273, 255, 273, 272, 256, 257, 274, 256, 274, 273, 257, 258
                , 275, 257, 275, 274, 258, 259, 276, 258, 276, 275, 259, 260, 277, 259, 277, 276
                , 260, 261, 278, 260, 278, 277, 261, 262, 279, 261, 279, 278, 262, 263, 280, 262
                , 280, 279, 263, 264, 281, 263, 281, 280, 264, 265, 282, 264, 282, 281, 265, 266
                , 283, 265, 283, 282, 266, 267, 284, 266, 284, 283, 267, 268, 285, 267, 285, 284
                , 268, 269, 286, 268, 286, 285, 269, 270, 287, 269, 287, 286, 270, 271, 288, 270
                , 288, 287
            };

            // Vertices & Indices
            private static readonly uint[][] MagicVertices = new uint[][] { Vertices0, Vertices1, Vertices2, Vertices3, Vertices4 };
            private static readonly UInt32[][] MagicIndices = new UInt32[][] { Indices0, Indices1, Indices2, Indices3, Indices4 };
            private static readonly int[] MagicBase = new int[] { 1, 2, 4, 8, 16 };
            private static readonly int[] MagicLength = new int[] { 16, 8, 4, 2, 1 };

            #endregion
        }
        #endregion
        
        #region Field
        /// <summary>
        /// Base game
        /// </summary>
        protected BaseGame game;

        /// <summary>
        /// Graphics device
        /// </summary>
        protected GraphicsDevice graphics;

        /// <summary>
        /// Gets terrain size (x, y, z)
        /// </summary>
        public Vector3 Size => size;

        private Vector3 size;

        /// <summary>
        /// Gets the heightfield data of the landscape
        /// </summary>
        public float[,] HeightField => heightField;

        private float[,] heightField;

        /// <summary>
        /// Gets the normal field of the landscape
        /// </summary>
        public Vector3[,] NormalField => normalField;

        private Vector3[,] normalField;

        /// <summary>
        /// Gets the tangent field of the landscape
        /// </summary>
        public Vector3[,] TangentField => tangentData;

        private Vector3[,] tangentData;

        /// <summary>
        /// Gets the terrain bounding box
        /// </summary>
        public BoundingBox TerrainBoundingBox => terrainBoundingBox;

        private BoundingBox terrainBoundingBox;

        /// <summary>
        /// Gets the number of patches on the x axis
        /// </summary>
        public int PatchCountOnXAxis => xPatchCount;

        /// <summary>
        /// Gets the number of patches on the y axis
        /// </summary>
        public int PatchCountOnYAxis => yPatchCount;

        /// <summary>
        /// Describes the patch count
        /// </summary>
        private int xPatchCount, yPatchCount;
        private List<Layer> layers = new();

        /// <summary>
        /// All terrain layers
        /// </summary>
        public List<Layer> Layers => layers;

        /// <summary>
        /// Patch groups
        /// </summary>
        public List<int>[] PatchGroups => patchGroups;

        private List<int>[] patchGroups;

        /// <summary>
        /// Gets terrain patches
        /// </summary>
        public List<Patch> Patches => patches;

        private List<Patch> patches;

        /// <summary>
        /// Describes the size of heightfield
        /// </summary>
        private int gridCountOnXAxis, gridCountOnYAxis;

        /// <summary>
        /// Gets the width of grid
        /// </summary>
        public int GridCountOnXAxis => gridCountOnXAxis;

        /// <summary>
        /// Gets the height of grid
        /// </summary>
        public int GridCountOnYAxis => gridCountOnYAxis;
        #endregion

        #region Method

        public virtual void ReadContent(ContentReader input)
        {
            // Size info
            size.X = input.ReadSingle();
            size.Y = input.ReadSingle();
            size.Z = input.ReadSingle();

            terrainBoundingBox = new BoundingBox(Vector3.Zero, size);

            // Heightfield
            gridCountOnXAxis = input.ReadInt32();
            gridCountOnYAxis = input.ReadInt32();
            heightField = new float[gridCountOnXAxis, gridCountOnYAxis];
            for (var y = 0; y < gridCountOnYAxis; y++)
            {
                for (var x = 0; x < gridCountOnXAxis; x++)
                {
                    // Remember how we write heighfield data
                    heightField[x, y] = input.ReadSingle();

                    // TEST: Lower vertices under water
                    if (heightField[x, y] < 0)
                    {
                        heightField[x, y] *= 1.4f;
                    }
                }
            }

            // Normals
            normalField = new Vector3[gridCountOnXAxis, gridCountOnYAxis];
            for (var y = 0; y < gridCountOnYAxis; y++)
            {
                for (var x = 0; x < gridCountOnXAxis; x++)
                {
                    normalField[x, y] = input.ReadVector3();
                }
            }

            // Tangents
            tangentData = new Vector3[GridCountOnXAxis, GridCountOnYAxis];
            for (var y = 0; y < GridCountOnYAxis; y++)
            {
                for (var x = 0; x < GridCountOnXAxis; x++)
                {
                    tangentData[x, y] = input.ReadVector3();
                }
            }

            // Patches
            xPatchCount = input.ReadInt32();
            yPatchCount = input.ReadInt32();
            patches = new List<Patch>(PatchCountOnXAxis * PatchCountOnYAxis);
            for (var i = 0; i < PatchCountOnXAxis * PatchCountOnYAxis; i++)
            {
                patches.Add(new Patch(input, i, this));
            }

            // Patch groups
            var patchGroupCount = input.ReadInt32();
            patchGroups = new List<int>[patchGroupCount];
            for (var i = 0; i < patchGroupCount; i++)
            {
                var n = input.ReadInt32();
                patchGroups[i] = new List<int>(n);
                for (var k = 0; k < n; k++)
                {
                    patchGroups[i].Add(input.ReadInt32());
                }
            }

            // Layers
            var layerCount = input.ReadInt32();
            layers = new List<Layer>(layerCount);
            for (var i = 0; i < layerCount; i++)
            {
                layers.Add(new Layer(input));
            }
        }

        public virtual void Initialize(BaseGame game)
        {
            this.game = game;
            graphics = game.GraphicsDevice;
        }

        /// <summary>
        /// Gets a given patch
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Patch GetPatch(int x, int y)
        {
            return patches[y * PatchCountOnXAxis + x];
        }

        /// <summary>
        /// Transform from heightfield grid to real world position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector2 GridToPosition(int x, int y)
        {
            return new Vector2(
                x * size.X / (gridCountOnXAxis - 1),
                y * size.Y / (gridCountOnYAxis - 1));
        }

        public Point PositionToGrid(float x, float y)
        {
            return new Point(
                (int)(x * (gridCountOnXAxis - 1) / size.X),
                (int)(y * (gridCountOnYAxis - 1) / size.Y));
        }

        public Point GridCount => new Point(gridCountOnXAxis, gridCountOnYAxis);

        /// <summary>
        /// Gets the landscape size.Z of a given point on the heightfield
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetGridHeight(int x, int y)
        {
            return heightField[x, y];
        }

        /// <summary>
        /// Gets whether the point is walkable (E.g., above water)
        /// </summary>
        public bool IsPointOccluded(float x, float y)
        {
            return GetHeight(x, y) < -4;
        }

        /// <summary>
        /// Gets the landscape size.Z of any given point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetHeight(float x, float y)
        {
            // Grabbed and modified from racing game
            // We don't want to cause any exception here
            if (x < 0)
            {
                x = 0;
            }
            else if (x >= size.X)
            {
                x = size.X - 1;  // x can't be heightfieldWidth-1
            }
            // or there'll be an out of range
            if (y < 0)          // exception. So is y.
            {
                y = 0;
            }
            else if (y >= size.Y)
            {
                y = size.Y - 1;
            }

            // Rescale to our heightfield dimensions
            x *= (gridCountOnXAxis - 1) / size.X;
            y *= (gridCountOnYAxis - 1) / size.Y;

            // Get the position ON the current tile (0.0-1.0)!!!
            float
                fX = x - ((float)((int)x)),
                fY = y - ((float)((int)y));

            // Interpolate the current position
            var ix2 = (int)x;
            var iy2 = (int)y;

            var ix1 = ix2 + 1;
            var iy1 = iy2 + 1;

            if (fX + fY > 1) // opt. version
            {
                // we are on triangle 1 !!
                //  0____1
                //   \   |
                //    \  |
                //     \ |
                //      \|
                //  2    3
                return
                    heightField[ix1, iy1] + // 1
                    (1.0f - fX) * (heightField[ix2, iy1] - heightField[ix1, iy1]) + // 0
                    (1.0f - fY) * (heightField[ix1, iy2] - heightField[ix1, iy1]); // 3
            }
            // we are on triangle 1 !!
            //  0     1
            //  |\  
            //  | \ 
            //  |  \ 
            //  |   \
            //  |    \
            //  2_____3
            var height =
                heightField[ix2, iy2] + // 2
                fX * (heightField[ix1, iy2] - heightField[ix2, iy2]) +    // 3
                fY * (heightField[ix2, iy1] - heightField[ix2, iy2]); // 0

            // For those area underwater, we set the height to zero
            return height;// < 0 ? 0 : height;
        }

        /// <summary>
        /// Gets the normal of a grid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector3 GetNormal(int x, int y)
        {
            return normalField[x, y];
        }

        /// <summary>
        /// Gets the normal of the terrain from a world position. (normalized)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector3 GetNormal(float x, float y)
        {
            // We don't want to cause any exception here
            if (x < 0)
            {
                x = 0;
            }
            else if (x >= size.X)
            {
                x = size.X - 1;     // x can't be heightfieldWidth-1
            }
            // or there'll be an out of range
            if (y < 0)              // exception. So is y.
            {
                y = 0;
            }
            else if (y >= size.Y)
            {
                y = size.Y - 1;
            }

            // Rescale to our heightfield dimensions
            x *= (gridCountOnXAxis - 1) / size.X;
            y *= (gridCountOnYAxis - 1) / size.Y;

            // Get the position ON the current tile (0.0-1.0)!!!
            float
                fX = x - ((float)((int)x)),
                fY = y - ((float)((int)y));

            // Interpolate the current position
            var ix2 = (int)x;
            var iy2 = (int)y;

            var ix1 = ix2 + 1;
            var iy1 = iy2 + 1;

            // Perform bilinear interpolation instead of spliting triangles :)
            var a = Vector3.Lerp(normalField[ix2, iy2], normalField[ix2, iy1], fY);
            var b = Vector3.Lerp(normalField[ix1, iy2], normalField[ix1, iy1], fY);

            return Vector3.Lerp(a, b, fX);
        }

        /// <summary>
        /// Cached pick position. Remember to set it to null each frame.
        /// </summary>
        private Vector3? picked = null;

        /// <summary>
        /// Gets the current point of the terrain picked by the cursor
        /// </summary>
        public Vector3? Pick()
        {
            return picked != null ? picked : Intersects(game.PickRay);
        }

        /// <summary>
        /// Checks whether a ray intersects the terrain mesh
        /// </summary>
        /// <remarks>
        /// This algorithm has some bugs right now... :(
        /// </remarks>
        /// <param name="ray"></param>
        /// <returns>Intersection point or null if there's no intersection</returns>
        private Vector3? Intersects(Ray ray, ref List<VertexPositionColor> track)
        {
            // Get two vertices to draw a line through the
            // heightfield.
            //
            // 1. Project the ray to XY plane
            // 2. Compute the 2 intersections of the ray and
            //    terrain bounding box (Projected)
            // 3. Find the 2 points to draw
            var i = 0;
            var points = new Vector3[2];

            // Line equation: y = k * (x - x0) + y0
            var k = ray.Direction.Y / ray.Direction.X;
            var invK = ray.Direction.X / ray.Direction.Y;
            var r = ray.Position.Y - ray.Position.X * k;
            if (r >= 0 && r <= size.Y)
            {
                points[i++] = new Vector3(0, r,
                    ray.Position.Z - ray.Position.X *
                    ray.Direction.Z / ray.Direction.X);
            }
            r = ray.Position.Y + (size.X - ray.Position.X) * k;
            if (r >= 0 && r <= size.Y)
            {
                points[i++] = new Vector3(size.X, r,
                    ray.Position.Z + (size.X - ray.Position.X) *
                    ray.Direction.Z / ray.Direction.X);
            }
            if (i < 2)
            {
                r = ray.Position.X - ray.Position.Y * invK;
                if (r >= 0 && r <= size.X)
                {
                    points[i++] = new Vector3(r, 0,
                        ray.Position.Z - ray.Position.Y *
                        ray.Direction.Z / ray.Direction.Y);
                }
            }
            if (i < 2)
            {
                r = ray.Position.X + (size.Y - ray.Position.Y) * invK;
                if (r >= 0 && r <= size.X)
                {
                    points[i++] = new Vector3(r, size.Y,
                        ray.Position.Z + (size.Y - ray.Position.Y) *
                        ray.Direction.Z / ray.Direction.Y);
                }
            }
            if (i < 2)
            {
                return null;
            }

            // When ray position is inside the box, it should be one
            // of the starting point
            var inside = ray.Position.X > 0 && ray.Position.X < size.X &&
                          ray.Position.Y > 0 && ray.Position.Y < size.Y;

            Vector3 v1 = Vector3.Zero, v2 = Vector3.Zero;
            // Sort the 2 points to make the line follow the direction
            if (ray.Direction.X > 0)
            {
                if (points[0].X < points[1].X)
                {
                    v2 = points[1];
                    v1 = inside ? ray.Position : points[0];
                }
                else
                {
                    v2 = points[0];
                    v1 = inside ? ray.Position : points[1];
                }
            }
            else if (ray.Direction.X < 0)
            {
                if (points[0].X > points[1].X)
                {
                    v2 = points[1];
                    v1 = inside ? ray.Position : points[0];
                }
                else
                {
                    v2 = points[0];
                    v1 = inside ? ray.Position : points[1];
                }
            }

            //Log.NewLine();
            //Log.Write("v1: " + v1, false);
            //Log.Write("v2: " + v2, false);

            // FIXME: If direction.x equals 0, this algorithm fails.
            //        These 2 cases are Really Really Really unusual,
            //        but we have to add them any way :(

            // Perform a Bresenham line drawing algorithm on
            // the heightfield so that only the points on the
            // heightfield is tested. Interpolation is avoided.

            Point p1 = PositionToGrid(v1.X, v1.Y);
            Point p2 = PositionToGrid(v2.X, v2.Y);

            var invert = false;
            var x = p1.X;
            var y = p1.Y;
            var sx = p2.X - p1.X;
            var sy = p2.Y - p1.Y;
            var dx = Math.Abs(sx);
            var dy = Math.Abs(sy);

            sx = (sx != 0) ? (sx > 0 ? 1 : -1) : 0;
            sy = (sy != 0) ? (sy > 0 ? 1 : -1) : 0;

            if (dy > dx)
            {
                // Swap dx, dy
                var t = dx;
                dx = dy;
                dy = t;
                invert = true;
            }

            // Init error term
            var e = (dy << 1) - dx;

            var n = dx;
            dx = dx * 2;
            dy = dy * 2;

            // Compute z and dz
            float z = v1.Z, dz;
            //float dz = ray.Direction.Z *
            //    (invert ? size.X / (heightfieldWidth - 1) :
            //              size.Y / (heightfieldHeight - 1)) /
            //    (new Vector2(ray.Direction.X, ray.Direction.Y).Length());
            if (invert)
            {
                var v = Vector2.Normalize(new Vector2(ray.Direction.X, ray.Direction.Z));
                v /= v.X;
                dz = v.Y * size.X / (gridCountOnXAxis - 1) / v.X;
            }
            else
            {
                var v = Vector2.Normalize(new Vector2(ray.Direction.Y, ray.Direction.Z));
                v /= v.X;
                dz = v.Y * size.Y / (gridCountOnYAxis - 1) / v.X;
            }

            // Start drawing pixels
            for (i = 0; i < n; ++i)
            {
                // Don't test bounding vertices to ease the generation
                // of precise intersection point :)
                if (x > 0 && x < gridCountOnXAxis - 1 &&
                    y > 0 && y < gridCountOnYAxis - 1)
                {
                    track.Add(new VertexPositionColor(new Vector3(
                        GridToPosition(x, y), heightField[x, y]), Color.White));
                    //Log.Write("x: " + x + "\ty: " + y + "\tz: " + z + "\theight: " + heightfield[x, y], false);

                    // Test a pixel
                    if (heightField[x, y] >= z)
                    {
                        // Find the first intersection, we
                        // need a precise value of the position
                        var v = new Vector3[4];

                        var min = new Point();
                        var max = new Point();

                        var xDirection = new int[] { -1, 0, -1, 0 };
                        var yDirection = new int[] { -1, -1, 0, 0 };

                        Point grid;
                        Vector3 ret;
                        for (var m = 0; m < 4; m++)
                        {
                            min.X = x + xDirection[m];
                            min.Y = y + yDirection[m];
                            max.X = min.X + 1;
                            max.Y = min.Y + 1;

                            v[0] = new Vector3(
                                GridToPosition(min.X, min.Y), heightField[min.X, min.Y]);
                            v[1] = new Vector3(
                                GridToPosition(max.X, min.Y), heightField[max.X, min.Y]);
                            v[2] = new Vector3(
                                GridToPosition(min.X, max.Y), heightField[min.X, max.Y]);
                            v[3] = new Vector3(
                                GridToPosition(max.X, max.Y), heightField[max.X, max.Y]);

                            // Test the first triangles
                            var plane = new Plane(v[0], v[1], v[3]);
                            var result = ray.Intersects(plane);
                            if (result != null)
                            {
                                ret = ray.Position + result.Value * ray.Direction;
                                grid = PositionToGrid(ret.X, ret.Y);

                                //Log.Write("Intersection: " + grid + "min: " + min + "max: " + max, false);

                                if (grid.X == min.X || grid.Y == min.Y)
                                {
                                    return ret;
                                }
                            }

                            // Test the second triangle
                            plane = new Plane(v[0], v[3], v[2]);
                            result = ray.Intersects(plane);
                            if (result != null)
                            {
                                ret = ray.Position + result.Value * ray.Direction;
                                grid = PositionToGrid(ret.X, ret.Y);

                                if (grid.X == min.X || grid.Y == min.Y)
                                {
                                    return ret;
                                }
                            }
                        }

                        // Any way, return an approximate value
                        //Log.Write(ray.ToString());
                        return new Vector3(GridToPosition(x, y), heightField[x, y]);
                    }
                }

                while (e > 0)
                {
                    if (invert)
                    {
                        x = x + sx;
                    }
                    else
                    {
                        y = y + sy;
                    }

                    e = e - dx;
                }

                if (invert)
                {
                    y = y + sy;
                }
                else
                {
                    x = x + sx;
                }

                e = e + dy;
                z += dz;
            }

            return null;
        }

        /// <summary>
        /// Checks whether a ray intersects the terrain mesh
        /// </summary>
        /// <param name="ray"></param>
        /// <returns>Intersection point or null if there's no intersection</returns>
        public Vector3? Intersects(Ray ray)
        {
            // Normalize ray direction
            ray.Direction.Normalize();

            // Get two vertices to draw a line through the
            // heightfield.
            //
            // 1. Project the ray to XY plane
            // 2. Compute the 2 intersections of the ray and
            //    terrain bounding box (Projected)
            // 3. Find the 2 points to draw
            var i = 0;
            var points = new Vector3[2];

            // Line equation: y = k * (x - x0) + y0
            var k = ray.Direction.Y / ray.Direction.X;
            var invK = ray.Direction.X / ray.Direction.Y;
            var r = ray.Position.Y - ray.Position.X * k;
            if (r >= 0 && r <= size.Y)
            {
                points[i++] = new Vector3(0, r,
                    ray.Position.Z - ray.Position.X *
                    ray.Direction.Z / ray.Direction.X);
            }
            r = ray.Position.Y + (size.X - ray.Position.X) * k;
            if (r >= 0 && r <= size.Y)
            {
                points[i++] = new Vector3(size.X, r,
                    ray.Position.Z + (size.X - ray.Position.X) *
                    ray.Direction.Z / ray.Direction.X);
            }
            if (i < 2)
            {
                r = ray.Position.X - ray.Position.Y * invK;
                if (r >= 0 && r <= size.X)
                {
                    points[i++] = new Vector3(r, 0,
                        ray.Position.Z - ray.Position.Y *
                        ray.Direction.Z / ray.Direction.Y);
                }
            }
            if (i < 2)
            {
                r = ray.Position.X + (size.Y - ray.Position.Y) * invK;
                if (r >= 0 && r <= size.X)
                {
                    points[i++] = new Vector3(r, size.Y,
                        ray.Position.Z + (size.Y - ray.Position.Y) *
                        ray.Direction.Z / ray.Direction.Y);
                }
            }
            if (i < 2)
            {
                return null;
            }

            // When ray position is inside the box, it should be one
            // of the starting point
            var inside = ray.Position.X > 0 && ray.Position.X < size.X &&
                          ray.Position.Y > 0 && ray.Position.Y < size.Y;

            Vector3 v1 = Vector3.Zero, v2 = Vector3.Zero;
            // Sort the 2 points to make the line follow the direction
            if (ray.Direction.X > 0)
            {
                if (points[0].X < points[1].X)
                {
                    v2 = points[1];
                    v1 = inside ? ray.Position : points[0];
                }
                else
                {
                    v2 = points[0];
                    v1 = inside ? ray.Position : points[1];
                }
            }
            else if (ray.Direction.X < 0)
            {
                if (points[0].X > points[1].X)
                {
                    v2 = points[1];
                    v1 = inside ? ray.Position : points[0];
                }
                else
                {
                    v2 = points[0];
                    v1 = inside ? ray.Position : points[1];
                }
            }

            // Trace steps along your line and determine the size.Z at each point,
            // for each sample point look up the size.Z of the terrain and determine
            // if the point on the line is above or below the terrain. Once you have
            // determined the two sampling points that are above and below the terrain
            // you can refine using binary searching.
            const float SamplePrecision = 5.0f;
            const int RefineSteps = 5;

            var length = Vector3.Subtract(v2, v1).Length();
            float current = 0;

            var point = new Vector3[2];
            Vector3 step = ray.Direction * SamplePrecision;
            point[0] = v1;

            while (current < length)
            {
                if (GetHeight(point[0].X, point[0].Y) >= point[0].Z)
                {
                    break;
                }

                point[0] += step;
                current += SamplePrecision;
            }

            if (current > 0 && current < length)
            {
                // Perform binary search

                Vector3 p = point[0];
                point[1] = point[0] - step;

                for (i = 0; i < RefineSteps; i++)
                {
                    p = (point[0] + point[1]) * 0.5f;

                    if (GetHeight(p.X, p.Y) >= p.Z)
                    {
                        point[0] = p;
                    }
                    else
                    {
                        point[1] = p;
                    }
                }

                return p;
            }

            return null;
        } 

        #endregion

        #region Update & Draw

        /// <summary>
        /// Update landscape every frame
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime)
        {
            // Set picked to null
            picked = null;
        }

        public abstract void Draw(GameTime gameTime);
        #endregion
    }
    #endregion
}
