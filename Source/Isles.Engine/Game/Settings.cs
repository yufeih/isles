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
        /// <summary>
        /// Filename of the default settings
        /// </summary>
        public const string Filename = "Config/Settings.xml";

        /// <summary>
        /// Asset name of the default sprite font
        /// </summary>
        public const string DefaultFontFile = "Fonts/Default";

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

        /// <summary>
        /// Create default game settings
        /// </summary>
        /// <param name="fromFile"></param>
        /// <returns></returns>
        public static Settings CreateDefaultSettings(bool fromFile)
        {
            Settings settings = new Settings();

            if (!fromFile)
            {
                settings.Save();
                return settings;
            }

            if (File.Exists(Filename))
            {
                // Initialize game settings from file
                using (FileStream file = new FileStream(Filename, FileMode.Open))
                {
                    settings = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(file);
                }
            }

            return settings;
        }

        /// <summary>
        /// Save settings
        /// </summary>
        public void Save()
        {
            using (FileStream file = new FileStream(Filename, FileMode.Create))
            {
                new XmlSerializer(typeof(Settings)).Serialize(file, this);
            }
        }
    }
}
