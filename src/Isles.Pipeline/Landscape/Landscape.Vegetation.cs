//-----------------------------------------------------------------------------
//  Isles v1.0
//
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Isles.Pipeline
{
    /// <summary>
    /// Custom content processor adds randomly positioned billboards on the
    /// surface of the input mesh. This technique can be used to scatter
    /// grass billboards across a landscape model.
    /// </summary>
    public partial class Landscape
    {
        private Random random = new Random();
        private List<Vector2>[] grassSize;
        private List<Vector2>[] grassPosition;
        private ExternalReference<TextureContent>[] grassTexture;

        /// <summary>
        /// Represents a vegetation layer in the terrain
        /// </summary>
        [Serializable()]
        public class Vegetation
        {
            public string Texture = "";
            public string Distribution = "";
            public int Width;
            public int Height;
            public int WindAmount;
            public int MaxCountPerGrid;
        }

        private void ProcessVegetation(ContentProcessorContext context)
        {
            grassSize = new List<Vector2>[vegetations.Count];
            grassPosition = new List<Vector2>[vegetations.Count];
            grassTexture = new ExternalReference<TextureContent>[vegetations.Count];

            //System.Diagnostics.Debugger.Launch();

            for (int i = 0; i < vegetations.Count; i++ )
            {
                grassSize[i] = new List<Vector2>();
                grassPosition[i] = new List<Vector2>();

                // Load vegetation distribution map
                Texture2DContent visibility = (Texture2DContent)
                    context.BuildAndLoadAsset<TextureContent, TextureContent>
                        (new ExternalReference<TextureContent>(Path.Combine(directory,
                            vegetations[i].Distribution)), "TextureProcessor");

                // Convert to pixel bitmap
                visibility.ConvertBitmapType(typeof(PixelBitmapContent<float>));
                PixelBitmapContent<float> pixmap = (PixelBitmapContent<float>)visibility.Mipmaps[0];

                Vector2 position;
                int xCells = heightFieldWidth - 1;
                int yCells = heightFieldHeight - 1;

                for (int y = 0; y < yCells; y++)
                {
                    for (int x = 0; x < xCells; x++)
                    {
                        int n = random.Next((int)(vegetations[i].MaxCountPerGrid *
                            pixmap.GetPixel(x * pixmap.Width / xCells, y * pixmap.Height / yCells)));

                        for (int k = 0; k < n; k++)
                        {
                            position.X = width * x / xCells + (float)random.NextDouble() * width / xCells;
                            position.Y = depth * y / yCells + (float)random.NextDouble() * depth / yCells;

                            grassPosition[i].Add(position);
                            grassSize[i].Add(new Vector2(
                                vegetations[i].Width * (float)(0.5 + random.NextDouble()),
                                vegetations[i].Height * (float)(0.5 + random.NextDouble())));
                        }
                    }
                }

                // Add texture
                grassTexture[i] = context.BuildAsset<TextureContent, TextureContent>(
                    new ExternalReference<TextureContent>(Path.Combine(
                        directory, vegetations[i].Texture)), "TextureProcessor");
            }
        }

        private void WriteVegetation(ContentWriter output)
        {
            output.Write(vegetations.Count);

            for (int i = 0; i < vegetations.Count; i++)
            {
                output.WriteExternalReference(grassTexture[i]);
                output.Write(grassPosition[i].Count);

                for (int k = 0; k < grassPosition[i].Count; k++)
                {
                    output.Write(grassPosition[i][k]);
                    output.Write(grassSize[i][k]);
                }
            }
        }

    }
}
