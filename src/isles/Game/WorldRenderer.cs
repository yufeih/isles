// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public readonly struct ViewMatrices
{
    public readonly Matrix View;
    public readonly Matrix Projection;
    public readonly Matrix ViewProjection;
    public readonly Matrix ViewProjectionInverse;
    public readonly Matrix ViewInverse;
    public readonly Matrix ProjectionInverse;
    public readonly Vector3 Eye;
    public readonly Vector3 Facing;

    public ViewMatrices(in Matrix view, in Matrix projection)
    {
        View = view;
        Projection = projection;
        ViewProjection = view * Projection;
        ViewInverse = Matrix.Invert(view);
        ProjectionInverse = Matrix.Invert(Projection);

        // Guess this is more accurate
        ViewProjectionInverse = ProjectionInverse * ViewInverse;

        // Update eye / facing / right
        Eye.X = ViewInverse.M41;
        Eye.Y = ViewInverse.M42;
        Eye.Z = ViewInverse.M43;

        Facing.X = -view.M13;
        Facing.Y = -view.M23;
        Facing.Z = -view.M33;
    }
}

public class WorldRenderer
{
    private readonly Input _input;
    private readonly ShadowEffect _shadow;
    private readonly Settings _settings;
    private readonly ModelRenderer _modelRenderer;
    private readonly ModelPicker<Entity> _modelPicker;
    private readonly GameModel _rallyPointModel = new("Models/rally");
    private Entity _pickedEntity;

    public Entity Pick() => _pickedEntity;

    public WorldRenderer(
        GraphicsDevice graphics, Settings settings, ModelRenderer modelRenderer, ShaderLoader shaderLoader, Input input)
    {
        _input = input;
        _settings = settings;
        _modelRenderer = modelRenderer;
        _shadow = settings.ShadowEnabled ? new(graphics, shaderLoader) : null;
        _modelPicker = new(graphics, modelRenderer);
    }

    public void Draw(GameWorld world, in ViewMatrices matrices, GameTime gameTime)
    {
        var objectMap = _modelPicker.DrawObjectMap(
            matrices.ViewProjection,
            world.WorldObjects.OfType<Entity>().Where(entity => entity.IsPickable),
            entity => entity.GameModel);

        _pickedEntity = objectMap.Pick();

        // Collect models
        _modelRenderer.Clear();
        foreach (var entity in world.WorldObjects)
        {
            Draw(entity, gameTime);
        }

        // Generate shadow map
        if (_shadow != null)
        {
            _shadow.Begin(matrices.Eye, matrices.Facing);
            _modelRenderer.DrawShadowMap(_shadow);
            _shadow.End();
        }

        // Draw spell
        Spell.CurrentSpell?.Draw(gameTime);

        world.Water.Draw(gameTime, matrices, world.Terrain.FogTexture);

        // Draw shadow receivers with the shadow map
        world.Terrain.DrawTerrain(_shadow, matrices);

        // Present surface
        world.Terrain.PresentSurface(matrices);

        // FIXME: There are some weired things when models are drawed after
        // drawing the terrain... Annoying...
        _modelRenderer.Draw(matrices.ViewProjection, true, false);

        // TODO: Draw particles with ZEnable = true, ZWriteEnable = false
        ParticleSystem.Present(matrices);

        _modelRenderer.Draw(matrices.ViewProjection, false, true);
    }

    private void Draw(BaseEntity baseEntity, GameTime gameTime)
    {
        switch (baseEntity)
        {
            case GameObject gameObject:
                DrawGameObject(gameObject, gameTime);
                break;
            case Entity entity:
                DrawEntity(entity);
                break;
        }
    }

    private void DrawEntity(Entity entity)
    {
        if (entity.GameModel != null && entity.Visible && entity.WithinViewFrustum)
        {
            entity.GameModel.Draw();
        }
    }

