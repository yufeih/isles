//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;


namespace Isles
{
    /// <summary>
    /// Game Isles
    /// </summary>
    public class GameIsles : BaseGame
    {
        /// <summary>
        /// Game screen
        /// </summary>
        GameScreen gameScreen;

        /// <summary>
        /// Gets game screen
        /// </summary>
        public GameScreen GameScreen
        {
            get { return gameScreen; }
        }

        public GameIsles()
        {
            // Initialize screens
            AddScreen("GameScreen", gameScreen = new GameScreen());

            // Register world objects
            GameWorld.RegisterCreator(typeof(Tree), delegate(GameWorld world)
            {
                return new Tree(world);
            });

            GameWorld.RegisterCreator(typeof(Building), delegate(GameWorld world)
            {
                return new Building(world);
            });

            GameWorld.RegisterCreator(typeof(Stone), delegate(GameWorld world)
            {
                return new Stone(world);
            });
        }

        protected override void OnInitialized()
        {
            // Start new level
            using (Stream stream = new FileStream("Content/Levels/World.xml", FileMode.Open))
            {
                gameScreen.StartLevel(stream);
            }

            // Start game screen
            StartScreen(gameScreen);

            base.OnInitialized();
        }
    }
}
