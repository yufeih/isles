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

    public struct EntityModel
    {
        public string Type { get; init; }

        public Vector3 Position { get; init; }

        public float Rotation { get; init; }
    }

    public struct DecorationModel
    {
        public string Model { get; init; }

        public float Scale { get; init; }

        public float Rotation { get; init; }

        public Vector3 Position { get; init; }
    }
}

public class PrefabModel
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Model { get; init; }

    public IReadOnlyDictionary<string, string>? Attachment { get; init; }

    public int Icon { get; init; }

    public int Snapshot { get; init; }

    public string? HotKey { get; init; }

    public string? Sound { get; init; }

    public string? SoundCombat { get; init; }

    public string? SoundDie { get; init; }

    public float Alpha { get; init; }

    public float ScaleBias { get; init; }

    public float RotationZBias { get; init; }

    public Vector2 ObstructorSize { get; init; }

    public float ViewDistance { get; init; }

    public Vector2 SpawnPoint { get; init; }

    public bool IsUnique { get; init; }

    public Vector2 Defense { get; init; }

    public Vector2 AttackRange { get; init; }

    public Vector2 Attack { get; init; }

    public float AttackDuration { get; init; }

    public float TrainingTime { get; init; }

    public float Speed { get; init; }

    public string[] Spells { get; init; } = Array.Empty<string>();

    public int Lumber { get; init; }

    public int Gold { get; init; }

    public int Food { get; init; }

    public int Health { get; init; }

    public float Priority { get; init; }

    public float ConstructionTime { get; init; }

    public float CoolDown { get; init; }

    public float AreaRadius { get; init; }

    public string? Halo { get; init; }
}