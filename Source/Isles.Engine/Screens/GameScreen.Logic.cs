//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;
using Isles.AI;

namespace Isles.Engine
{
    /// <summary>
    /// Game logic
    /// </summary>
    public partial class GameScreen
    {
        #region Fields
        /// <summary>
        /// Game gravity
        /// </summary>
        public Vector3 Gravity = new Vector3(0, 0, -9.8f);

        /// <summary>
        /// How much wood we have now
        /// </summary>
        public int Wood;

        /// <summary>
        /// How much gold we have now
        /// </summary>
        public int Gold;

        /// <summary>
        /// How much food we have now
        /// </summary>
        public int Food;

        /// <summary>
        /// All building settings
        /// </summary>
        public BuildingSettingsCollection BuildingSettings = new BuildingSettingsCollection();

        /// <summary>
        /// All tree settings
        /// </summary>
        public TreeSettingsCollection TreeSettings = new TreeSettingsCollection();

        /// <summary>
        /// All stone settings
        /// </summary>
        public StoneSettingsCollection StoneSettings = new StoneSettingsCollection();

        /// <summary>
        /// All spell settings
        /// </summary>
        public SpellSettingsCollection SpellSettings = new SpellSettingsCollection();

        /// <summary>
        /// Dependency list
        /// </summary>
        public Dictionary<string, bool> Dependencies = new Dictionary<string, bool>();

        /// <summary>
        /// Function list
        /// </summary>
        Dictionary<string, IFunction> functions = new Dictionary<string, IFunction>();
        #endregion

        #region Methods

        public void AddFunction(IFunction function)
        {
            functions.Add(function.Name, function);
        }

        public IFunction GetFunction(string key)
        {
            return functions[key];
        }

        public void RemoveFunction(IFunction function)
        {
            functions.Remove(function.Name);
        }

        PathManager pathManager;

        void InitializeGameLogic()
        {
            pathManager = new PathManager(landscape);
        }
         
        void DrawGameStates()
        {
            Text.DrawString(
                "Wood: " + Wood +
                " Gold: " + Gold +
                " Food: " + Food +
                "\nHand Power: " + hand.Power +
                "\nHand State: " + hand.State.ToString(),
                15, new Vector2(600, 0), Color.Yellow);

            if (entityManager.Selected != null)
            {
                Text.DrawString("Name: " + entityManager.Selected.Name, 14, new Vector2(0, 100), Color.Pink);
            }            

            //pathManager.QueryImmediate(2000, 2020);

            //if (path.Count > 1)
            //    Text.DrawLineStrip(path.ToArray(), Vector3.Zero);
        }

        /// <summary>
        /// Check dependencies
        /// </summary>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public bool CheckDependency(List<string> keys)
        {
            foreach (string key in keys)
                if (!Dependencies.ContainsKey(key) || !Dependencies[key])
                    return false;

            return true;
        }
        #endregion
    }
}
