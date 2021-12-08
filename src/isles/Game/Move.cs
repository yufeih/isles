// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public sealed partial class Move : IDisposable
{
    private readonly World _world = move_world_new();
    private readonly List<Unit> _units = new();

    public void Step(float timeStep = 1.0f / 60)
    {
        move_world_step(_world, timeStep);
    }

    public void AddUnit(float x, float y, float radius)
    {
        _units.Add(move_add_unit(_world, x, y, radius));
    }

    public void SetUnitVelocity(int i, float vx, float vy)
    {
        move_set_unit_velocity(_units[i], vx, vy);
    }

    public (float x, float y) GetUnitPosition(int i)
    {
        move_get_unit(_units[i], out var x, out var y, out var vx, out var vy);
        return (x, y);
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
}