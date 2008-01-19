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
using Isles.Graphics;

namespace Isles.Engine
{
    #region ILevel
    /// <summary>
    /// Interface for a game level
    /// </summary>
    public interface ILevel
    {
        /// <summary>
        /// Load a game level
        /// </summary>
        void Load(GameWorld world, Loading progress);

        /// <summary>
        /// Unload a game level
        /// </summary>
        void Unload();

        /// <summary>
        /// Update level stuff
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Draw level stuff
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime);
    }
    #endregion

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
        public virtual void Load(GameWorld world, Loading progress)
        {
            // Set initial money
            world.GameLogic.Wood = 40000;
            world.GameLogic.Gold = 40000;
            world.GameLogic.Food = 50000;

            progress.Refresh(90);
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
