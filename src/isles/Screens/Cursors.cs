// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

using static SDL2.SDL;

namespace Isles;

public static class Cursors
{
    public static IntPtr MenuDefault { get; } = LoadCursor("menu");
    public static IntPtr Default { get; } = LoadCursor("default");
    public static IntPtr TargetRed { get; } = LoadCursor("target_red");
    public static IntPtr TargetGreen { get; } = LoadCursor("target_green");
    public static IntPtr Top { get; } = LoadCursor("screen_top");
    public static IntPtr Bottom { get; } = LoadCursor("screen_bottom");
    public static IntPtr Left { get; } = LoadCursor("screen_left");
    public static IntPtr Right { get; } = LoadCursor("screen_right");
    public static IntPtr Move { get; } = LoadCursor("screen_move");
    public static IntPtr Rotate { get; } = LoadCursor("screen_rotate");

    public static void SetCursor(IntPtr cursor)
    {
        SDL_SetCursor(cursor);
    }

    private static unsafe IntPtr LoadCursor(string name)
    {
        var (pixels, w, h) = TextureLoader.ReadAllPixels($"data/cursors/{name}.png");
        var info = JsonHelper.DeserializeAnonymousType(
            File.ReadAllBytes($"data/cursors/{name}.json"), new { hotspot = Array.Empty<int>() });

        fixed (Color* ptr = pixels)
        {
            var surface = SDL_CreateRGBSurfaceFrom((IntPtr)ptr, w, h, 32, w * 4, 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);
            var cursor = SDL_CreateColorCursor(surface, info.hotspot[0], info.hotspot[1]);
            SDL_FreeSurface(surface);
            return cursor;
        }
    }
}
