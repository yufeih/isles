//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;

namespace Isles.Engine
{
    /// <summary>
    /// Contains common game logic, e.g., wood, food...
    /// Maybe game logic should mean how to control & play the game
    /// </summary>
    public class GameLogic
    {
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
        /// Dependency list. Stores a key/bool pair. E.g., ["farmhouse", true] means that
        /// farmhouse is available.
        /// </summary>
        public Dictionary<string, bool> Dependencies = new Dictionary<string, bool>();

        /// <summary>
        /// Function list
        /// </summary>
        Dictionary<string, IFunction> functions = new Dictionary<string, IFunction>();

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

        /// <summary>
        /// Check building dependencies
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

        /// <summary>
        /// Reset game logic
        /// </summary>
        public void Reset()
        {
            Wood = 0;
            Gold = 0;
            Food = 0;

            Dependencies.Clear();
            functions.Clear();
        }
    }
}
