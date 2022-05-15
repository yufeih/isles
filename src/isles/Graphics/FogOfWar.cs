// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Graphics;

public class FogOfWar
{
    private const int Size = 128;

    /// <summary>
    /// Gets the default glow texture for each unit.
    /// </summary>
    public static Texture2D Glow
    {
        get
        {
            if (glow == null || glow.IsDisposed)
            {
                glow = BaseGame.Singleton.TextureLoader.LoadTexture("data/ui/glow.png");
            }

            return glow;
        }
    }

    private static Texture2D glow;

    /// <summary>
    /// Gets the width of the mask.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height of the mask.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Objects are invisible when the intensity is below this value.
    /// </summary>
    public const float VisibleIntensity = 0.5f;

    /// <summary>
    /// Common stuff.
    /// </summary>
    private readonly GraphicsDevice graphics;
    private readonly SpriteBatch sprite;
    private Rectangle textureRectangle;

    /// <summary>
    /// Gets the result mask texture (Fog of war).
    /// </summary>
    public Texture2D Mask { get; private set; }

    private RenderTarget2D allFramesVisibleArea;
    private readonly RenderTarget2D thisFrameVisibleArea;
    private readonly RenderTarget2D maskCanvas;

    /// <summary>
    /// Fog intensities.
    /// </summary>
    private readonly bool[] visibility;

    /// <summary>
    /// Visible areas.
    /// </summary>
    private struct Entry
    {
        public float Radius;
        public Vector2 Position;
    }

    private readonly List<Entry> visibleAreas = new();

    public FogOfWar(GraphicsDevice graphics, float width, float height)
    {
        if (graphics == null || width <= 0 || height <= 0)
        {
            throw new ArgumentException();
        }

        Width = width;
        Height = height;
        this.graphics = graphics;
        sprite = new SpriteBatch(graphics);
        visibility = new bool[Size * Size];
        textureRectangle = new Rectangle(0, 0, Size, Size);
        thisFrameVisibleArea = new RenderTarget2D(graphics, Size, Size, true, SurfaceFormat.Color, graphics.PresentationParameters.DepthStencilFormat);
        maskCanvas = new RenderTarget2D(graphics, Size, Size, true, SurfaceFormat.Color, graphics.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
    }

    /// <summary>
    /// Gets the whether the specified point is in the fog of war.
    /// </summary>
    public bool Contains(float x, float y)
    {
        return x <= 0 || y <= 0 || x >= Width || y >= Height || !visibility[Size * (int)(Size * y / Height) + (int)(Size * x / Width)];
    }

    /// <summary>
    /// Call this each frame to mark an area as visible.
    /// </summary>
    /// <remarks>
    /// TODO: Custom glow texture.
    /// </remarks>
    public void DrawVisibleArea(float radius, float x, float y)
    {
        Entry entry;

        entry.Radius = radius;
        entry.Position.X = x;
        entry.Position.Y = y;

        visibleAreas.Add(entry);
    }

    /// <summary>
    /// Call this to refresh the fog of war texture.
    /// </summary>
    public void Refresh()
    {
        if (Mask != null && visibleAreas.Count <= 0)
        {
            return;
        }

        // Draw current glows
        graphics.PushRenderTarget(thisFrameVisibleArea);
        graphics.Clear(Color.Black);

        // Draw glows
        sprite.Begin(SpriteSortMode.Immediate, BlendState.Additive);

        Rectangle destination;
        foreach (Entry entry in visibleAreas)
        {
            destination.X = (int)(Size * (entry.Position.X - entry.Radius) / Width);
            destination.Y = (int)(Size * (entry.Position.Y - entry.Radius) / Height);
            destination.Width = (int)(Size * entry.Radius * 2 / Width);
            destination.Height = (int)(Size * entry.Radius * 2 / Height);

            // Draw the glow texture
            sprite.Draw(Glow, destination, Color.White);
        }

        sprite.End();

        // Draw discovered area texture without clearing it
        if (allFramesVisibleArea is null)
        {
            allFramesVisibleArea = new RenderTarget2D(graphics, Size, Size, true, SurfaceFormat.Color, graphics.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
            graphics.SetRenderTarget(allFramesVisibleArea);
            graphics.Clear(Color.Black);
        }
        else
        {
            graphics.SetRenderTarget(allFramesVisibleArea);
        }

        sprite.Begin(SpriteSortMode.Immediate, BlendState.Additive);
        sprite.Draw(thisFrameVisibleArea, textureRectangle, Color.White);
        sprite.End();

        // Draw final mask texture
        graphics.SetRenderTarget(maskCanvas);
        graphics.Clear(Color.Black);

        sprite.Begin(SpriteSortMode.Immediate, BlendState.Additive);
        sprite.Draw(allFramesVisibleArea, textureRectangle, Color.Gray * 0.5f);
        sprite.Draw(thisFrameVisibleArea, textureRectangle, Color.White);
        sprite.End();

        // Restore states
        graphics.PopRenderTarget();

        Mask = maskCanvas;

        // Manually update intensity map
        UpdateIntensity();

        // Clear visible areas
        visibleAreas.Clear();
    }

    private void UpdateIntensity()
    {
        for (var i = 0; i < visibility.Length; i++)
        {
            visibility[i] = false;
        }

        var CellWidth = Width / Size;
        var CellHeight = Height / Size;

        foreach (Entry entry in visibleAreas)
        {
            var minX = (int)(Size * (entry.Position.X - entry.Radius) / Width);
            var minY = (int)(Size * (entry.Position.Y - entry.Radius) / Height);
            var maxX = (int)(Size * (entry.Position.X + entry.Radius) / Width) + 1;
            var maxY = (int)(Size * (entry.Position.Y + entry.Radius) / Height) + 1;

            if (minX < 0)
            {
                minX = 0;
            }

            if (minY < 0)
            {
                minY = 0;
            }

            if (maxX >= Size)
            {
                maxX = Size - 1;
            }

            if (maxY >= Size)
            {
                maxY = Size - 1;
            }

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    Vector2 v;

                    v.X = x * CellWidth + CellWidth / 2 - entry.Position.X;
                    v.Y = y * CellHeight + CellHeight / 2 - entry.Position.Y;

                    if (v.LengthSquared() <= entry.Radius * entry.Radius)
                    {
                        visibility[y * Size + x] = true;
                    }
                }
            }
        }
    }
}
