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
        /// Get level landscape
        /// </summary>
        Landscape Landscape { get; }

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
        /// Landscape for demo level
        /// </summary>
        Landscape landscape;

        /// <summary>
        /// ILevel interface
        /// </summary>
        public Landscape Landscape
        {
            get { return landscape; }
        }

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
            screen.Wood = 40000;
            screen.Gold = 40000;
            screen.Food = 50000;

            progress.Refresh(10);

            // Initialize landscape
            landscape = screen.LevelContent.Load<Landscape>("Landscapes/Landscape");

            progress.Refresh(50);

            InitializeSettings(screen, progress);

            progress.Refresh(90);

            InitializeWorldContent(screen);

            InitializeFunctions(screen);
        }

        private void InitializeFunctions(GameScreen screen)
        {
            screen.AddFunction(new FunctionPlantTree(screen));
        }

        void InitializeSettings(GameScreen screen, Loading progress)
        {
            using (FileStream file = new FileStream("Config/Buildings.xml", FileMode.Open))
            {
                screen.BuildingSettings = (BuildingSettingsCollection)
                    new XmlSerializer(typeof(BuildingSettingsCollection)).Deserialize(file);
            }

            foreach (BuildingSettings settings in screen.BuildingSettings)
                screen.LevelContent.Load<Model>(settings.Model);

            progress.Refresh(65);

            using (FileStream file = new FileStream("Config/Trees.xml", FileMode.Open))
            {
                screen.TreeSettings = (TreeSettingsCollection)
                    new XmlSerializer(typeof(TreeSettingsCollection)).Deserialize(file);
            }

            foreach (TreeSettings settings in screen.TreeSettings)
                screen.LevelContent.Load<Model>(settings.Model);

            progress.Refresh(70);

            using (FileStream file = new FileStream("Config/Stones.xml", FileMode.Open))
            {
                screen.StoneSettings = (StoneSettingsCollection)
                    new XmlSerializer(typeof(StoneSettingsCollection)).Deserialize(file);
            }

            foreach (StoneSettings settings in screen.StoneSettings)
                screen.LevelContent.Load<Model>(settings.Model);

            progress.Refresh(80);

            using (FileStream file = new FileStream("Config/Spells.xml", FileMode.Open))
            {
                screen.SpellSettings = (SpellSettingsCollection)
                    new XmlSerializer(typeof(SpellSettingsCollection)).Deserialize(file);
            }
        }

        void InitializeWorldContent(GameScreen screen)
        {
            const string WorldContentFile = "Content/Levels/World.Isles";

            WorldContent.Save(
                WorldContent.GenerateTestContent(), WorldContentFile);
            WorldContent world = WorldContent.Load(WorldContentFile);

            for (int i = 0; i < world.TreePositions.Count; i++)
            {
                Tree tree = screen.EntityManager.CreateTree(screen.TreeSettings[0]);
                if (!tree.Place(landscape, new Vector3(world.TreePositions[i], 0), 0))
                    screen.EntityManager.RemoveTree(tree);
            }

            for (int i = 0; i < world.StonePositions.Count; i++)
            {
                Stone stone = screen.EntityManager.CreateStone(screen.StoneSettings[0]);
                if (!stone.Place(landscape, new Vector3(world.StonePositions[i], 0), 0))
                    screen.EntityManager.RemoveStone(stone);
            }
        }

        /// <summary>
        /// Unload a game level
        /// </summary>
        public virtual void Unload()
        {
            if (landscape != null)
                landscape.Unload();
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
