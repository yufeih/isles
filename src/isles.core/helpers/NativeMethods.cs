// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

static class NativeMethods
{
    private const string LibName = "isles.native";

    [StructLayout(LayoutKind.Sequential)]
    public struct MoveContact
    {
        public int A;
        public int B;
    }

    [DllImport(LibName)] public static extern IntPtr move_new();
    [DllImport(LibName)] public static extern void move_delete(IntPtr world);
    [DllImport(LibName)] public static unsafe extern void move_step(IntPtr world, float dt, void* units, int length, int sizeInBytes);
    [DllImport(LibName)] public static unsafe extern void move_add_obstacle(IntPtr world, Vector2* vertices, int length);
    [DllImport(LibName)] public static unsafe extern int move_get_contacts(IntPtr world, MoveContact* contacts, int length);

    [DllImport(LibName)] public static extern IntPtr navmesh_new_polygon();
    [DllImport(LibName)] public static extern void navmesh_delete_polygon(IntPtr polygon);
    [DllImport(LibName)] public static unsafe extern void navmesh_polygon_add_polylines(IntPtr polygon, Vector2* vertices, int length);
    [DllImport(LibName)] public static unsafe extern int navmesh_polygon_triangulate(IntPtr polygon, out ushort* indices);
}
