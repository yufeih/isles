// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public interface IScreen : IEventListener
{
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}
