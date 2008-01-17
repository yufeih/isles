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
    public partial class Landscape
    {
        float grassViewDistanceSquared = 400000;
        List<Billboard> vegetations = new List<Billboard>(512);

        void ReadVegetationContent(ContentReader input)
        {
            int count = input.ReadInt32();

            Vector2 position;
            Billboard billboard;
            Texture2D texture;
            for (int i = 0; i < count; i++)
            {
                texture = input.ReadExternalReference<Texture2D>();

                int n = input.ReadInt32();

                for (int k = 0; k < n; k++)
                {
                    billboard.Texture = texture;
                    position = input.ReadVector2();
                    billboard.Position = new Vector3(position, GetHeight(position.X, position.Y));
                    billboard.Size = input.ReadVector2();
                    billboard.Normal = Vector3.UnitZ;
                    billboard.Type = BillboardType.NormalOriented;
                    billboard.SourceRectangle = Billboard.DefaultSourceRectangle;

                    vegetations.Add(billboard);
                }
            }
        }

        void InitializeVegetation()
        {

        }

        void DrawVegetation(GameTime gameTime)
        {
            foreach (Billboard billboard in vegetations)
            {
                if (grassViewDistanceSquared >=
                    Vector3.DistanceSquared(billboard.Position, game.Eye))
                {
                    game.Billboard.Draw(billboard);
                }
            }
        }
    }
}
