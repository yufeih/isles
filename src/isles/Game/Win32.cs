using System;
using System.Runtime.InteropServices;

namespace Isles.Engine
{
    public sealed class Win32
    {
        [DllImport("user32.dll", EntryPoint = "LoadCursorFromFileW", CharSet = CharSet.Unicode)]
        public extern static IntPtr LoadCursorFromFile(string filename);
    }
}