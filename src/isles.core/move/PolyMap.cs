// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class PolyMap
{
    public static int[][] BuildVisibilityMap(Vector2[] vertices)
    {
        var result = new int[vertices.Length][];
        for (var i = 0; i < vertices.Length; i++)
            result[i] = BuildVisibilityMap(vertices, vertices[i]);
        return result;
    }

    public static int[] BuildVisibilityMap(Vector2[] vertices, Vector2 v)
    {
        const int Resolution = 256;
        var scanline = new (int i, float depth)[Resolution];

        // Clear
        for (var i = 0; i < scanline.Length; i++)
            scanline[i] = (-1, float.MaxValue);

        // Draw each line segment
        for (var i = 0; i < vertices.Length; i++)
        {
            var a = vertices[i] - v;
            var b = vertices[(i + 1) % vertices.Length] - v;

            var nearClip = 1f;
            var farClip = 1000f;

            var ya = (-a.Y - nearClip) / (farClip - nearClip);
            var yb = (-b.Y - nearClip) / (farClip - nearClip);

            var ia = (int)Math.Min((a.X / -a.Y + 1) * 0.5f * Resolution, Resolution - 1);
            var ib = (int)Math.Min((b.X / -b.Y + 1) * 0.5f * Resolution, Resolution - 1);

            Console.WriteLine($"{a} --- {b} \t[{ia}, {ib}] -> \t[{ya}, {yb}]");

            if (ia == ib)
            {
                var y = Math.Min(ya, yb);
                if (y < scanline[ia].depth)
                    scanline[ia] = (i, y);
            }
            else
            {
                if (ib < ia)
                {
                    (ia, ib) = (ib, ia);
                    (ya, yb) = (yb, ya);
                }

                var y = ya;
                var dy = (yb - ya) / (ib - ia);

                for (var x = ia; x <= ib; x++)
                {
                    if (y < scanline[x].depth)
                        scanline[x] = (i, y);
                    y += dy;
                }
            }
        }

        // Check visible vertices
        var result = new HashSet<int>();
        foreach (var (i, _) in scanline)
        {
            if (i >= 0)
            {
                result.Add(i);
                result.Add((i + 1) % vertices.Length);
            }
        }

        return result.ToArray();
    }
}
