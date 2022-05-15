// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Isles;

public class GameWorld
{
    public static GameWorld Singleton { get; set; }

    private readonly List<BaseEntity> _pendingAdds = new();
    private readonly List<BaseEntity> _pendingRemoves = new();

    public IEnumerable<BaseEntity> WorldObjects => _worldObjects;

    private readonly List<BaseEntity> _worldObjects = new();

    public BaseGame Game { get; } = BaseGame.Singleton;

    public Terrain Terrain { get; private set; }

    public Water Water { get; private set; }

    public Heightmap Heightmap { get; private set; }

    public PathManager PathManager { get; private set; }

    public FogOfWar FogOfWar { get; private set; }

    public Func<Entity> Pick { get; set; }

    public void Update(GameTime gameTime)
    {
        Flush();

        foreach (var o in _worldObjects)
        {
            o.Update(gameTime);
        }

        if (FogOfWar != null)
        {
            FogOfWar.Refresh();
        }

        PathManager.Update();
    }

    public void Load(LevelModel model, ILoading context)
    {
        context.Refresh(2);

        var terrainData = JsonSerializer.Deserialize<TerrainData>(File.ReadAllBytes(model.Landscape));

        Heightmap = Heightmap.Load(terrainData.Heightmap, terrainData.Step, terrainData.MinHeight, terrainData.MaxHeight);
        Terrain = new(Game.GraphicsDevice, terrainData, Heightmap, Game.ShaderLoader, Game.TextureLoader);
        Water = new(Game.GraphicsDevice, terrainData, Game.ShaderLoader, Game.TextureLoader);
        FogOfWar = new(Game.GraphicsDevice, Heightmap.Size.X, Heightmap.Size.Y);
        PathManager = new(Heightmap, model.PathOccluders);

        context.Refresh(10);

        // Load world objects
        foreach (ref readonly var entity in model.Entities.AsSpan())
        {
            if (Create(entity.Type) is var worldObject)
            {
                worldObject.Position = entity.Position;
                if (worldObject is Goldmine goldmine)
                {
                    goldmine.SetRotation(entity.Rotation);
                }
                Add(worldObject);
            }
        }

        var rotateX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.PiOver2);
        
        foreach (ref readonly var decoration in model.Decorations.AsSpan())
        {
            var worldObject = new Decoration();
            worldObject.GameModel = new(decoration.Model);
            worldObject.Position = decoration.Position;
            worldObject.Rotation = rotateX * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(decoration.Rotation));
            worldObject.Scale = decoration.Scale * Vector3.One;
            Add(worldObject);
        }

        Flush();
    }

    public BaseEntity Create(string typeName)
    {
        var entity = JsonSerializer.Deserialize<BaseEntity>(GameDefault.Singleton.Prefabs[typeName], JsonHelper.Options);
        entity.ClassID = typeName;
        return entity;
    }

    public void Add(BaseEntity worldObject)
    {
        _pendingAdds.Add(worldObject);

        if (worldObject is Entity entity)
        {
            entity.OnCreate();
        }
    }

    public void Remove(BaseEntity worldObject)
    {
        if (worldObject is Entity e)
        {
            e.OnDestroy();
        }

        if (worldObject == null)
        {
            return;
        }
        _pendingRemoves.Add(worldObject);
    }

    public void Flush()
    {
        foreach (var worldObject in _pendingAdds)
        {
            _worldObjects.Add(worldObject);
        }
        _pendingAdds.Clear();

        foreach (var worldObject in _pendingRemoves)
        {
            _worldObjects.Remove(worldObject);
        }
        _pendingRemoves.Clear();
    }

    public IEnumerable<BaseEntity> ObjectsFromRegion(BoundingFrustum boundingFrustum)
    {
        // This is a really slow method
        foreach (var o in _worldObjects)
        {
            if (o is Entity e && e.Intersects(boundingFrustum))
            {
                yield return e;
            }
        }
    }

    public IEnumerable<BaseEntity> GetNearbyObjects(Vector3 position, float radius)
    {
        foreach (var o in _worldObjects)
        {
            Vector2 v;

            v.X = o.Position.X - position.X;
            v.Y = o.Position.Y - position.Y;

            if (v.LengthSquared() <= radius * radius)
            {
                yield return o;
            }
        }
    }
}
