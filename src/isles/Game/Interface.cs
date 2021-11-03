// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml;
using Isles.Graphics;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    /// <summary>
    /// Represents a game screen.
    /// </summary>
    public interface IScreen : IEventListener
    {
        /// <summary>
        /// Handle game updates.
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Handle game draw event.
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime);
    }

    /// <summary>
    /// Interface for a landscape.
    /// The landscape lays on the XY plane, Z value is used to represent the height.
    /// The position of the landscape is fixed at (0, 0).
    /// </summary>
    public interface ILandscape
    {
        /// <summary>
        /// Gets the size of the landscape.
        /// </summary>
        Vector3 Size { get; }

        /// <summary>
        /// Gets the height (Z value) of a point (x, y) on the landscape.
        /// </summary>
        float GetHeight(float x, float y);

        /// <summary>
        /// Gets the number of grids.
        /// </summary>
        Point GridCount { get; }

        /// <summary>
        /// Gets whether the point is walkable (E.g., above water).
        /// </summary>
        bool IsPointOccluded(float x, float y);
    }
}
