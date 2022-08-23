// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

static class MoveCollision
{
    public static bool CollideMovables(in Movable a, in Movable b, ref Vector2 normal, ref float penetration)
    {
        normal = b.Position - a.Position;
        var distanceSq = normal.LengthSquared();
        if (distanceSq >= (a.Radius + b.Radius) * (a.Radius + b.Radius) ||
            distanceSq <= MathFHelper.Epsilon * MathFHelper.Epsilon)
            return false;

        var distance = MathF.Sqrt(distanceSq);
        normal /= distance;
        penetration = a.Radius + b.Radius - distance;
        return true;
    }
}
