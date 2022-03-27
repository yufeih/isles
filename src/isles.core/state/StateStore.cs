// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class StateStore
{
    private readonly Dictionary<int, State> _states = new();

    public State? Get(int id)
    {
        return _states.TryGetValue(id, out var value) ? value : null;
    }

    public void Post(Command command)
    {
        
    }

    public void Update(GameTime dt)
    {

    }
}
