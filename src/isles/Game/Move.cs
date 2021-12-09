// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

public sealed class Move : IDisposable
{
    private const string LibName = "isles.native";

    private readonly IntPtr _world = move_world_new();
    private readonly List<(IntPtr ptr, float radius, float speed)> _units = new();
    private readonly List<IntPtr> _obstacles = new();

    public int UnitCount => _units.Count;

    public void Step(float timeStep = 1.0f / 60)
    {
        move_world_step(_world, timeStep);
    }

    public void AddUnit(float x, float y, float radius, float speed)
    {
        var ptr = move_unit_add(_world, radius, x, y);
        _units.Add((ptr, radius, speed));
    }

    public void SetUnitTarget(int i, float x, float y)
    {
        var (ptr, radius, speed) = _units[i];
        move_unit_get(ptr, out var px, out var py, out var _, out var _);

        var gap = new Vector2(x - px, y - py);
        var distanceSq = gap.LengthSquared();
        if (distanceSq < radius * radius)
        {
            move_unit_set_velocity(ptr, 0, 0);
            return;
        }

        gap.Normalize();
        gap *= speed;

        move_unit_set_velocity(ptr, gap.X, gap.Y);
    }

    public (float x, float y, float vx, float vy) GetUnit(int i)
    {
        move_unit_get(_units[i].ptr, out var x, out var y, out var vx, out var vy);
        return (x, y, vx, vy);
    }

    public void AddObstacle(float x, float y, float w, float h)
    {
        _obstacles.Add(move_obstacle_add(_world, x, y, w, h));
    }

    public bool IsRunning()
    {
        foreach (var (ptr, _, _) in _units)
        {
            if (move_unit_is_awake(ptr) != 0)
            {
                return true;
            }
        }
        return false;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        move_world_delete(_world);
    }

    ~Move()
    {
        move_world_delete(_world);
    }

    [DllImport(LibName)] private static extern IntPtr move_world_new();
    [DllImport(LibName)] private static extern void move_world_delete(IntPtr world);
    [DllImport(LibName)] private static extern void move_world_step(IntPtr world, float timeStep);

    [DllImport(LibName)] private static extern IntPtr move_unit_add(IntPtr world, float radius, float x, float y);
    [DllImport(LibName)] private static extern void move_unit_remove(IntPtr world, IntPtr unit);
    [DllImport(LibName)] private static extern int move_unit_is_awake(IntPtr unit);
    [DllImport(LibName)] private static extern void move_unit_get(IntPtr unit, out float x, out float y, out float vx, out float vy);
    [DllImport(LibName)] private static extern void move_unit_set_velocity(IntPtr unit, float x, float y);
    [DllImport(LibName)] private static extern void move_unit_apply_force(IntPtr unit, float x, float y);
    [DllImport(LibName)] private static extern void move_unit_apply_impulse(IntPtr unit, float x, float y);

    [DllImport(LibName)] private static extern IntPtr move_obstacle_add(IntPtr world, float x, float y, float w, float h);
    [DllImport(LibName)] private static extern void move_obstacle_remove(IntPtr world, IntPtr unit);
}
