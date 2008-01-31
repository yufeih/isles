//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    /// <summary>
    /// Type of landscape
    /// </summary>
    public enum LandscapeType
    {
        Unknown, Ground, Water, Obstacle
    }

    public partial class Landscape
    {
        #region Grid
        
        /// <summary>
        /// The data to hold on each grid
        /// </summary>
        public struct Grid
        {
            /// <summary>
            /// Type of landscape
            /// </summary>
            public LandscapeType Type;

            /// <summary>
            /// Owners of this grid, allow overlapping
            /// </summary>
            public List<Entity> Owners;
        }

        /// <summary>
        /// Get grid data
        /// </summary>
        public Grid[,] Data;

        /// <summary>
        /// Describes the size of heightfield
        /// </summary>
        int gridColumnCount, gridRowCount;

        /// <summary>
        /// Describes the size of heightfield
        /// </summary>
        float gridSizeOnXAxis, gridSizeOnYAxis;

        /// <summary>
        /// Gets the size.X of grid
        /// </summary>
        public int GridColumnCount
        {
            get { return gridColumnCount; }
        }

        /// <summary>
        /// Gets the height of grid
        /// </summary>
        public int GridRowCount
        {
            get { return gridRowCount; }
        }

        /// <summary>
        /// Gets the size.X of grid
        /// </summary>
        public float GridSizeOnXAxis
        {
            get { return gridSizeOnXAxis; }
        }

        /// <summary>
        /// Gets the height of grid
        /// </summary>
        public float GridSizeOnYAxis
        {
            get { return gridSizeOnYAxis; }
        }

        public Vector2 GridSize
        {
            get { return new Vector2(gridSizeOnXAxis, gridSizeOnYAxis); }
        }

        public Point GridCount
        {
            get { return new Point(gridColumnCount, gridRowCount); }
        }

        /// <summary>
        /// Checks if a grid is within the boundery of the terrain
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public bool IsValidGrid(Point grid)
        {
            return grid.X >= 0 && grid.X < gridColumnCount &&
                   grid.Y >= 0 && grid.Y < gridRowCount;
        }

        /// <summary>
        /// Initialize all grid data
        /// </summary>
        void InitializeGrid()
        {
            Data = new Grid[gridColumnCount, gridRowCount];

            gridSizeOnXAxis = size.X / gridColumnCount;
            gridSizeOnYAxis = size.Y / gridRowCount;

            // Initialize landscape type
            for (int x = 0; x < gridColumnCount; x++)
                for (int y = 0; y < gridRowCount; y++)
                {
                    Data[x, y].Type = heightField[x, y] > 0 ?
                        LandscapeType.Ground : LandscapeType.Water;

                    Data[x, y].Owners = new List<Entity>(2);
                }
        }

        public IEnumerable<Point> EnumerateGrid(Vector3 position, Vector3 size)
        {
            Point min = PositionToGrid(position.X - size.X / 2, position.Y - size.Y / 2);
            Point max = PositionToGrid(position.X + size.X / 2, position.Y + size.Y / 2);

            if (min.X < 0)
                min.X = 0;
            if (min.Y < 0)
                min.Y = 0;

            if (max.X >= gridColumnCount)
                max.X = gridColumnCount - 1;
            if (max.Y >= gridRowCount)
                max.Y = gridRowCount - 1;

            for (int y = min.Y; y <= max.Y; y++)
                for (int x = min.X; x <= max.X; x++)
                    yield return new Point(x, y);
        }
        #endregion

        #region GridEnumerator

        /// <summary>
        /// Enumerate all the grid points that falls inside a 
        /// rectangle on the XY plane.
        /// </summary>
        private class GridEnumerator : IEnumerable<Point>
        {
            Matrix inverse;
            Point pMin, pMax;
            Vector2 vMin, vMax;
            Vector2 min, max;
            Landscape landscape;

            /// <summary>
            /// Create the rectangle from 2 points and a transform matrix
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate</param>
            /// <param name="min">The minimum point of the bounding box, Z value is ignored</param>
            /// <param name="max">The maximum point of the bounding box, Z value is ignored</param>
            public GridEnumerator(Landscape landscape, Vector3 min, Vector3 max, Vector3 position, float rotationZ)
                : this(landscape, new Vector2(min.X, min.Y), new Vector2(max.X, max.Y),
                Matrix.CreateRotationZ(rotationZ) * Matrix.CreateTranslation(position))
            {
            }            

            /// <summary>
            /// Create the rectangle from 2 points and a transform matrix
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate</param>
            /// <param name="min">The minimum point of the bounding box, Z value is ignored</param>
            /// <param name="max">The maximum point of the bounding box, Z value is ignored</param>
            /// <param name="transform">The matrix used to transform the bounding box</param>
            public GridEnumerator(Landscape landscape, Vector3 min, Vector3 max, Matrix transform)
                : this(landscape, new Vector2(min.X, min.Y), new Vector2(max.X, max.Y), transform)
            {
            }

            /// <summary>
            /// Create the rectangle from size, position and rotation
            /// </summary>
            /// <param name="landscape"></param>
            /// <param name="size"></param>
            /// <param name="position"></param>
            /// <param name="rotationZ"></param>
            public GridEnumerator(Landscape landscape, Vector2 size, Vector3 position, float rotationZ)
                : this(landscape, new Vector2(-size.X/2, -size.Y/2), new Vector2(size.X/2, size.Y/2),
                Matrix.CreateRotationZ(rotationZ) * Matrix.CreateTranslation(position))
            {
            }            
            
            /// <summary>
            /// Create the rectangle from 2 points and a transform matrix
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate</param>
            /// <param name="min">The minimum point of the bounding box</param>
            /// <param name="max">The maximum point of the bounding box</param>
            /// <param name="transform">The matrix used to transform the bounding box</param>
            public GridEnumerator(Landscape landscape, Vector2 min, Vector2 max, Matrix transform)
            {
                this.min = min;
                this.max = max;
                this.landscape = landscape;

                // This is not an fast algorithm, but at least it works :)
                // 
                // 1. Project the rectangle to XY plane and get its
                //    Axis Aligned Bouding Box.
                //
                // 2. For each grid point in the AABB, transform it
                //    to object space, test it with the rectangle

                Vector2[] points = new Vector2[4];

                points[0] = new Vector2(min.X, min.Y);
                points[1] = new Vector2(max.X, min.Y);
                points[2] = new Vector2(min.X, max.Y);
                points[3] = new Vector2(max.X, max.Y);

                vMin = new Vector2(10000, 10000);
                vMax = new Vector2(-10000, -10000);

                for (int i = 0; i < 4; i++)
                {
                    points[i] = Vector2.Transform(points[i], transform);

                    if (points[i].X < vMin.X)
                        vMin.X = points[i].X;
                    if (points[i].X > vMax.X)
                        vMax.X = points[i].X;

                    if (points[i].Y < vMin.Y)
                        vMin.Y = points[i].Y;
                    if (points[i].Y > vMax.Y)
                        vMax.Y = points[i].Y;
                }

                pMin = landscape.PositionToGrid(vMin.X, vMin.Y);
                pMax = landscape.PositionToGrid(vMax.X, vMax.Y);

                // Restrict to map border
                pMin.X = pMin.X < 0 ? 0 : pMin.X;
                pMin.Y = pMin.Y < 0 ? 0 : pMin.Y;
                pMax.X = pMax.X > landscape.GridCount.X ? landscape.GridCount.X : pMax.X;
                pMax.Y = pMax.Y > landscape.GridCount.Y ? landscape.GridCount.Y : pMax.Y;

                // Make sure max is greater than min
                pMax.X = pMax.X < pMin.X ? pMin.X : pMax.X;
                pMax.Y = pMax.Y < pMin.Y ? pMin.Y : pMax.Y;

                // Compute world inverse to transform from world space to object space
                inverse = Matrix.Invert(transform);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns></returns>
            public IEnumerator<Point> GetEnumerator()
            {
                bool[,] grids = new bool[
                    pMax.X - pMin.X + 1,
                    pMax.Y - pMin.Y + 1];

                Vector2 v;
                for (int x = pMin.X; x <= pMax.X; x++)
                    for (int y = pMin.Y; y <= pMax.Y; y++)
                    {
                        v = landscape.GridToPosition(x, y);
                        v = Vector2.Transform(v, inverse);

                        // We can finally test in the object space
                        if (v.X >= min.X && v.X <= max.X &&
                            v.Y >= min.Y && v.Y <= max.Y)
                        {
                            grids[x - pMin.X, y - pMin.Y] = true;
                            yield return new Point(x, y);

                            if (x != pMin.X && !grids[x - pMin.X - 1, y - pMin.Y])
                            {
                                grids[x - pMin.X - 1, y - pMin.Y] = true;
                                yield return new Point(x - 1, y);
                            }

                            if (y != pMin.Y && !grids[x - pMin.X, y - pMin.Y - 1])
                            {
                                grids[x - pMin.X, y - pMin.Y - 1] = true;
                                yield return new Point(x, y - 1);
                            }

                            if (x != pMin.X && y != pMin.Y && !grids[x - pMin.X - 1, y - pMin.Y - 1])
                            {
                                grids[x - pMin.X - 1, y - pMin.Y - 1] = true;
                                yield return new Point(x - 1, y - 1);
                            }
                        }
                    }
            }
            
            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns></returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion
    }
}
