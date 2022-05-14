// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class MiniMap : UIElement
{
    private readonly Rectangle FogOfWarSourceRectangle = new(500, 692, 429, 429);
    private Camera camera;
    private readonly BaseGame game;
    private readonly GameWorld world;
    private const float Factor = 0.7f;
    private const float RadiusFactor = 0.44f;
    private Rectangle actualArea;
    private const double GoldMinePointerFactor = 0.13;
    private Rectangle goldMineSourceRectangle;

    public Rectangle GoldMineSourceRectangle
    {
        get => goldMineSourceRectangle;
        set => goldMineSourceRectangle = value;
    }

    /// <summary>
    /// Gest the Actual Area.
    /// </summary>
    public Rectangle ActualArea
    {
        get
        {
            if (IsDirty)
            {
                actualArea.X = (int)(DestinationRectangle.Left +
                                     DestinationRectangle.Width / 2 * (1 - Factor));
                actualArea.Y = (int)(DestinationRectangle.Top +
                                     DestinationRectangle.Height / 2 * (1 - Factor));

                actualArea.Width = (int)(Factor * DestinationRectangle.Width);
                actualArea.Height = (int)(Factor * DestinationRectangle.Height);
            }

            return actualArea;
        }
    }

    /// <summary>
    /// Gets button destination rectangle.
    /// </summary>
    public override Rectangle DestinationRectangle
    {
        get
        {
            if (IsDirty)
            {
                actualArea.X = (int)(base.DestinationRectangle.Left +
                                     base.DestinationRectangle.Width / 2 * (1 - Factor));
                actualArea.Y = (int)(base.DestinationRectangle.Top +
                                     base.DestinationRectangle.Height / 2 * (1 - Factor));
                actualArea.Width = (int)(Factor * base.DestinationRectangle.Width);
                actualArea.Height = (int)(Factor * base.DestinationRectangle.Height);

                IsDirty = false;
            }

            return base.DestinationRectangle;
        }
    }

    /// <summary>
    /// Gets the center of the actualArea.
    /// </summary>
    public Point Center => new(DestinationRectangle.Left + DestinationRectangle.Width / 2,
                             DestinationRectangle.Top + DestinationRectangle.Height / 2);

    public Texture2D GoldMinePointerTexture { get; set; }

    public MiniMap(BaseGame game, GameWorld world)
    {
        this.game = game;
        camera = game.Camera;
        this.world = world;
    }

    public Vector3? MapToWorld(Point mapPoint)
    {
        if (ActualArea.Contains(mapPoint))
        {
            var rtv = new Vector3
            {
                X = world.Landscape.Size.X * (mapPoint.X - ActualArea.X) / ActualArea.Width,
                Y = world.Landscape.Size.Y * (ActualArea.Bottom - mapPoint.Y) / ActualArea.Height,
            };
            rtv.Z = world.Landscape.GetHeight(rtv.X, rtv.Y);
            return rtv;
        }

        return null;
    }

    private Vector3 MapPointToWorldPositionNegativeAllowed(Point mapPoint)
    {
        var rtv = new Vector3
        {
            X = world.Landscape.Size.X * (mapPoint.X - ActualArea.X) / ActualArea.Width,
            Y = world.Landscape.Size.Y * (ActualArea.Bottom - mapPoint.Y) / ActualArea.Height,
        };
        rtv.Z = world.Landscape.GetHeight(rtv.X, rtv.Y);
        return rtv;
    }

    public Point? WorldToMap(Vector3 position)
    {
        if (position.X >= 0 && position.X <= world.Landscape.Size.X &&
            position.Y >= 0 && position.Y <= world.Landscape.Size.Y)
        {
            var mp = new Point
            {
                X = (int)(position.X / world.Landscape.Size.X * ActualArea.Width + ActualArea.X),
                Y = (int)((1 - position.Y / world.Landscape.Size.Y) * ActualArea.Height + ActualArea.Y),
            };
            return mp;
        }

        return null;
    }

    private Point WorldPositionToMapPointNegativeAllowed(Vector3 position)
    {
        var mp = new Point
        {
            X = (int)(position.X / world.Landscape.Size.X * ActualArea.Width + ActualArea.X),
            Y = (int)((1 - position.Y / world.Landscape.Size.Y) * ActualArea.Height + ActualArea.Y),
        };
        return mp;
    }

    public override void Draw(GameTime gameTime, SpriteBatch sprite)
    {
        sprite.Draw(Texture, DestinationRectangle, SourceRectangle, Color.White);
        sprite.Draw(Texture, DestinationRectangle, FogOfWarSourceRectangle, new Color(new Vector4(1, 1, 1, 0.6f)));

        // Draw the gold
        Rectangle goldMineDest;
        goldMineDest.Width = (int)(GoldMinePointerFactor * DestinationRectangle.Width);
        goldMineDest.Height = goldMineDest.Width;

        foreach (var goldmine in world.WorldObjects.OfType<Goldmine>())
        {
            var goldMinePointerPosition = WorldToMap(goldmine.Position);
            if (goldMinePointerPosition == null)
            {
                throw new InvalidOperationException("Gold Mine position out of range.");
            }

            goldMineDest.X = goldMinePointerPosition.Value.X - goldMineDest.Width / 2;
            goldMineDest.Y = goldMinePointerPosition.Value.Y - goldMineDest.Height / 2;
            sprite.Draw(GoldMinePointerTexture, goldMineDest, goldMineSourceRectangle, Color.White);
        }

        // Draw the entities
        DrawEntitySign();

        // Draw the sight region rectangle
        DrawSightRegion();
    }

    private void DrawSightRegion()
    {
        var sightRegion = new Point[4];

        var xyPlane = new Plane(0, 0, 1, 0);
        Ray ray = game.Unproject(0, 0);
        var dist = ray.Intersects(xyPlane);
        if (dist == null)
        {
            return;
        }

        Vector3 intersectPoint = ray.Position + Vector3.Normalize(ray.Direction) * dist.Value;
        sightRegion[0] = WorldPositionToMapPointNegativeAllowed(intersectPoint);

        ray = game.Unproject(game.ScreenWidth, 0);
        dist = ray.Intersects(xyPlane);
        if (dist == null)
        {
            return;
        }

        intersectPoint = ray.Position + Vector3.Normalize(ray.Direction) * dist.Value;
        sightRegion[1] = WorldPositionToMapPointNegativeAllowed(intersectPoint);

        ray = game.Unproject(game.ScreenWidth, game.ScreenHeight);
        dist = ray.Intersects(xyPlane);
        if (dist == null)
        {
            return;
        }

        intersectPoint = ray.Position + Vector3.Normalize(ray.Direction) * dist.Value;
        sightRegion[2] = WorldPositionToMapPointNegativeAllowed(intersectPoint);

        ray = game.Unproject(0, game.ScreenHeight);
        dist = ray.Intersects(xyPlane);
        if (dist == null)
        {
            return;
        }

        intersectPoint = ray.Position + Vector3.Normalize(ray.Direction) * dist.Value;
        sightRegion[3] = WorldPositionToMapPointNegativeAllowed(intersectPoint);

        Graphics2D.DrawLine(sightRegion[0], sightRegion[1], Color.White);
        Graphics2D.DrawLine(sightRegion[1], sightRegion[2], Color.White);
        Graphics2D.DrawLine(sightRegion[2], sightRegion[3], Color.White);
        Graphics2D.DrawLine(sightRegion[3], sightRegion[0], Color.White);
    }

    public void DrawGameObject(Vector3 position, float size, Color color)
    {
        EntitySign sign;

        Point mapPoint = WorldPositionToMapPointNegativeAllowed(position);

        sign.Tint = color;
        sign.Destination = new Rectangle(mapPoint.X - (int)(size / 2),
                                         mapPoint.Y - (int)(size / 2), (int)size, (int)size);

        entitySigns.Add(sign);
    }

    private struct EntitySign
    {
        public Rectangle Destination;
        public Color Tint;
    }

    private readonly List<EntitySign> entitySigns = new();

    private void DrawEntitySign()
    {
        foreach (EntitySign sign in entitySigns)
        {
            Graphics2D.DrawRectangle(sign.Destination, sign.Tint);
        }

        entitySigns.Clear();
    }

    public override void Update(GameTime gameTime)
    {
        if (camera == null)
        {
            camera = game.Camera;
        }

        if (draging)
        {
            var dist = new Vector2(game.Input.MousePosition.X - Center.X,
                                       game.Input.MousePosition.Y - Center.Y);
            if (dist.X * dist.X + dist.Y * dist.Y <=
                RadiusFactor * DestinationRectangle.Width * RadiusFactor * DestinationRectangle.Width)
            {
                Vector3 position = MapPointToWorldPositionNegativeAllowed(game.Input.MousePosition);
                camera.FlyTo(position, true);
            }
            else
            {
                var offset = new Vector2(dist.X, dist.Y);
                offset = RadiusFactor * DestinationRectangle.Width * Vector2.Normalize(offset);
                Mouse.SetPosition((int)offset.X + Center.X, (int)offset.Y + Center.Y);
            }
        }
    }

    private bool draging;

    public override EventResult HandleEvent(EventType type, object sender, object tag)
    {
        if (Enabled && Visible)
        {
            var input = sender as Input;
            var dist = new Vector2(input.MousePosition.X - Center.X,
                                        input.MousePosition.Y - Center.Y);
            if (type == EventType.LeftButtonDown &&
                dist.X * dist.X + dist.Y * dist.Y <=
                RadiusFactor * DestinationRectangle.Width * RadiusFactor * DestinationRectangle.Width)
            {
                var pointTo = new Point();
                draging = true;
                pointTo.X = input.MousePosition.X - ActualArea.X;
                pointTo.Y = input.MousePosition.Y - ActualArea.Y;
                Vector3? position = MapToWorld(input.MousePosition);
                if (position != null)
                {
                    camera.FlyTo(position.Value, true);
                    return EventResult.Handled;
                }
            }

            if (type == EventType.LeftButtonUp &&
                dist.X * dist.X + dist.Y * dist.Y <=
                RadiusFactor * DestinationRectangle.Width * RadiusFactor * DestinationRectangle.Width)
            {
                draging = false;
                return EventResult.Unhandled;
            }

            if (type == EventType.RightButtonDown &&
                dist.X * dist.X + dist.Y * dist.Y <=
                RadiusFactor * DestinationRectangle.Width * RadiusFactor * DestinationRectangle.Width)
            {
                Vector3? point = MapToWorld(input.MousePosition);
                if (point.HasValue)
                {
                    Player.LocalPlayer.PerformAction(point.Value);
                }

                return EventResult.Handled;
            }
        }

        return EventResult.Unhandled;
    }
}
