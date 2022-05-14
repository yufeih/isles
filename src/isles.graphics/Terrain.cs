// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public interface ITerrain
{
    Vector3 Size { get; }

    float GetHeight(float x, float y);
}
