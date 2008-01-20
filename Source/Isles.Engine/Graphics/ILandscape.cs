//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    /// <summary>
    /// Interface for a landscape.
    /// The landscape lays on the XY plane, Z value is used to represent the height.
    /// The position of the landscape is fixed at (0, 0).
    /// </summary>
    public interface ILandscape
    {
        /// <summary>
        /// Gets the size of the landscape
        /// </summary>
        Vector3 Size { get; }

        /// <summary>
        /// Gets the height (Z value) of a point (x, y) on the landscape
        /// </summary>
        float GetHeight(float x, float y);

        /// <summary>
        /// Gets the height of a grid. 
        /// </summary>
        float GetGridHeight(int x, int y);

        /// <summary>
        /// Gets the number of grids
        /// </summary>
        Point GridCount { get; }

        /// <summary>
        /// Gets the size of single grid
        /// </summary>
        Vector2 GridSize { get; }

        /// <summary>
        /// Gets the position of a grid
        /// </summary>
        Vector2 GridToPosition(int x, int y);

        /// <summary>
        /// Gets the grid occupying the point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        Point PositionToGrid(float x, float y);
        
        /// <summary>
        /// Performs a ray landscape intersection test, the intersection point
        /// is returned.
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        Vector3? Intersects(Ray ray);

        /// <summary>
        /// Update the landscape
        /// </summary>
        void Update(GameTime gameTime);

        /// <summary>
        /// Draw the landscape
        /// </summary
        void Draw(GameTime gameTime);
        
        /// <summary>
        /// Draw the given texture onto the landscape
        /// </summary>
        void DrawSurface(Texture2D texture, Vector2 position, Vector2 size);
    }
}
