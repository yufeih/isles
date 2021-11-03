// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using static SDL2.SDL;

namespace Isles
{
    public static unsafe class Cursors
    {
        public static IntPtr StoredCursor;

        private static readonly SDL_Cursor* s_cursor = (SDL_Cursor*)SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);

        public static IntPtr MenuDefault { get; } = LoadCursor("NormalCursor.cur");
        public static IntPtr Default { get; } = LoadCursor("default.ani");
        public static IntPtr TargetRed { get; } = LoadCursor("target_red.ani");
        public static IntPtr TargetGreen { get; } = LoadCursor("target_green.ani");
        public static IntPtr Top { get; } = LoadCursor("screen_top.cur");
        public static IntPtr Bottom { get; } = LoadCursor("screen_bottom.cur");
        public static IntPtr Left { get; } = LoadCursor("screen_left.cur");
        public static IntPtr Right { get; } = LoadCursor("screen_right.cur");
        public static IntPtr Move { get; } = LoadCursor("screen_move.cur");
        public static IntPtr Rotate { get; } = LoadCursor("screen_rotate.cur");

        private static IntPtr LoadCursor(string name)
        {
#if WINDOWS
            return LoadCursorFromFile(System.IO.Path.Combine(AppContext.BaseDirectory, "data/cursors", name));
#else
            return default;
#endif
        }

        public static unsafe void SetCursor(IntPtr cursor)
        {
            // SDL calls SetCursor in the WM_SETCURSOR message handler:
            // https://github.com/libsdl-org/SDL/blob/19dee1cd16e38451f1d7beae67dd74b471b403a8/src/video/windows/SDL_windowsevents.c#L1168
            //
            // This hack tricks SDL to use our own cursor returned by LoadCursorFromFile.
            // Cursors created from SDL_CreateColorCursor doesn't support animation and
            // doesn't respect Windows pointer size settings.
            s_cursor->driverdata = cursor;
            SDL_SetCursor((IntPtr)s_cursor);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SDL_Cursor
        {
            public IntPtr next;
            public IntPtr driverdata;
        };

#if WINDOWS
        [DllImport("user32.dll", EntryPoint = "LoadCursorFromFileW", CharSet = CharSet.Unicode)]
        private extern static IntPtr LoadCursorFromFile(string filename);
#endif
    }
}
