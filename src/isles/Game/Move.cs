// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

public sealed class Move : IDisposable
{
    private const string LibName = "isles.native";

    private readonly IntPtr _world = move_world_new();
    private readonly List<IntPtr> _units = new();
    private readonly Random _random = new(0);

    public void Step(float timeStep = 1.0f / 60)
    {
        move_world_step(_world, timeStep);
    }

    public void AddUnit(float x, float y, float radius)
    {
        // Give it some random initial velocity to kick start the simulation.
        var vx = _random.NextSingle() - 0.5f;
        var vy = _random.NextSingle() - 0.5f;

        _units.Add(move_add_unit(_world, radius, damping: 100.0f, x, y, vx, vy));
    }

    public void SetUnitVelocity(int i, float vx, float vy)
    {
        move_set_unit_velocity(_units[i], vx, vy);
    }

    public (float x, float y) GetUnitPosition(int i)
    {
        move_get_unit(_units[i], out var x, out var y, out _, out _);
        return (x, y);
    }

    public bool IsRunning()
    {
        foreach (var unit in _units)
        {
            if (move_get_unit_is_awake(unit) != 0)
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

    [DllImport(LibName)] private static extern IntPtr move_add_unit(IntPtr world, float radius, float damping, float x, float y, float vx, float vy);
    [DllImport(LibName)] private static extern void move_remove_unit(IntPtr world, IntPtr unit);
    [DllImport(LibName)] private static extern void move_get_unit(IntPtr unit, out float x, out float y, out float vx, out float vy);
    [DllImport(LibName)] private static extern int move_get_unit_is_awake(IntPtr unit);
    [DllImport(LibName)] private static extern void move_set_unit_velocity(IntPtr unit, float vx, float vy);

    [DllImport(LibName)] private static extern IntPtr move_add_obstacle(IntPtr world, float x, float y, float w, float h);
    [DllImport(LibName)] private static extern void move_remove_obstacle(IntPtr world, IntPtr unit);
}
