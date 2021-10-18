//-----------------------------------------------------------------------------
//  Isles v1.0
//
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
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
        /// Directory for game assets
        /// </summary>
        public string ContentDirectory = "Content";
        public string ArchiveFile = "Content.ixa";

        /// <summary>
        /// Default game font
        /// </summary>
        public string DefaultFont = "Fonts/Default";
        public string Graphics2DEffect = "Effects/Graphics2D";
        public string PlayerName = "Unnamed";
        public bool EnableScreenshot = true;
        public bool EnableProfile = true;
        public bool IsMouseVisible;
        public bool IsFixedTimeStep = true;
        public bool ClipCursor;
        public bool EnableSound = true;
        public bool ShowPathGraph;
        public bool RevealMap;
        public int MaxPathSearchStepsPerUpdate = 2000;
        public bool DirectEnter;
        public bool TraceUnits;
        public bool Cheat;
        public double GameSpeed = 1;
        public GameCameraSettings CameraSettings;

        public int ScreenWidth = 960;
        public int ScreenHeight = 600;
        public bool Fullscreen;
        public bool NormalMappedTerrain;
        public bool RealisticWater;
        public bool ReflectionEnabled;
        public bool ShadowEnabled = true;
        public bool ShowLandscape = true;
        public bool ShowWater = true;
        public bool ShowObjectReflection = true;
        public BloomEffect BloomSettings;
        public float ViewDistanceSquared = 800;
        public string ModelEffect = "Effects/Model";

        // We want to trace frame performance, so turn off V'Sync
#if DEBUG
        public bool VSync;
#else
        public bool VSync = false;
#endif

        [Serializable()]
        public class BloomEffect
        {
            public bool Enabled;
            public string Type = "Saturated";
            public float Threshold = 0.25f;
            public float Blur = 2;
            public float BloomIntensity = 1;
            public float BaseIntensity = 1;
            public float BloomSaturation = 2;
            public float BaseSaturation;

        }

        /// <summary>
        /// Create default game settings
        /// </summary>
        /// <param name="fromFile"></param>
        /// <returns></returns>
        public static Settings CreateDefaultSettings(Stream stream)
        {
            var settings = new Settings();

            return stream == null ? settings : (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
        }

        /// <summary>
        /// Save settings
        /// </summary>
        public void Save(Stream stream)
        {
            new XmlSerializer(typeof(Settings)).Serialize(stream, this);
        }

    }
}
