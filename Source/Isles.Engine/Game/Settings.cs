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

namespace Isles.Engine
{
    /// <summary>
    /// Game settings
    /// </summary>
    [Serializable()]
    public class Settings
    {
        #region General Settings
        /// <summary>
        /// Directory for game assets
        /// </summary>
        public string ContentDirectory = "Content";

        /// <summary>
        /// Default game font
        /// </summary>
        public string DefaultFont = "Fonts/Default";
        #endregion

        #region Graphics Settings

        int screenWidth = 960;
        int screenHeight = 600;
        bool fullscreen = false;
        bool bloomEffect = false;
        bool normalMappedTerrain = false;

        // We want to trace frame performance, so turn off V'Sync
#if DEBUG
        bool vsync = false;
#else
        bool vsync = false;
#endif

        /// <summary>
        /// Screen width
        /// </summary>
        public int ScreenWidth
        {
            get { return screenWidth; }
            set { screenWidth = value; }
        }

        /// <summary>
        /// Screen height
        /// </summary>
        public int ScreenHeight
        {
            get { return screenHeight; }
            set { screenHeight = value; }
        }

        /// <summary>
        /// Whether the game is running on full screen mode
        /// </summary>
        public bool Fullscreen
        {
            get { return fullscreen; }
            set { fullscreen = value; }
        }

        /// <summary>
        /// Synchronize With Vertical Retrace
        /// </summary>
        public bool VSync
        {
            get { return vsync; }
            set { vsync = value; }
        }

        /// <summary>
        /// Gets or sets whether normal mapping is used when rendering terrain
        /// </summary>
        public bool NormalMappedTerrain
        {
            get { return normalMappedTerrain; }
            set { normalMappedTerrain = value; }
        }

        /// <summary>
        /// Gets or sets whether bloom post processing is turned on
        /// </summary>
        public bool BloomEffect
        {
            get { return bloomEffect; }
            set { bloomEffect = value; }
        }

        #endregion

        #region Game play settings

        string playerName = "Unnamed";
        bool enableScreenshot = true;
        bool enablePofile = true;

        /// <summary>
        /// God name
        /// </summary>
        public string PlayerName
        {
            get { return playerName; }
            set { playerName = value; }
        }

        /// <summary>
        /// Gets or sets whether the game performance will be profiled
        /// </summary>
        public bool EnableProfile
        {
            get { return enablePofile; }
            set { enablePofile = value; }
        }

        /// <summary>
        /// Gets or sets whether it is allowed to capture screen shot
        /// </summary>
        public bool EnableScreenshot
        {
            get { return enableScreenshot; }
            set { enableScreenshot = value; }
        }

        #endregion

        #region Method
        /// <summary>
        /// Create default game settings
        /// </summary>
        /// <param name="fromFile"></param>
        /// <returns></returns>
        public static Settings CreateDefaultSettings(Stream stream)
        {
            Settings settings = new Settings();

            if (stream == null)
                return settings;

            return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
        }

        /// <summary>
        /// Save settings
        /// </summary>
        public void Save(Stream stream)
        {
            new XmlSerializer(typeof(Settings)).Serialize(stream, this);
        }
        #endregion
    }
}
