// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public static class MathFHelper
{
    public static float NormalizeRotation(float r)
    {
        while (r > MathF.PI)
            r -= 2 * MathF.PI;
        while (r <= -MathF.PI)
            r += 2 * MathF.PI;
        return r;
    }
}