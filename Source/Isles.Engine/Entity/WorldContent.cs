using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Engine
{
    /// <summary>
    /// Game world content.
    /// Manages world objects, entities...
    /// And their serialization...
    /// </summary>
    [Serializable()]
    public class WorldContent
    {
        #region Field
        /// <summary>
        /// Positions of all trees
        /// </summary>
        public List<Vector2> TreePositions = new List<Vector2>();

        /// <summary>
        /// Types of all trees.
        /// Each value represents an index of the tree settings array.
        /// </summary>
        public List<int> TreeTypes = new List<int>();

        /// <summary>
        /// Scales of all trees
        /// </summary>
        public List<float> TreeScales = new List<float>();

        /// <summary>
        /// Positions of all stones
        /// </summary>
        public List<Vector2> StonePositions = new List<Vector2>();

        /// <summary>
        /// Types of all stones.
        /// Each value represents an index of the stone settings array.
        /// </summary>
        public List<int> StoneTypes = new List<int>();

        /// <summary>
        /// Scales of all stones
        /// </summary>
        public List<float> StoneScales = new List<float>();
        #endregion

        #region Methods
        /// <summary>
        /// Generate a few world content for test only
        /// </summary>
        public static WorldContent GenerateTestContent()
        {
            Random random = new Random();

            WorldContent content = new WorldContent();

            int nTrees = 10 + random.Next(20);

            for (int i = 0; i < nTrees; i++)
            {
                content.TreeTypes.Add(0);
                content.TreePositions.Add(new Vector2(
                    500 + random.Next(1000), 500 + random.Next(1000)));
                content.TreeScales.Add(0.5f + 0.5f * (float)random.NextDouble());
            }

            int nStones = 10 + random.Next(10);

            for (int i = 0; i < nStones; i++)
            {
                content.StoneTypes.Add(0);
                content.StonePositions.Add(new Vector2(
                    500 + random.Next(1000), 500 + random.Next(1000)));
                content.StoneScales.Add(0.5f + 0.5f * (float)random.NextDouble());
            }

            return content;
        }

        /// <summary>
        /// Loads world content from file
        /// </summary>
        public static WorldContent Load(string filename)
        {
            using (FileStream file = new FileStream(filename, FileMode.Open))
            {
                IFormatter formatter = new BinaryFormatter();
                return (WorldContent)formatter.Deserialize(file);
            }
        }

        /// <summary>
        /// Saves the world content to a file
        /// </summary>
        public static void Save(WorldContent world, string filename)
        {
            using (FileStream file = new FileStream(filename, FileMode.OpenOrCreate))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(file, world);
            }
        }
        #endregion
    }
}
