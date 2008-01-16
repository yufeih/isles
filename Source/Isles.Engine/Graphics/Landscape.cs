using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    /// <summary>
    /// Manipulate terrain, patches for landscape visualization.
    /// Uses Geometry Mipmapping to generate continously level of detailed terrain.
    /// Uses multi-pass technology to render different terrain layers (textures)
    /// </summary>
    public partial class Landscape : IDisposable
    {
        #region Variables

        /// <summary>
        /// Base game
        /// </summary>
        BaseGame game;

        /// <summary>
        /// Graphics device
        /// </summary>
        GraphicsDevice graphics;

        /// <summary>
        /// Terrain size (x, y, z)
        /// </summary>
        float width, depth, height;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the effect used for rendering the terrain
        /// </summary>
        public Effect TerrainEffect
        {
            get { return terrainEffect; }
            //set { terrainEffect = value; }
        }

        /// <summary>
        /// Gets the width of the landscape (x axis)
        /// </summary>
        public float Width
        {
            get { return width; }
        }

        /// <summary>
        /// Gets or sets the error ratio when computing terrain LOD
        /// </summary>
        public float TerrainErrorRatio
        {
            get { return terrainErrorRatio; }
            set { terrainErrorRatio = value; }
        }

        /// <summary>
        /// Gets the depth of the landscape (y axis)
        /// </summary>
        public float Depth
        {
            get { return depth; }
        }

        /// <summary>
        /// Gets the height of the landscape (z axis)
        /// </summary>
        public float Height
        {
            get { return height; }
        }

        /// <summary>
        /// Gets the heightfield data of the landscape
        /// </summary>
        public float[,] HeightField
        {
            get { return heightfield; }
        }

        /// <summary>
        /// Gets terrain vertices
        /// </summary>
        public TerrainVertex[,] TerrainVertices
        {
            get { return terrainVertices; }
        }

        /// <summary>
        /// Gets the number of patches on the x axis
        /// </summary>
        public int PatchCountOnXAxis
        {
            get { return xPatchCount; }
        }

        /// <summary>
        /// Gets the terrain bounding box
        /// </summary>
        public BoundingBox TerrainBoundingBox
        {
            get { return terrainBoundingBox; }
        }

        /// <summary>
        /// Gets the number of patches on the y axis
        /// </summary>
        public int PatchCountOnYAxis
        {
            get { return yPatchCount; }
        }

        #endregion
        
        #region Methods

        public Landscape()
        {
        }

        /// <summary>
        /// Initialize landscape from XNB file
        /// </summary>
        /// <param name="input"></param>
        public Landscape(ContentReader input)
        {
            // Size info
            width = input.ReadSingle();
            depth = input.ReadSingle();
            height = input.ReadSingle();

            // Initialize terrain
            ReadTerrainContent(input);

            // Initialize Water
            ReadWaterContent(input);

            // Initialize sky
            ReadSkyContent(input);

            // Initialize vegetation
            ReadVegetationContent(input);

            // Log landscape info
            Log.Write("Landscape loaded...");
        }

        /// <summary>
        /// Call this everytime a landscape is loaded
        /// </summary>
        public void Initialize(BaseGame game)
        {
            this.game = game;
            this.graphics = game.GraphicsDevice;
            this.surfaceEffect = new BasicEffect(graphics, null);
            this.surfaceDeclaraction = new VertexDeclaration(
                graphics, VertexPositionTexture.VertexElements);

            InitializeWater();
            InitializeSky();
            InitializeTerrain();
            InitializeGrid();
        }

        public void Unload()
        {
            UnloadManualContent();
        }

        /// <summary>
        /// Transform from heightfield grid to real world position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector2 GridToPosition(int x, int y)
        {
            return new Vector2(
                x * width / (gridColumnCount - 1),
                y * depth / (gridRowCount - 1) );
        }

        public Point PositionToGrid(float x, float y)
        {
            return new Point(
                (int)(x * (gridColumnCount - 1) / width),
                (int)(y * (gridRowCount - 1) / depth) );
        }

        /// <summary>
        /// Gets the landscape height of a given point on the heightfield
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetHeight(int x, int y)
        {
            return heightfield[x, y];
        }

        /// <summary>
        /// Gets the landscape height of any given point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetHeight(float x, float y)
        {            
            // Grabbed and modified from racing game
            // We don't want to cause any exception here
            if (x < 0)
                x = 0;
            else if (x >= width)
                x = width - 1;  // x can't be heightfieldWidth-1
                                // or there'll be an out of range
            if (y < 0)          // exception. So is y.
                y = 0;
            else if (y >= depth)
                y = depth - 1;

            // Rescale to our heightfield dimensions
            x *= (gridColumnCount - 1) / width;
            y *= (gridRowCount - 1) / depth;

            // Get the position ON the current tile (0.0-1.0)!!!
            float
                fX = x - ((float)((int)x)),
                fY = y - ((float)((int)y));

            // Interpolate the current position
            int ix2 = (int)x;
            int iy2 = (int)y;

            int ix1 = ix2 + 1;
            int iy1 = iy2 + 1;

            if (fX + fY > 1) // opt. version
            {
                // we are on triangle 1 !!
                //  0____1
                //   \   |
                //    \  |
                //     \ |
                //      \|
                //  2    3
                return
                    heightfield[ix1, iy1] + // 1
                    (1.0f - fX) * (heightfield[ix2, iy1] - heightfield[ix1, iy1]) + // 0
                    (1.0f - fY) * (heightfield[ix1, iy2] - heightfield[ix1, iy1]); // 3
            }
            // we are on triangle 1 !!
            //  0     1
            //  |\  
            //  | \ 
            //  |  \ 
            //  |   \
            //  |    \
            //  2_____3
            return
                heightfield[ix2, iy2] + // 2
                fX * (heightfield[ix1, iy2] - heightfield[ix2, iy2]) +    // 3
                fY * (heightfield[ix2, iy1] - heightfield[ix2, iy2]); // 0
        }

        /// <summary>
        /// Gets a terrain vertex at a given position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public TerrainVertex GetTerrainVertex(int x, int y)
        {
            return terrainVertices[x, y];
        }

        /// <summary>
        /// Gets a given patch
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Patch GetPatch(int x, int y)
        {
            return patches[y * xPatchCount + x];
        }

        /// <summary>
        /// Checks whether a ray intersects the terrain mesh
        /// </summary>
        /// <remarks>
        /// This algorithm has some bugs right now... :(
        /// </remarks>
        /// <param name="ray"></param>
        /// <returns>Intersection point or null if there's no intersection</returns>
        Nullable<Vector3> Intersects(Ray ray, ref List<VertexPositionColor> track)
        {
            // Get two vertices to draw a line through the
            // heightfield.
            //
            // 1. Project the ray to XY plane
            // 2. Compute the 2 intersections of the ray and
            //    terrain bounding box (Projected)
            // 3. Find the 2 points to draw
            int i = 0;
            Vector3[] points = new Vector3[2];

            // Line equation: y = k * (x - x0) + y0
            float k = ray.Direction.Y / ray.Direction.X;
            float invK = ray.Direction.X / ray.Direction.Y;
            float r = ray.Position.Y - ray.Position.X * k;
            if (r >= 0 && r <= depth)
            {
                points[i++] = new Vector3(0, r,
                    ray.Position.Z - ray.Position.X *
                    ray.Direction.Z / ray.Direction.X);
            }
            r = ray.Position.Y + (width - ray.Position.X) * k;
            if (r >= 0 && r <= depth)
            {
                points[i++] = new Vector3(width, r,
                    ray.Position.Z + (width - ray.Position.X) *
                    ray.Direction.Z / ray.Direction.X);
            }
            if (i < 2)
            {
                r = ray.Position.X - ray.Position.Y * invK;
                if (r >= 0 && r <= width)
                    points[i++] = new Vector3(r, 0,
                        ray.Position.Z - ray.Position.Y *
                        ray.Direction.Z / ray.Direction.Y);
            }
            if (i < 2)
            {
                r = ray.Position.X + (depth - ray.Position.Y) * invK;
                if (r >= 0 && r <= width)
                    points[i++] = new Vector3(r, depth,
                        ray.Position.Z + (depth - ray.Position.Y) *
                        ray.Direction.Z / ray.Direction.Y);
            }
            if (i < 2)
                return null;

            // When ray position is inside the box, it should be one
            // of the starting point
            bool inside = ray.Position.X > 0 && ray.Position.X < width &&
                          ray.Position.Y > 0 && ray.Position.Y < depth;

            Vector3 v1 = Vector3.Zero, v2 = Vector3.Zero;
            // Sort the 2 points to make the line follow the direction
            if (ray.Direction.X > 0) 
            {
                if (points[0].X < points[1].X)
                {
                    v2 = points[1];
                    v1 = inside ? ray.Position : points[0];
                }
                else
                {
                    v2 = points[0];
                    v1 = inside ? ray.Position : points[1];
                }
            }
            else if (ray.Direction.X < 0)
            {
                if (points[0].X > points[1].X)
                {
                    v2 = points[1];
                    v1 = inside ? ray.Position : points[0];
                }
                else
                {
                    v2 = points[0];
                    v1 = inside ? ray.Position : points[1];
                }
            }


            //Log.NewLine();
            //Log.Write("v1: " + v1, false);
            //Log.Write("v2: " + v2, false);

            // FIXME: If direction.x equals 0, this algorithm fails.
            //        These 2 cases are Really Really Really unusual,
            //        but we have to add them any way :(

            // Perform a Bresenham line drawing algorithm on
            // the heightfield so that only the points on the
            // heightfield is tested. Interpolation is avoided.

            Point p1 = PositionToGrid(v1.X, v1.Y);
            Point p2 = PositionToGrid(v2.X, v2.Y);

            bool invert = false;
            int x = p1.X;
            int y = p1.Y;
            int sx = p2.X - p1.X;
            int sy = p2.Y - p1.Y;
            int dx = Math.Abs(sx);
            int dy = Math.Abs(sy);

            sx = (sx != 0) ? (sx > 0 ? 1 : -1) : 0;
            sy = (sy != 0) ? (sy > 0 ? 1 : -1) : 0;

            if (dy > dx)
            {
                // Swap dx, dy
                int t = dx;
                dx = dy;
                dy = t;
                invert = true;
            }

            // Init error term
            int e = (dy << 1) - dx;

            int n = dx;
            dx = dx * 2;
            dy = dy * 2;

            // Compute z and dz
            float z = v1.Z, dz;
            //float dz = ray.Direction.Z *
            //    (invert ? width / (heightfieldWidth - 1) :
            //              depth / (heightfieldHeight - 1)) /
            //    (new Vector2(ray.Direction.X, ray.Direction.Y).Length());
            if (invert)
            {
                Vector2 v = Vector2.Normalize(new Vector2(ray.Direction.X, ray.Direction.Z));
                v /= v.X;
                dz = v.Y * width / (gridColumnCount - 1) / v.X;
            }
            else
            {
                Vector2 v = Vector2.Normalize(new Vector2(ray.Direction.Y, ray.Direction.Z));
                v /= v.X;
                dz = v.Y * depth / (gridRowCount - 1) / v.X;
            }
            
            // Start drawing pixels
            for (i = 0; i < n; ++i)
            {
                // Don't test bounding vertices to ease the generation
                // of precise intersection point :)
                if (x > 0 && x < gridColumnCount - 1 &&
                    y > 0 && y < gridRowCount - 1)
                {
                    track.Add(new VertexPositionColor(new Vector3(
                        GridToPosition(x, y), heightfield[x, y]), Color.White));
                    //Log.Write("x: " + x + "\ty: " + y + "\tz: " + z + "\theight: " + heightfield[x, y], false);

                    // Test a pixel
                    if (heightfield[x, y] >= z)
                    {
                        // Find the first intersection, we
                        // need a precise value of the position
                        Vector3[] v = new Vector3[4];

                        Point min = new Point();
                        Point max = new Point();

                        int[] xDirection = new int[] { -1, 0, -1, 0 };
                        int[] yDirection = new int[] { -1, -1, 0, 0 };

                        Point grid;
                        Vector3 ret;
                        for (int m = 0; m < 4; m++)
                        {
                            min.X = x + xDirection[m];
                            min.Y = y + yDirection[m];
                            max.X = min.X + 1;
                            max.Y = min.Y + 1;

                            v[0] = new Vector3(
                                GridToPosition(min.X, min.Y), heightfield[min.X, min.Y]);
                            v[1] = new Vector3(
                                GridToPosition(max.X, min.Y), heightfield[max.X, min.Y]);
                            v[2] = new Vector3(
                                GridToPosition(min.X, max.Y), heightfield[min.X, max.Y]);
                            v[3] = new Vector3(
                                GridToPosition(max.X, max.Y), heightfield[max.X, max.Y]);

                            // Test the first triangles
                            Plane plane = new Plane(v[0], v[1], v[3]);
                            Nullable<float> result = ray.Intersects(plane);
                            if (result != null)
                            {
                                ret = ray.Position + result.Value * ray.Direction;
                                grid = PositionToGrid(ret.X, ret.Y);

                                //Log.Write("Intersection: " + grid + "min: " + min + "max: " + max, false);

                                if (grid.X == min.X || grid.Y == min.Y)
                                    return ret;
                            }

                            // Test the second triangle
                            plane = new Plane(v[0], v[3], v[2]);
                            result = ray.Intersects(plane);
                            if (result != null)
                            {
                                ret = ray.Position + result.Value * ray.Direction;
                                grid = PositionToGrid(ret.X, ret.Y);

                                if (grid.X == min.X || grid.Y == min.Y)
                                    return ret;
                            }
                        }

                        // Any way, return an approximate value
                        //Log.Write(ray.ToString());
                        return new Vector3(GridToPosition(x, y), heightfield[x, y]);
                    }
                }

                while (e > 0)
                {
                    if (invert)
                        x = x + sx;
                    else
                        y = y + sy;

                    e = e - dx;
                }

                if (invert)
                    y = y + sy;
                else
                    x = x + sx;

                e = e + dy;
                z += dz;
            }

            return null;
        }

        /// <summary>
        /// Checks whether a ray intersects the terrain mesh
        /// </summary>
        /// <param name="ray"></param>
        /// <returns>Intersection point or null if there's no intersection</returns>
        public Nullable<Vector3> Intersects(Ray ray)
        {
            // Normalize ray direction
            ray.Direction.Normalize();

            // Get two vertices to draw a line through the
            // heightfield.
            //
            // 1. Project the ray to XY plane
            // 2. Compute the 2 intersections of the ray and
            //    terrain bounding box (Projected)
            // 3. Find the 2 points to draw
            int i = 0;
            Vector3[] points = new Vector3[2];

            // Line equation: y = k * (x - x0) + y0
            float k = ray.Direction.Y / ray.Direction.X;
            float invK = ray.Direction.X / ray.Direction.Y;
            float r = ray.Position.Y - ray.Position.X * k;
            if (r >= 0 && r <= depth)
            {
                points[i++] = new Vector3(0, r,
                    ray.Position.Z - ray.Position.X *
                    ray.Direction.Z / ray.Direction.X);
            }
            r = ray.Position.Y + (width - ray.Position.X) * k;
            if (r >= 0 && r <= depth)
            {
                points[i++] = new Vector3(width, r,
                    ray.Position.Z + (width - ray.Position.X) *
                    ray.Direction.Z / ray.Direction.X);
            }
            if (i < 2)
            {
                r = ray.Position.X - ray.Position.Y * invK;
                if (r >= 0 && r <= width)
                    points[i++] = new Vector3(r, 0,
                        ray.Position.Z - ray.Position.Y *
                        ray.Direction.Z / ray.Direction.Y);
            }
            if (i < 2)
            {
                r = ray.Position.X + (depth - ray.Position.Y) * invK;
                if (r >= 0 && r <= width)
                    points[i++] = new Vector3(r, depth,
                        ray.Position.Z + (depth - ray.Position.Y) *
                        ray.Direction.Z / ray.Direction.Y);
            }
            if (i < 2)
                return null;

            // When ray position is inside the box, it should be one
            // of the starting point
            bool inside = ray.Position.X > 0 && ray.Position.X < width &&
                          ray.Position.Y > 0 && ray.Position.Y < depth;

            Vector3 v1 = Vector3.Zero, v2 = Vector3.Zero;
            // Sort the 2 points to make the line follow the direction
            if (ray.Direction.X > 0)
            {
                if (points[0].X < points[1].X)
                {
                    v2 = points[1];
                    v1 = inside ? ray.Position : points[0];
                }
                else
                {
                    v2 = points[0];
                    v1 = inside ? ray.Position : points[1];
                }
            }
            else if (ray.Direction.X < 0)
            {
                if (points[0].X > points[1].X)
                {
                    v2 = points[1];
                    v1 = inside ? ray.Position : points[0];
                }
                else
                {
                    v2 = points[0];
                    v1 = inside ? ray.Position : points[1];
                }
            }

            // Trace steps along your line and determine the height at each point,
            // for each sample point look up the height of the terrain and determine
            // if the point on the line is above or below the terrain. Once you have
            // determined the two sampling points that are above and below the terrain
            // you can refine using binary searching.
            const float SamplePrecision = 5.0f;
            const int RefineSteps = 5;

            float length = Vector3.Subtract(v2, v1).Length();
            float current = 0;

            Vector3[] point = new Vector3[2];
            Vector3 step = ray.Direction * SamplePrecision;
            point[0] = v1;

            while (current < length)
            {
                if (GetHeight(point[0].X, point[0].Y) >= point[0].Z)
                    break;

                point[0] += step;
                current += SamplePrecision;
            }

            if (current > 0 && current < length)
            {
                // Perform binary search

                Vector3 p = point[0];
                point[1] = point[0] - step;

                for (i = 0; i < RefineSteps; i++)
                {
                    p = (point[0] + point[1]) * 0.5f;

                    if (GetHeight(p.X, p.Y) >= p.Z)
                        point[0] = p;
                    else
                        point[1] = p;
                }

                return p;
            }

            return null;
        } 

        #endregion

        #region Update

        /// <summary>
        /// Update landscape every frame
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Get current view frustum from camera
            BoundingFrustum viewFrustum = game.ViewFrustum;

            bool visible;
            bool LODChanged = false;
            bool visibleAreaChanged = false;
            bool visibleAreaEnlarged = false;

            Vector3 eye = Vector3.Transform(Vector3.Zero, game.ViewInverse);

            for (int i = 0; i < patches.Count; i++)
            {
                // Perform a bounding box test on each terrain patch
                visible = viewFrustum.Intersects(patches[i].BoundingBox);
                if (visible != patches[i].Visible)
                {
                    if (visible)
                        visibleAreaEnlarged = true;
                    visibleAreaChanged = true;
                    patches[i].Visible = visible;
                }
            
                // Update patch LOD if patch visibility has changed
                if (patches[i].UpdateLOD(eye))
                    LODChanged = true;
            }

            // No need to update anything if visibility hasn't changed.
            // (That means terrain LOD hasn't changed too)
            if (!visibleAreaChanged && !LODChanged)
                return;
            
            // If patch LOD hasn't changed and the visible area
            // isn't enlarged, we only need to update index buffers :)
            if (LODChanged || visibleAreaEnlarged)
                UpdateTerrainVertexBuffer();
            UpdateTerrainIndexBufferSet();
        }

        uint[] workingIndices;
        TerrainVertex[] workingVertices;

        uint terrainVertexCount;
        uint[] terrainIndexCount;

        private void UpdateTerrainVertexBuffer()
        {
            if (workingVertices == null)
            {
                workingVertices = new TerrainVertex[
                    6 * xPatchCount * yPatchCount *
                    Patch.MaxPatchResolution * Patch.MaxPatchResolution];
            }

            terrainVertexCount = 0;
            for (int i = 0; i < patches.Count; i++)
            {
                if (patches[i].Visible)
                { 
                    // Update patch starting vertex
                    patches[i].StartingVertex = terrainVertexCount;
                    terrainVertexCount += patches[i].
                        FillVertices(ref workingVertices, terrainVertexCount);
                }
            }

            if (terrainVertexCount > 0)
                terrainVertexBuffer.SetData<TerrainVertex>(
                    workingVertices, 0, (int)terrainVertexCount);
        }

        private void UpdateTerrainIndexBufferSet()
        {
            if (workingIndices == null)
            {
                workingIndices = new uint[
                    6 * xPatchCount * yPatchCount *
                    Patch.MaxPatchResolution * Patch.MaxPatchResolution];
                terrainIndexCount = new uint[patchGroups.Length];
            }

            for (int i = 0; i < patchGroups.Length; i++)
            {
                terrainIndexCount[i] = 0;
                foreach (int index in patchGroups[i])
                {
                    if (patches[index].Visible)
                        terrainIndexCount[i] += patches[index].
                            FillIndices(ref workingIndices, terrainIndexCount[i]);
                }

                if (terrainIndexCount[i] > 0)
                    terrainIndexBufferSet[i].SetData<uint>(
                        workingIndices, 0, (int)terrainIndexCount[i]);
            }
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw landscape
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            graphics.RenderState.CullMode = CullMode.CullClockwiseFace;

            DrawSky(gameTime);
            DrawTerrain(Matrix.Identity, null);
            DrawWater(gameTime);
            DrawVegetation(gameTime);
            
            graphics.RenderState.DepthBufferEnable = true;
            graphics.RenderState.DepthBufferWriteEnable = true;
            graphics.RenderState.CullMode = CullMode.None;
            graphics.RenderState.AlphaBlendEnable = false;
        }

        public void Draw(GameTime gameTime, Matrix lightViewProjection, Texture shadowMap)
        {
            graphics.RenderState.CullMode = CullMode.CullClockwiseFace;

            DrawSky(gameTime);
            DrawTerrain(lightViewProjection, shadowMap);
            DrawWater(gameTime);
            DrawVegetation(gameTime);

            graphics.RenderState.DepthBufferEnable = true;
            graphics.RenderState.DepthBufferWriteEnable = true;
            graphics.RenderState.CullMode = CullMode.None;
            graphics.RenderState.AlphaBlendEnable = false;
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeSky();
                DisposeWater();
                DisposeTerrain();
            }
        }

        #endregion
    }
}
