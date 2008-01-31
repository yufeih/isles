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

        /// <summary>
        /// Gets settins stream
        /// </summary>
        static Stream SettingsStream
        {
            get { return new FileStream("Config/Settings.xml", FileMode.Open); }
        }

        public GameIsles() : base(Settings.CreateDefaultSettings(SettingsStream))
        {

        }

        /// <summary>
        /// Initialize everything here
        /// </summary>
        protected override void FirstTimeInitialize()
        {
            // Register everything
            Register();

            // Start new level
            using (Stream stream = new FileStream("Content/Levels/World.xml", FileMode.Open))
            {
                gameScreen.StartLevel(stream);
            }

            // Start game screen
            StartScreen(gameScreen);
        }


        private void Register()
        {
            // Register world objects
            GameWorld.RegisterCreator("Tree", delegate(GameWorld world)
            {
                return new Tree(world);
            });

            GameWorld.RegisterCreator("Stone", delegate(GameWorld world)
            {
                return new Stone(world);
            });

            GameWorld.RegisterCreator("Townhall", delegate(GameWorld world)
            {
                return new Building(world, "Townhall");
            });

            GameWorld.RegisterCreator("Farmhouse", delegate(GameWorld world)
            {
                return new Building(world, "Farmhouse");
            });

            GameWorld.RegisterCreator("Windmill", delegate(GameWorld world)
            {
                return new Building(world, "Windmill");
            });

            GameWorld.RegisterCreator("Storage", delegate(GameWorld world)
            {
                return new Building(world, "Storage");
            });
            
            GameWorld.RegisterCreator("AltarOfPeace", delegate(GameWorld world)
            {
                return new Building(world, "AltarOfPeace");
            });

            GameWorld.RegisterCreator("AltarOfDestruction", delegate(GameWorld world)
            {
                return new Building(world, "AltarOfDestruction");
            });


            // Register levels
            GameWorld.RegisterLevel("Demo", new Level());


            // Register spells
            Spell.RegisterCreator("Fireball", delegate(GameWorld world)
            {
                return new SpellFireball(world);
            });

            Spell.RegisterCreator("PlantTree", delegate(GameWorld world)
            {
                return new SpellPlantTree(world);
            });


            // Register screens
            AddScreen("GameScreen", gameScreen = new GameScreen());
        }
    }
}
