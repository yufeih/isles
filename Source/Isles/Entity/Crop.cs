//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;

namespace Isles
{
    #region Crop

    /// <summary>
    /// Respresents a crop in the game
    /// </summary>
    public class Crop : Entity
    {
        const float CropDensity = 0.15f;
        const float BillboardSize = 8;
        const float RandomFactor = 0.5f;

        #region Fields

        /// <summary>
        /// Texture used to draw the crop
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// All crop quads
        /// </summary>
        Billboard[,] billboards;

        #endregion
        
        #region Methods
        /// <summary>
        /// Create a new crop
        /// </summary>
        public Crop(GameWorld world, Vector3 size)
            : base(world)
        {
            // FIXME : Size...

            texture = world.LevelContent.Load<Texture2D>("Textures/Vegetation/Crop2");

            Random random = new Random();

            billboards = new Billboard[
                (int)(Size.X * CropDensity), (int)(Size.Y * CropDensity)];

            int xCount = billboards.GetLength(0);
            int yCount = billboards.GetLength(1);

            Vector3 fieldSize = CropDensity * Size;

            fieldSize.X *= (xCount - 1);
            fieldSize.Y *= (yCount - 1);

            Vector3 basePosition = -Size/2 + (Size - fieldSize) / 2;

            for (int y = 0; y < yCount; y++)
                for (int x = 0; x < xCount; x++)
                {
                    billboards[x, y].Texture = texture;
                    billboards[x, y].Normal = Vector3.UnitZ;
                    billboards[x, y].Type = BillboardType.Vegetation;
                    billboards[x, y].SourceRectangle = Billboard.DefaultSourceRectangle;

                    // Invert some of the billboard
                    if (random.NextDouble() < 0.5)
                        billboards[x, y].SourceRectangle = Billboard.DefaultSourceRectangleXInversed;

                    billboards[x, y].Size.X = BillboardSize * (float)(1 + random.NextDouble() * RandomFactor);
                    billboards[x, y].Size.Y = BillboardSize * (float)(1 + random.NextDouble() * RandomFactor);

                    billboards[x, y].Position.X = Size.X * x * CropDensity + basePosition.X;
                    billboards[x, y].Position.Y = Size.Y * y * CropDensity + basePosition.Y;

                    billboards[x, y].Position.X += (float)(-0.5f + random.NextDouble()) *
                        RandomFactor * CropDensity * Size.X;

                    billboards[x, y].Position.Y += (float)(-0.5f + random.NextDouble()) *
                        RandomFactor * CropDensity * Size.Y;

                    // Get height
                    billboards[x, y].Position.Z = 0;
                }
        }

        public override void Update(GameTime gameTime)
        {

        }

        /// <summary>
        /// Place the crop, crops doesn't add itself to grid data.
        /// </summary>
        /// <remarks>Crop can only be placed once</remarks>
        public override bool Place(Landscape landscape, Vector3 newPosition, float newRotation)
        {
            Position = newPosition;
            Rotation = newRotation;

            // Change the crop position
            for (int y = 0; y < billboards.GetLength(1); y++)
                for (int x = 0; x < billboards.GetLength(0); x++)
                {
                    billboards[x, y].Position = LocalToWorld(billboards[x, y].Position);

                    // Get height
                    billboards[x, y].Position.Z = world.Landscape.GetHeight(
                        billboards[x, y].Position.X, billboards[x, y].Position.Y);
                }

            // Can't be place at other place
            return false;
        }

        /// <summary>
        /// Draw the crop
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            foreach (Billboard b in billboards)
            {
                BaseGame.Singleton.Billboard.Draw(b);
            }
        }

        /// <summary>
        /// Remove the game entity from the landscape
        /// </summary>
        /// <returns>Success or not</returns>
        public override bool Pickup(Landscape landscape)
        {
            // Crops can't be picked up
            return false;
        }

        /// <summary>
        /// Ray intersection test
        /// </summary>
        /// <param name="ray">Target ray</param>
        /// <returns>
        /// Distance from the intersection point to the ray starting position,
        /// Null if there's no intersection.
        /// </returns>
        public override Nullable<float> Intersects(Ray ray)
        {
            // Not need to intersect with crops
            return null;
        }

        public override bool BeginDrag(Hand hand)
        {
            return false;
        }

        public override void EndDrag(Hand hand)
        {
            // Nothing will be dragged, since we can't change crop's position
        }

        public override bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // If dropped on a food storage, add to our total food amount,
            Building building = entity as Building;
            if (building != null && building.StoresFood)
                world.GameLogic.Food += 100; // TODO: crop settings

            // We're done with this crop
            hand.Drop();
            world.Destroy(this);
            return false;
        }

        public override void Follow(Hand hand)
        {
            // Highlight buildings that can store food
            Building building = world.Pick() as Building;
            if (building != null && building.StoresFood)
                world.Highlight(building);
            else
                world.Highlight(null);

            base.Follow(hand);
        }
        #endregion
    }

    #endregion
}
