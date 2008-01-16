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
    #region Farmland

    /// <summary>
    /// Respresents a farmland in the game
    /// </summary>
    public class Farmland : Building
    {
        /// <summary>
        /// Crop grown on this farmland
        /// </summary>
        Crop crop;

        #region Methods
        /// <summary>
        /// Create a new farmland
        /// </summary>
        public Farmland(GameScreen screen, BuildingSettings settings)
            : base(screen, settings)
        {
            crop = new Crop(screen, size);
        }

        /// <summary>
        /// Update farmland
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            // Update crops
            if (crop != null && state == BuildingState.Normal)
                crop.Update(gameTime);
            
            base.Update(gameTime);
        }

        /// <summary>
        /// Draw farmland
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            // Draw house model
            base.Draw(gameTime);

            // Draw crops
            if (crop != null && state == BuildingState.Normal)
                crop.Draw(gameTime);
        }

        public override bool Place(Landscape landscape, Vector3 newPosition, float newRotation)
        {
            // Place crop
            crop.Place(landscape, newPosition, newRotation);

            // Place this building
            return base.Place(landscape, newPosition, newRotation);
        }

        /// <summary>
        /// Delegate to crop
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public override bool BeginDrag(Hand hand)
        {
            return crop.BeginDrag(hand);
        }

        /// <summary>
        /// Drag crops from the farmland, delegate to crop
        /// </summary>
        /// <param name="hand"></param>
        public override void EndDrag(Hand hand)
        {
            /*
            if (hand.Power >= dragForce)
            {
                Crop crop = new Crop(screen, screen.CropSettings[0]);
                screen.EntityManager.Add(crop);
                hand.Drag(crop);
            }*/

            crop.EndDrag(hand);
        }
        #endregion
    }

    #endregion
}
