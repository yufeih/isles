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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles
{
    /// <summary>
    /// Default settings for game entities and spells
    /// </summary>
    public class GameDefault
    {
        /// <summary>
        /// Gets or sets the default attributes for world objects.
        /// The key of the outer dictionary is the type name of a world object,
        /// the inner dictionary stores its default {attribute, value} pair.
        /// </summary>
        public Dictionary<string, IDictionary<string, string>>
            WorldObjectDefaults = new Dictionary<string, IDictionary<string, string>>();

        /// <summary>
        /// Gets or sets the default attributes for spells
        /// </summary>
        public Dictionary<string, IDictionary<string, string>>
            SpellDefaults = new Dictionary<string, IDictionary<string, string>>();

        /// <summary>
        /// Load the game defaults from a stream
        /// </summary>
        /// <param name="stream"></param>
        public void Load(Stream stream)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            if (doc.DocumentElement.Name != "GameDefault")
                throw new InvalidDataException();

            // Gets world object defaults
            XmlElement element =
                doc.DocumentElement.SelectSingleNode("WorldObject") as XmlElement;

            if (element != null)
            {
                foreach (XmlNode node in element.ChildNodes)
                {
                    XmlElement child = node as XmlElement;

                    if (child != null)
                    {
                        WorldObjectDefaults.Add(child.Name, new Dictionary<string,string>());
                        foreach (XmlAttribute attribute in child.Attributes)
                            WorldObjectDefaults[child.Name].Add(attribute.Name, attribute.Value);
                    }
                }
            }

            // Gets spell defaults
            element = doc.DocumentElement.SelectSingleNode("Spell") as XmlElement;

            if (element != null)
            {
                foreach (XmlNode node in element.ChildNodes)
                {
                    XmlElement child = node as XmlElement;

                    if (child != null)
                    {
                        SpellDefaults.Add(child.Name, new Dictionary<string, string>());
                        foreach (XmlAttribute attribute in child.Attributes)
                            SpellDefaults[child.Name].Add(attribute.Name, attribute.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Save the game defaults to a stream
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            throw new NotImplementedException();
        }

        private static GameDefault gameDefault;

        /// <summary>
        /// Create a game default from file
        /// </summary>
        /// <returns></returns>
        public static GameDefault Default
        {
            get
            {
                if (gameDefault != null)
                    return gameDefault;

                using (FileStream stream = new FileStream("Config/Defaults.xml", FileMode.Open))
                {
                    gameDefault = new GameDefault();
                    gameDefault.Load(stream);

                    return gameDefault;
                }
            }
        }
    }
}
