// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class LevelModel
{
    public string Landscape { get; init; } = "";

    public Vector2[] SpawnPoints { get; init; } = Array.Empty<Vector2>();

    public Point[] PathOccluders { get; init; } = Array.Empty<Point>();

    public EntityModel[] Entities { get; init; } = Array.Empty<EntityModel>();

    public DecorationModel[] Decorations { get; init; } = Array.Empty<DecorationModel>();

    public class EntityModel
    {
        public string Type { get; init; } = "";

        public Vector3 Position { get; init; }

        public float Rotation { get; init; }
    }

    public class DecorationModel
    {
        public string Model { get; init; } = "";

        public Vector3 Scale { get; init; }

        public Vector3 Position { get; init; }

        public float RotationX { get; init; }

        public float RotationY { get; init; }

        public float RotationZ { get; init; }
    }
}