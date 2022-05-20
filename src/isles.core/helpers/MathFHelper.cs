// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public static class MathFHelper
{
    // Difference between 1 and the least value greater than 1 that is representable.
    // Epsilon (1E-45) represents the smallest positive value that is greater than zero
    // which is way too small to be practical.
    private const float Epsilon = 1.19209290e-7F;

    public static float NormalizeRotation(float r)
    {
        while (r > MathF.PI)
            r -= 2 * MathF.PI;
        while (r <= -MathF.PI)
            r += 2 * MathF.PI;
        return r;
    }

    public static bool TryNormalize(this Vector2 v)
    {
        var length = v.Length();
        if (length < Epsilon)
        {
            return false;
        }

        var invLength = 1.0f / length;
        v.X *= invLength;
        v.Y *= invLength;

        return true;
    }
}