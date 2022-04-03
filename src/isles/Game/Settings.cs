// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class Settings
{
    public string ContentDirectory { get; set; } = "Content";

    public string PlayerName { get; set; } = "Unnamed";
    public bool IsMouseVisible { get; set; }
    public bool IsFixedTimeStep { get; set; } = true;
    public bool RevealMap { get; set; }
    public bool TraceUnits { get; set; }
    public bool Cheat { get; set; }
    public GameCameraSettings CameraSettings { get; set; } = new();

    public int ScreenWidth { get; set; } = 960;
    public int ScreenHeight { get; set; } = 600;
    public bool Fullscreen { get; set; }
    public bool NormalMappedTerrain { get; set; }
    public bool ReflectionEnabled { get; set; }
    public bool ShadowEnabled { get; set; } = true;
    public bool ShowObjectReflection { get; set; } = true;
    public BloomSettings BloomSettings { get; set; } = new();
    public float ViewDistanceSquared { get; set; } = 800;
    public string ModelEffect { get; set; } = "Effects/Model";

    public bool VSync { get; set; }
}
