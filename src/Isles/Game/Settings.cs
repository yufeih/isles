// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Engine
{
    public class Settings
    {
        public string ContentDirectory { get; set; } = "Content";
        public string ArchiveFile { get; set; } = "Content.ixa";

        public string DefaultFont { get; set; } = "Fonts/Default";
        public string Graphics2DEffect { get; set; } = "Effects/Graphics2D";
        public string PlayerName { get; set; } = "Unnamed";
        public bool EnableScreenshot { get; set; } = true;
        public bool EnableProfile { get; set; } = true;
        public bool IsMouseVisible { get; set; }
        public bool IsFixedTimeStep { get; set; } = true;
        public bool ClipCursor { get; set; }
        public bool EnableSound { get; set; } = true;
        public bool ShowPathGraph { get; set; }
        public bool RevealMap { get; set; }
        public int MaxPathSearchStepsPerUpdate { get; set; } = 2000;
        public bool TraceUnits { get; set; }
        public bool Cheat { get; set; }
        public double GameSpeed { get; set; } = 1;
        public GameCameraSettings CameraSettings { get; set; } = new();

        public int ScreenWidth { get; set; } = 960;
        public int ScreenHeight { get; set; } = 600;
        public bool Fullscreen { get; set; }
        public bool NormalMappedTerrain { get; set; }
        public bool RealisticWater { get; set; }
        public bool ReflectionEnabled { get; set; }
        public bool ShadowEnabled { get; set; } = true;
        public bool ShowLandscape { get; set; } = true;
        public bool ShowWater { get; set; } = true;
        public bool ShowObjectReflection { get; set; } = true;
        public BloomEffect BloomSettings { get; set; } = new();
        public float ViewDistanceSquared { get; set; } = 800;
        public string ModelEffect { get; set; } = "Effects/Model";

        public bool VSync { get; set; }

        public class BloomEffect
        {
            public bool Enabled { get; set; }
            public string Type { get; set; } = "Saturated";
            public float Threshold { get; set; } = 0.25f;
            public float Blur { get; set; } = 2;
            public float BloomIntensity { get; set; } = 1;
            public float BaseIntensity { get; set; } = 1;
            public float BloomSaturation { get; set; } = 2;
            public float BaseSaturation { get; set; }
        }
    }
}
