// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

public partial class Move
{
    private const string LibName = "isles.native";

    [DllImport(LibName)] private static extern World move_world_new();
    [DllImport(LibName)] private static extern void move_world_delete(World world);
    [DllImport(LibName)] private static extern void move_world_step(World world, float timeStep);

    [DllImport(LibName)] private static extern Unit move_add_unit(World world, float x, float y, float radius);
    [DllImport(LibName)] private static extern void move_remove_unit(World world, Unit unit);
    [DllImport(LibName)] private static extern void move_get_unit(Unit unit, out float x, out float y, out float vx, out float vy);
    [DllImport(LibName)] private static extern void move_set_unit_velocity(Unit unit, float vx, float vy);

    [DllImport(LibName)] private static extern Obstacle move_add_obstacle(World world, float x, float y, float w, float h);
    [DllImport(LibName)] private static extern void move_remove_obstacle(World world, Obstacle unit);

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0169  // The field '_' is never used

    struct World { private IntPtr _; }
    struct Unit { private IntPtr _; }
    struct Obstacle { private IntPtr _; }
}