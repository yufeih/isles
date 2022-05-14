// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    private static IntPtr s_cursor;

    public static void SetCursor(IntPtr cursor)
    {
        if (s_cursor != cursor)
        {
            s_cursor = cursor;
            SDL_SetCursor(cursor);
        }
    }

    private static unsafe IntPtr LoadCursor(string name)
    {
        var pixels = TextureLoader.ReadAllPixels($"data/cursors/{name}.png", out var w, out var h);
        var info = JsonHelper.DeserializeAnonymousType(
            File.ReadAllBytes($"data/cursors/{name}.json"), new { hotspot = Array.Empty<int>() });

        fixed (void* ptr = pixels)
        {
            var surface = SDL_CreateRGBSurfaceFrom((IntPtr)ptr, w, h, 32, w * 4, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
            var cursor = SDL_CreateColorCursor(surface, info.hotspot[0], info.hotspot[1]);
            SDL_FreeSurface(surface);
            return cursor;
        }
    }
}
