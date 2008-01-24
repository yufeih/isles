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
        public Dictionary<string, XmlElement>
            WorldObjectDefaults = new Dictionary<string, XmlElement>();

        /// <summary>
        /// Gets or sets the default attributes for spells
        /// </summary>
        public Dictionary<string, XmlElement>
            SpellDefaults = new Dictionary<string, XmlElement>();

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
                        WorldObjectDefaults.Add(child.Name, child);
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
                        SpellDefaults.Add(child.Name, child);
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

        /// <summary>
        /// Assign the default attributes to the xml element of a specific type.
        /// </summary>
        /// <remarks>
        /// If there is an default XML element describing an avator like this:
        /// 
        /// <Avator Model="models/avator">
        ///     <Spells>
        ///         <Fireball Level="1" />
        ///         <Windwalk Level="1" />
        ///     </Spells>
        ///     <Items>
        ///         <HealthPotion Power="300" />
        ///         <HealthPotion Power="300" />
        ///     </Items>
        /// </Avator>
        /// 
        /// The game world file only need to store its position, all other attributes
        /// are appended using MergeAttributes automatically.
        /// 
        /// <Avator Position="1024, 512, 0" />
        /// 
        /// However for world object containing the child nodes, it's up to the client
        /// to determine whether to keep the default settings or not. So in the XML
        /// element below, the avator has only 1 item, the health potions and spells
        /// are ignored.
        /// 
        /// <Avator Position="1024, 512, 0">
        ///     <Items>
        ///         <ManaPotion Power="500" />
        ///     </Items>
        /// </Avator>
        /// 
        /// This method only append default attributes that the target xml do not have.
        /// </remarks>
        public void MergeAttributes(string type, XmlElement xml)
        {
            if (xml == null || type == null)
                return;

            XmlElement value;

            // Get default XML node describing the object type
            if (WorldObjectDefaults.TryGetValue(type, out value))
            {
                foreach (XmlAttribute attribute in value.Attributes)
                {
                    if (!xml.HasAttribute(attribute.Name))
                        xml.SetAttribute(attribute.Name, attribute.Value);
                }
            }
        }

        private static GameDefault gameDefault;

        /// <summary>
        /// Create a game default from file
        /// </summary>
        /// <returns></returns>
        public static GameDefault Singleton
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
