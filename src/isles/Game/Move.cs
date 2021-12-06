// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public sealed partial class Move : IDisposable
{
    private readonly World _world = move_world_new();

    public void Step()
    {
        move_world_step(_world);
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