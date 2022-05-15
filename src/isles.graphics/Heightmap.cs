// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Graphics;

public class Heightmap
{
    public int Width { get; }
    public int Height { get; }
    public float Step { get; }
    public Half[] Heights { get; }
    
    public Vector2 Size => new((Width - 1) * Step, (Height - 1) * Step);

    public Heightmap(int width, int height, float step, Half[] heights)
    {
        Width = width;
        Height = height;
        Step = step;
        Heights = heights;
    }

    public static Heightmap Load(string file, float step, float minHeight, float maxHeight)
    {
        var pixels = TextureLoader.ReadAllPixels(file, out var w, out var h);
        var heights = new Half[w * h];
        
        for (var i = 0; i < heights.Length; i++)
            heights[i] = (Half)(minHeight + (maxHeight - minHeight) * pixels[i].R / 255f);

        return new(w, h, step, heights);
    }

    public float GetHeight(float x, float y)
    {
        x /= Step;
        y /= Step;

        // Interpolate the current position
        var ix2 = Math.Max(0, Math.Min((int)x, Width - 2));
        var iy2 = Math.Max(0, Math.Min((int)y, Height - 2));

        var ix1 = ix2 + 1;
        var iy1 = iy2 + 1;

        // Get the position ON the current tile (0.0-1.0)!!!
        var fX = MathHelper.Clamp(x - (int)x, 0, 1);
        var fY = MathHelper.Clamp(y - (int)y, 0, 1);

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
                (float)Heights[ix1 + iy1 * Width] + // 1
                (1.0f - fX) * ((float)Heights[ix2 + iy1 * Width] - (float)Heights[ix1 + iy1 * Width]) + // 0
                (1.0f - fY) * ((float)Heights[ix1 + iy2 * Width] - (float)Heights[ix1 + iy1 * Width]); // 3
        }

        // we are on triangle 1 !!
        //  0     1
        //  |\
        //  | \
        //  |  \
        //  |   \
        //  |    \
        //  2_____3
        var height =
            (float)Heights[ix2 + iy2 * Width] + // 2
            fX * ((float)Heights[ix1 + iy2 * Width] - (float)Heights[ix2 + iy2 * Width]) + // 3
            fY * ((float)Heights[ix2 + iy1 * Width] - (float)Heights[ix2 + iy2 * Width]); // 0

        // For those area underwater, we set the height to zero
        return height;// < 0 ? 0 : height;
    }

    public Vector3? Raycast(Ray ray)
    {
        var sizeX = Width * Step;
        var sizeY = Height * Step;

        // Normalize ray direction
        ray.Direction.Normalize();

        // Get two vertices to draw a line through the
        // Heights.
        //
        // 1. Project the ray to XY plane
        // 2. Compute the 2 intersections of the ray and
        //    terrain bounding box (Projected)
        // 3. Find the 2 points to draw
        var i = 0;
        var points = new Vector3[2];

        // Line equation: y = k * (x - x0) + y0
        var k = ray.Direction.Y / ray.Direction.X;
        var invK = ray.Direction.X / ray.Direction.Y;
        var r = ray.Position.Y - ray.Position.X * k;
        if (r >= 0 && r <= sizeY)
        {
            points[i++] = new Vector3(0, r,
                ray.Position.Z - ray.Position.X *
                ray.Direction.Z / ray.Direction.X);
        }

        r = ray.Position.Y + (sizeX - ray.Position.X) * k;
        if (r >= 0 && r <= sizeY)
        {
            points[i++] = new Vector3(sizeX, r,
                ray.Position.Z + (sizeX - ray.Position.X) *
                ray.Direction.Z / ray.Direction.X);
        }

        if (i < 2)
        {
            r = ray.Position.X - ray.Position.Y * invK;
            if (r >= 0 && r <= sizeX)
            {
                points[i++] = new Vector3(r, 0,
                    ray.Position.Z - ray.Position.Y *
                    ray.Direction.Z / ray.Direction.Y);
            }
        }

        if (i < 2)
        {
            r = ray.Position.X + (sizeY - ray.Position.Y) * invK;
            if (r >= 0 && r <= sizeX)
            {
                points[i++] = new Vector3(r, sizeY,
                    ray.Position.Z + (sizeY - ray.Position.Y) *
                    ray.Direction.Z / ray.Direction.Y);
            }
        }

        if (i < 2)
        {
            return null;
        }

        // When ray position is inside the box, it should be one
        // of the starting point
        var inside = ray.Position.X > 0 && ray.Position.X < sizeX &&
                      ray.Position.Y > 0 && ray.Position.Y < sizeY;

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

        // Trace steps along your line and determine the size.Z at each point,
        // for each sample point look up the size.Z of the terrain and determine
        // if the point on the line is above or below the terrain. Once you have
        // determined the two sampling points that are above and below the terrain
        // you can refine using binary searching.
        const float SamplePrecision = 5.0f;
        const int RefineSteps = 5;

        var length = Vector3.Subtract(v2, v1).Length();
        float current = 0;

        Span<Vector3> point = stackalloc Vector3[2];
        Vector3 step = ray.Direction * SamplePrecision;
        point[0] = v1;

        while (current < length)
        {
            if (GetHeight(point[0].X, point[0].Y) >= point[0].Z)
            {
                break;
            }

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
                {
                    point[0] = p;
                }
                else
                {
                    point[1] = p;
                }
            }

            return p;
        }

        return null;
    }
}
