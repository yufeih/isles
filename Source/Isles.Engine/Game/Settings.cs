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
        public string PlayerName = "Unnamed";
        public bool EnableScreenshot = true;
        public bool EnableProfile = true;
        public bool IsMouseVisible = false;
        public bool IsFixedTimeStep = true;
        public Camera CameraSettings;

        [Serializable()]
        public class Camera
        {
            public float FieldOfView = MathHelper.PiOver4;
            public float FarPlane = 10000;
            public float NearPlane = 1;

            public float MinHeightAboveGround = 10.0f;
            public float NavigationAreaSize = 10;
            public float WheelFactor = 0.1f;
            public float ScrollHeightFactor = 1.25f;
            public float InitialHeight = 100.0f;
            public float RotationFactorX = MathHelper.PiOver2 / 300;
            public float RotationFactorY = MathHelper.PiOver2 / 150;

            public float BaseSpeed = 0.15f;
            public float Radius = 200.0f;
            public float Roll = MathHelper.PiOver4;
            public float Pitch = -MathHelper.PiOver4;

            public float MinRoll = MathHelper.ToRadians(10);
            public float MaxRoll = MathHelper.ToRadians(80);

            public float MinRadius = 10.0F;
            public float MaxRadius = 1500.0f;
        }
        #endregion

        #region Graphics Settings

        public int ScreenWidth = 960;
        public int ScreenHeight = 600;
        public bool Fullscreen = false;
        public bool NormalMappedTerrain = false;
        public BloomEffect BloomSettings;
        public float ViewDistanceSquared = 800;

        // We want to trace frame performance, so turn off V'Sync
#if DEBUG
        public bool VSync = false;
#else
        public bool VSync = false;
#endif

        [Serializable()]
        public class BloomEffect
        {
            public bool Enabled = false;
            public string Type = "Saturated";
            public float Threshold = 0.25f;
            public float Blur = 2;
            public float BloomIntensity = 1;
            public float BaseIntensity = 1;
            public float BloomSaturation = 2;
            public float BaseSaturation = 0;

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
