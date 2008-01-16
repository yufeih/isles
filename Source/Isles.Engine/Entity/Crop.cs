using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
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
        public Crop(GameScreen screen, Vector3 size) : base(screen)
        {
            this.size = size;

            texture = screen.LevelContent.Load<Texture2D>("Textures/Vegetation/Crop2");

            Random random = new Random();

            billboards = new Billboard[
                (int)(size.X * CropDensity), (int)(size.Y * CropDensity)];

            int xCount = billboards.GetLength(0);
            int yCount = billboards.GetLength(1);

            Vector3 fieldSize = CropDensity * size;

            fieldSize.X *= (xCount - 1);
            fieldSize.Y *= (yCount - 1);

            Vector3 basePosition = -size/2 + (size - fieldSize) / 2;

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

                    billboards[x, y].Position.X = size.X * x * CropDensity + basePosition.X;
                    billboards[x, y].Position.Y = size.Y * y * CropDensity + basePosition.Y;

                    billboards[x, y].Position.X += (float)(-0.5f + random.NextDouble()) *
                        RandomFactor * CropDensity * size.X;

                    billboards[x, y].Position.Y += (float)(-0.5f + random.NextDouble()) *
                        RandomFactor * CropDensity * size.Y;

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
            position = newPosition;
            rotation = newRotation;

            // Change the crop position
            for (int y = 0; y < billboards.GetLength(1); y++)
                for (int x = 0; x < billboards.GetLength(0); x++)
                {
                    billboards[x, y].Position = LocalToWorld(billboards[x, y].Position);

                    // Get height
                    billboards[x, y].Position.Z = screen.Landscape.GetHeight(
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
                screen.Game.Billboard.Draw(b);
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
            if (building != null && building.Settings.StoreFood)
                screen.Food += 100; // TODO: crop settings

            // We're done with this crop
            hand.Drop();
            screen.EntityManager.Remove(this);
            return false;
        }

        public override void Follow(Hand hand)
        {
            // Highlight buildings that can store food
            Building building = screen.Pick() as Building;
            if (building != null && building.Settings.StoreFood)
                screen.EntityManager.Highlighted = building;
            else
                screen.EntityManager.Highlighted = null;

            base.Follow(hand);
        }
        #endregion
    }

    #endregion
}
