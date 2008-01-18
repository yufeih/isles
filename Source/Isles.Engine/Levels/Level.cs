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
        void Load(GameScreen screen, Loading progress);

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
        public virtual void Load(GameScreen screen, Loading progress)
        {
            // Set initial money
            screen.World.GameLogic.Wood = 40000;
            screen.World.GameLogic.Gold = 40000;
            screen.World.GameLogic.Food = 50000;

            InitializeSettings(screen, progress);

            progress.Refresh(90);

            InitializeWorldContent(screen);

            InitializeFunctions(screen);
        }

        private void InitializeFunctions(GameScreen screen)
        {
            //screen.AddFunction(new FunctionPlantTree(screen));
        }

        void InitializeSettings(GameScreen screen, Loading progress)
        {
            using (FileStream file = new FileStream("Config/Buildings.xml", FileMode.Open))
            {
                screen.World.GameLogic.BuildingSettings = (BuildingSettingsCollection)
                    new XmlSerializer(typeof(BuildingSettingsCollection)).Deserialize(file);
            }

            using (FileStream file = new FileStream("Config/Trees.xml", FileMode.Open))
            {
                screen.World.GameLogic.TreeSettings = (TreeSettingsCollection)
                    new XmlSerializer(typeof(TreeSettingsCollection)).Deserialize(file);
            }

            using (FileStream file = new FileStream("Config/Stones.xml", FileMode.Open))
            {
                screen.World.GameLogic.StoneSettings = (StoneSettingsCollection)
                    new XmlSerializer(typeof(StoneSettingsCollection)).Deserialize(file);
            }

            using (FileStream file = new FileStream("Config/Spells.xml", FileMode.Open))
            {
                screen.World.GameLogic.SpellSettings = (SpellSettingsCollection)
                    new XmlSerializer(typeof(SpellSettingsCollection)).Deserialize(file);
            }
        }

        void InitializeWorldContent(GameScreen screen)
        {
            //const string WorldContentFile = "Content/Levels/World.Isles";

            //WorldContent.Save(
            //    WorldContent.GenerateTestContent(), WorldContentFile);
            //WorldContent world = WorldContent.Load(WorldContentFile);

            //for (int i = 0; i < world.TreePositions.Count; i++)
            //{
            //    Tree tree = screen.EntityManager.CreateTree(screen.TreeSettings[0]);
            //    if (!tree.Place(landscape, new Vector3(world.TreePositions[i], 0), 0))
            //        screen.EntityManager.RemoveTree(tree);
            //}

            //for (int i = 0; i < world.StonePositions.Count; i++)
            //{
            //    Stone stone = screen.EntityManager.CreateStone(screen.StoneSettings[0]);
            //    if (!stone.Place(landscape, new Vector3(world.StonePositions[i], 0), 0))
            //        screen.EntityManager.RemoveStone(stone);
            //}
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
