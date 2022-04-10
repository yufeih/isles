// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public static class PlotLine
{
    public static bool BresenhamLine<T>(
        int x0, int y0, int x1, int y1, Func<int, int, T, bool> setPixel, T state)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = (dx > dy ? dx : -dy) / 2;

        while (true)
        {
            if (setPixel(x0, y0, state))
            {
                return true;
            }

            if (x0 == x1 && y0 == y1)
            {
                return false;
            }

            var err = error;
            if (err > -dx)
            {
                error -= dy;
                x0 += sx;
            }
            if (err < dy)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    public static bool WuLine<T>(
        int x0, int y0, int x1, int y1, Func<int, int, float, T, bool> setPixel, T state)
    {
        if (x0 == x1)
        {
            if (y0 > y1)
            {
                (y0, y1) = (y1, y0);
            }
            for (var y = y0; y <= y1; y++)
            {
                if (setPixel(x0, y, 1, state))
                {
                    return true;
                }
            }
            return false;
        }

        if (y0 == y1)
        {
            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
            }
            for (var x = x0; x <= x1; x++)
            {
                if (setPixel(x, y0, 1, state))
                {
                    return true;
                }
            }
            return false;
        }

        if (setPixel(x0, y0, 1, state) || setPixel(x1, y1, 1, state))
        {
            return true;
        }

        if (Math.Abs(x1 - x0) >= Math.Abs(y1 - y0))
        {
            if (x0 > x1)
            {
                (x0, x1, y0, y1) = (x1, x0, y1, y0);
            }

            var gradient = 1.0f * (y1 - y0) / (x1 - x0);
            var total = (float)y0 + gradient;

            for (var x = x0 + 1; x < x1; x++)
            {
                var i = (int)total;
                var f = total - i;
                if (setPixel(x, i, 1 - f, state) || setPixel(x, i + 1, f, state))
                {
                    return true;
                }
                total += gradient;
            }
        }
        else
        {
            if (y0 > y1)
            {
                (x0, x1, y0, y1) = (x1, x0, y1, y0);
            }

            var gradient = 1.0f * (x1 - x0) / (y1 - y0);
            var total = (float)x0 + gradient;

            for (var y = y0 + 1; y < y1; y++)
            {
                var i = (int)total;
                var f = total - i;
                if (setPixel(i, y, 1 - f, state) || setPixel(i + 1, y, f, state))
                {
                    return true;
                }
                total += gradient;
            }
        }

        return false;
    }
}
