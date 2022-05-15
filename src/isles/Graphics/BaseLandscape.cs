// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Graphics;

public abstract class BaseLandscape : ILandscape, ITerrain
{
    protected struct Layer
    {
        public Texture2D ColorTexture;
        public Texture2D AlphaTexture;
    }

    protected BaseGame game;
    protected GraphicsDevice graphics;

    /// <summary>
    /// Gets terrain size (x, y, z).
    /// </summary>
    public Vector3 Size => size;

    private Vector3 size;

    /// <summary>
    /// All terrain layers.
    /// </summary>
    protected List<Layer> Layers { get; private set; } = new();

    /// <summary>
    /// Gets the width of grid.
    /// </summary>
    protected int GridCountOnXAxis { get; private set; }

    /// <summary>
    /// Gets the height of grid.
    /// </summary>
    protected int GridCountOnYAxis { get; private set; }

    protected Heightmap Heightmap { get; private set; }

    public virtual void Load(TerrainData data, Heightmap heightnmap, TextureLoader textureLoader)
    {
        Heightmap = heightnmap;

        var w = heightnmap.Width;
        var h = heightnmap.Height;
        size = new((w - 1) * data.Step, (h - 1) * data.Step, data.MaxHeight - data.MinHeight);

        GridCountOnXAxis = w;
        GridCountOnYAxis = h;

        foreach (var layer in data.Layers)
        {
            Layers.Add(new()
            {
                ColorTexture = textureLoader.LoadTexture(layer.ColorTexture),
                AlphaTexture = textureLoader.LoadTexture(layer.AlphaTexture),
            });
        }
    }

    public virtual void Initialize(BaseGame game)
    {
        this.game = game;
        graphics = game.GraphicsDevice;
    }
}
