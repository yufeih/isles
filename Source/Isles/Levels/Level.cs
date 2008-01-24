//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
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
    #region Level
    /// <summary>
    /// Respresents a level in the game
    /// </summary>
    public class Level : ILevel
    {
        /// <summary>
        /// Create a new stone
        /// </summary>
        public Level()
        {
        }

        #region ILevel
        /// <summary>
        /// Load a game level
        /// </summary>
        public virtual void Load(GameWorld world, ILoading progress)
        {
            // Set initial money
            world.GameLogic.Wood = 40000;
            world.GameLogic.Gold = 40000;
            world.GameLogic.Food = 50000;

            // Set available buildings
            world.GameLogic.AvailableBuildings.AddRange(new string[] 
            {
                "Townhall", "Farmhouse", "Farmhouse", "Farmhouse", "Farmhouse", "Farmhouse", "Farmhouse", "Farmhouse",
                "Townhall", "Farmhouse", "Farmhouse", "Farmhouse", "Farmhouse", "Farmhouse", "Farmhouse", "Farmhouse"
            });

            world.GameLogic.CurrentSpells.AddRange(new Spell[]
            {
                Spell.Create("Fireball", world),
            });
        }

        /// <summary>
        /// Unload a game level
        /// </summary>
        public virtual void Unload()
        {

        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime)
        {

        }

        /// <summary>
        /// Draw the stone
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Draw(GameTime gameTime)
        {

        }
        #endregion
    }
    #endregion
}