    private void DrawGameObject(GameObject entity, GameTime gameTime)
    {
        if (entity is Charactor charactor)
        {
            if (charactor.ShowGlow && charactor.ShouldDrawModel)
            {
                if (charactor.Glow == null)
                {
                    charactor.Glow = new EffectGlow(charactor);
                }

                charactor.Glow.Update(gameTime);
                charactor.ShowGlow = false;
            }
        }

        // Flash the model
        if (entity.flashElapsedTime <= GameObject.FlashDuration)
        {
            var glow = (float)Math.Sin(MathHelper.Pi * entity.flashElapsedTime / GameObject.FlashDuration);
            entity.GameModel.Glow = new Vector3(MathHelper.Clamp(glow, 0, 1));

            entity.flashElapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (entity.flashElapsedTime > GameObject.FlashDuration)
            {
                entity.GameModel.Glow = default;
            }
        }

        // Draw model
        if (!entity.InFogOfWar)
        {
            DrawEntity(entity);

            if (entity.Owner != null)
            {
                GameUI.Singleton.Minimap.DrawGameObject(entity.Position, 4, entity.Owner.TeamColor);
            }
        }

        // Draw copyed model shadow
        if (entity.InFogOfWar && entity.Spotted && entity.VisibleInFogOfWar && entity.modelShadow != null)
        {
            if (entity.WithinViewFrustum)
            {
                entity.modelShadow.Draw();
            }

            if (entity.Owner != null)
            {
                GameUI.Singleton.Minimap.DrawGameObject(entity.Position, 4, entity.Owner.TeamColor);
            }
        }

        // Draw attachments
        if (entity.ShouldDrawModel)
        {
            switch (entity)
            {
                case Worker worker:
                    DrawWorkerAttachments(worker);
                    break;
                case Hunter hunter:
                    DrawHunterAttachments(hunter);
                    break;
                default:
                    DrawAttachments(entity);
                    break;
            }
        }

        // Draw status
        if (entity.Visible && !entity.InFogOfWar && entity.WithinViewFrustum && entity.ShowStatus)
        {
            if (entity.Selected && entity.IsAlive)
            {
                entity.World.Terrain.DrawSurface(
                    entity.AreaRadius > 16 ?
                    GameObject.SelectionAreaTextureLarge : GameObject.SelectionAreaTexture, 
                    entity.Position,
                    entity.AreaRadius * 2, entity.AreaRadius * 2,
                    entity.Owner == null ? Color.Yellow : (
                        entity.Owner.GetRelation(Player.LocalPlayer) == PlayerRelation.Opponent ?
                                            Color.Red : Color.GreenYellow));
            }

            if (entity.IsAlive && entity.MaxHealth > 0)
            {
                if (entity.Highlighted || _input.Keyboard.IsKeyDown(Keys.LeftAlt) || _input.Keyboard.IsKeyDown(Keys.RightAlt))
                {
                    var color = ProgressColor(1.0f * entity.health / entity.MaxHealth);
                    GameUI.Singleton.DrawProgress(entity.TopCenter, 0, (int)(entity.AreaRadius * 10.0f),
                        100 * entity.Health / entity.MaxHealth, color);

                    if (entity is Building building)
                    {
                        DrawBuildingStatus(building);
                    }
                }
            }
        }

        if (entity is Building building1)
        {
            DrawRallyPoints(building1);
        }

        static Color ProgressColor(float percentage)
        {
            Vector3 v, v1, v2;

            if (percentage > 0.5f)
            {
                percentage = (percentage - 0.5f) * 2;
                v1 = Color.Yellow.ToVector3();
                v2 = Color.Green.ToVector3();
            }
            else
            {
                percentage *= 2;
                v1 = Color.Red.ToVector3();
                v2 = Color.Yellow.ToVector3();
            }

            v.X = MathHelper.Lerp(v1.X, v2.X, percentage);
            v.Y = MathHelper.Lerp(v1.Y, v2.Y, percentage);
            v.Z = MathHelper.Lerp(v1.Z, v2.Z, percentage);

            return new Color(v);
        }
    }

    private void DrawWorkerAttachments(Worker worker)
    {
        foreach (var (model, _) in worker.Attachment)
        {
            if (model == worker.wood)
            {
                if (worker.LumberCarried > 0)
                {
                    model.Draw();
                }
            }
            else if (model == worker.gold)
            {
                if (worker.GoldCarried > 0)
                {
                    model.Draw();
                }
            }
            else
            {
                model.Draw();
            }
        }
    }

    private void DrawHunterAttachments(Hunter hunter)
    {
        foreach (var (model, _) in hunter.Attachment)
        {
            if (model == hunter.weapon && hunter.weaponVisible || model != hunter.weapon)
            {
                model.Draw();
            }
        }
    }

    private void DrawAttachments(GameObject entity)
    {
        foreach (var (model, _) in entity.Attachment)
        {
            model.Draw();
        }
    }

    private void DrawBuildingStatus(Building building)
    {
        if (building.state == Building.BuildingState.Constructing)
        {
            GameUI.Singleton.DrawProgress(building.TopCenter, 5,
                                        (int)(building.AreaRadius * 10.0f),
                                        100 * building.ConstructionTimeElapsed / building.ConstructionTime,
                                        Color.Orange);
        }
    }

    private void DrawRallyPoints(Building building)
    {
        // Draw rally point
        if (building.Selected && building.ShouldDrawModel && building.IsAlive && building.Owner is LocalPlayer &&
            (building.state == Building.BuildingState.Normal || building.state == Building.BuildingState.Constructing) &&
            building.RallyPoints.Count > 0)
        {
            Vector3 position = Vector3.Zero;

            if (building.RallyPoints[0] is Entity)
            {
                position = (building.RallyPoints[0] as Entity).Position;
            }
            else if (building.RallyPoints[0] is Vector3 vector)
            {
                position = vector;
            }

            _rallyPointModel.Transform = Matrix.CreateTranslation(position);
            _rallyPointModel.Draw();
        }
    }
}
