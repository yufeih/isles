// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class WorldRenderer
{
    private readonly ShadowEffect _shadow;
    private readonly Settings _settings;
    private readonly ModelRenderer _modelRenderer;
    private readonly ModelPicker<Entity> _modelPicker;
    private Entity _pickedEntity;

    public Entity Pick() => _pickedEntity;

    public WorldRenderer(GraphicsDevice graphics, Settings settings, ModelRenderer modelRenderer, ShadowEffect shadow)
    {
        _shadow = shadow;
        _settings = settings;
        _modelRenderer = modelRenderer;
        _modelPicker = new ModelPicker<Entity>(graphics, modelRenderer);
    }

    public void Draw(GameWorld world, Matrix viewProjection, Vector3 eye, Vector3 facing, GameTime gameTime)
    {
        var objectMap = _modelPicker.DrawObjectMap(
            viewProjection,
            world.WorldObjects.OfType<Entity>().Where(entity => entity.IsPickable),
            entity => entity.Model);

        _pickedEntity = objectMap.Pick();

        if (_settings.ReflectionEnabled)
        {
            world.Landscape.UpdateWaterReflectionAndRefraction(
                gameTime, (gameTime, view, projection) =>
                {
                    foreach (var o in world.WorldObjects)
                    {
                        o.DrawReflection(gameTime, view, projection);
                    }
                });
        }

        // Generate shadow map
        if (_shadow != null)
        {
            _shadow.Begin(eye, facing);

            _modelRenderer.Clear();

            // Draw shadow casters. Currently we only draw all world object
            foreach (var o in world.WorldObjects)
            {
                o.DrawShadowMap(gameTime, _shadow);
            }

            _modelRenderer.DrawShadowMap(_shadow);

            // Resolve shadow map
            _shadow.End();
        }

        _modelRenderer.Clear();

        // Draw world objects before landscape
        foreach (var o in world.WorldObjects)
        {
            o.Draw(gameTime);
        }

        // Draw spell
        Spell.CurrentSpell?.Draw(gameTime);

        world.Landscape.DrawWater(gameTime);

        // Draw shadow receivers with the shadow map
        world.Landscape.DrawTerrain(_shadow);

        // Present surface
        world.Landscape.PresentSurface();

        // FIXME: There are some weired things when models are drawed after
        // drawing the terrain... Annoying...
        _modelRenderer.Draw(viewProjection, true, false);

        // TODO: Draw particles with ZEnable = true, ZWriteEnable = false
        ParticleSystem.Present();

        _modelRenderer.Draw(viewProjection, false, true);
    }
}
