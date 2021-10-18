using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

// Contains managed wrappers and implementations of Win32
// structures, delegates, constants and platform invokes
// used by the GradientFill and Subclassing samples.

namespace Isles.Engine
{

    public sealed class Win32
    {
        // WM_NOTIFY notificaiton message header.
        [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
        public class NMHDR
        {
            private IntPtr hwndFrom;
            public uint idFrom;
            public uint code;
        }

        // Native representation of a point.
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public struct TVHITTESTINFO
        {
            public POINT pt;
            public uint flags;
            public IntPtr hItem;
        }

        // A callback to a Win32 window procedure (wndproc):
        // Parameters:
        //   hwnd - The handle of the window receiving a message.
        //   msg - The message</param>
        //   wParam - The message's parameters (part 1).
        //   lParam - The message's parameters (part 2).
        //  Returns an integer as described for the given message in MSDN.
        public delegate int WndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);

        [DllImport("user32.dll")]
        public extern static IntPtr SetWindowLong(
            IntPtr hwnd, int nIndex, IntPtr dwNewLong);
        public const int GWL_WNDPROC = -4;

        [DllImport("user32.dll")]
        public extern static int CallWindowProc(
            IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, uint wParam, int lParam);

        [DllImport("user32.dll")]
        public extern static int DefWindowProc(
            IntPtr hwnd, uint msg, uint wParam, int lParam);

        [DllImport("coredll.dll")]
        public extern static int SendMessage(
            IntPtr hwnd, uint msg, uint wParam, ref TVHITTESTINFO lParam);

        [DllImport("coredll.dll")]
        public extern static uint GetMessagePos();

        [DllImport("user32.dll")]
        public extern static bool ClipCursor(ref RECT rect);

        [DllImport("user32.dll", EntryPoint = "LoadCursorFromFileW",
        CharSet = CharSet.Unicode)]
        public extern static IntPtr LoadCursorFromFile(string filename);

        // Helper function to convert a Windows lParam into a Point.
        //   lParam - The parameter to convert.
        // Rreturns a Point where X is the low 16 bits and Y is the
        // high 16 bits of the value passed in.
        public static Point LParamToPoint(int lParam)
        {
            uint ulParam = (uint)lParam;
            return new Point(
                (int)(ulParam & 0x0000ffff),
                (int)((ulParam & 0xffff0000) >> 16));
        }

        // Windows messages
        public const uint WM_PAINT = 0x000F;
        public const uint WM_ERASEBKGND = 0x0014;
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        public const uint WM_MOUSEMOVE = 0x0200;
        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_LBUTTONUP = 0x0202;
        public const uint WM_NOTIFY = 0x4E;

        // Notifications
        public const uint NM_CLICK = 0xFFFFFFFE;
        public const uint NM_DBLCLK = 0xFFFFFFFD;
        public const uint NM_RCLICK = 0xFFFFFFFB;
        public const uint NM_RDBLCLK = 0xFFFFFFFA;

        // Key
        public const uint VK_SPACE = 0x20;
        public const uint VK_RETURN = 0x0D;

        // Treeview
        public const uint TV_FIRST = 0x1100;
        public const uint TVM_HITTEST = TV_FIRST + 17;

        public const uint TVHT_NOWHERE = 0x0001;
        public const uint TVHT_ONITEMICON = 0x0002;
        public const uint TVHT_ONITEMLABEL = 0x0004;
        public const uint TVHT_ONITEM = (TVHT_ONITEMICON | TVHT_ONITEMLABEL | TVHT_ONITEMSTATEICON);
        public const uint TVHT_ONITEMINDENT = 0x0008;
        public const uint TVHT_ONITEMBUTTON = 0x0010;
        public const uint TVHT_ONITEMRIGHT = 0x0020;
        public const uint TVHT_ONITEMSTATEICON = 0x0040;
        public const uint TVHT_ABOVE = 0x0100;
        public const uint TVHT_BELOW = 0x0200;
        public const uint TVHT_TORIGHT = 0x0400;
        public const uint TVHT_TOLEFT = 0x0800;

        public const uint TVM_GETITEM = TV_FIRST + 62;  //TVM_GETITEMW

        public const uint TVIF_TEXT = 0x0001;
        public const uint TVIF_IMAGE = 0x0002;
        public const uint TVIF_PARAM = 0x0004;
        public const uint TVIF_STATE = 0x0008;
        public const uint TVIF_HANDLE = 0x0010;
        public const uint TVIF_SELECTEDIMAGE = 0x0020;
        public const uint TVIF_CHILDREN = 0x0040;
        public const uint TVIF_DI_SETITEM = 0x1000;
    }
}