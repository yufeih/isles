//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;
using Isles.Engine;
using Isles.UI;


namespace Isles
{
    public class MiniMap : UIElement
    {
        readonly Rectangle FogOfWarSourceRectangle = new Rectangle(500, 692, 429, 429);

        Dictionary<int, Vector3> GoldMineList = new Dictionary<int,Vector3>();

        private Texture2D goldMinePointer;
        GameCamera camera;
        BaseGame game;
        GameWorld world;

        const float Factor = 0.7f;
        const float RadiusFactor = 0.44f;

        Rectangle actualArea;
        const double GoldMinePointerFactor = 0.13;

        Rectangle goldMineSourceRectangle;

        public Rectangle GoldMineSourceRectangle
        {
            get { return goldMineSourceRectangle; }
            set { goldMineSourceRectangle = value; }
        }

        /// <summary>
        /// Gest the Actual Area
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

                    actualArea.Width  = (int)(Factor * DestinationRectangle.Width);
                    actualArea.Height = (int)(Factor * DestinationRectangle.Height);
                }
                return actualArea;
            }
        }

        /// <summary>
        /// Gets button destination rectangle
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
        /// Gets the center of the actualArea
        /// </summary>
        public Point Center
        {
            get 
            {
                return new Point(DestinationRectangle.Left + DestinationRectangle.Width / 2,
                                 DestinationRectangle.Top  + DestinationRectangle.Height / 2);
            }   
        }

        /// <summary>
        /// Gets or sets texture for gold mine pointer
        /// </summary>
        public Texture2D GoldMinePointerTexture
        {
            get { return goldMinePointer; }
            set { goldMinePointer = value; }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="game"></param>
        public MiniMap(BaseGame game,  GameWorld world)
        {
            this.game = game;
            this.camera = game.Camera as GameCamera;
            this.world = world;
        }

        static int GoldmineCounter = 0;

        /// <summary>
        /// Add a gold mine
        /// </summary>
        public int AddGoldmine(Vector3 postion)
        {
            GoldMineList.Add(GoldmineCounter, postion);
            return GoldmineCounter++;
        }

        /// <summary>
        /// Remove a gold mine
        /// </summary>
        /// <param name="key"></param>
        public void RemoveGoldmine(int key)
        {
            GoldMineList.Remove(key);
        }

        
        /// <summary>
        /// Gets the corresponding position in the real world.
        /// </summary>
        /// <param name="mapPoint">Position in the map</param>
        /// <returns></returns>
        public Vector3? MapToWorld(Point mapPoint)
        {
            
            if (ActualArea.Contains(mapPoint))
            {
                Vector3 rtv = new Vector3();
                rtv.X = world.Landscape.Size.X * (mapPoint.X - ActualArea.X) / ActualArea.Width;
                rtv.Y = world.Landscape.Size.Y * (ActualArea.Bottom - mapPoint.Y) / ActualArea.Height;
                rtv.Z = world.Landscape.GetHeight(rtv.X, rtv.Y);
                return rtv;
            }
            
            return null;
        }

        Vector3 MapPointToWorldPositionNegativeAllowed(Point mapPoint)
        {
            Vector3 rtv = new Vector3();
            rtv.X = world.Landscape.Size.X * (mapPoint.X - ActualArea.X) / ActualArea.Width;
            rtv.Y = world.Landscape.Size.Y * (ActualArea.Bottom - mapPoint.Y) / ActualArea.Height;
            rtv.Z = world.Landscape.GetHeight(rtv.X, rtv.Y);
            return rtv;
        }


        /// <summary>
        /// Gets the corresponding point in the map.
        /// </summary>
        /// <param name="position">Position in real world</param>
        /// <returns></returns>
        public Point? WorldToMap(Vector3 position)
        {
            if (position.X >= 0 && position.X <= world.Landscape.Size.X &&
                position.Y >= 0 && position.Y <= world.Landscape.Size.Y)
            {
                Point mp = new Point();
                mp.X = (int)(position.X / world.Landscape.Size.X * ActualArea.Width + ActualArea.X);
                mp.Y = (int)((1 - position.Y / world.Landscape.Size.Y) * ActualArea.Height + ActualArea.Y);
                return mp;
            }
            
            return null;
        }


        Point WorldPositionToMapPointNegativeAllowed(Vector3 position)
        {
            Point mp = new Point();
            mp.X = (int)(position.X / world.Landscape.Size.X * ActualArea.Width + ActualArea.X);
            mp.Y = (int)((1 - position.Y / world.Landscape.Size.Y) * ActualArea.Height + ActualArea.Y);
            return mp;
        }

        /// <summary>
        /// Draw
        /// </summary>
        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            sprite.Draw(Texture, DestinationRectangle, SourceRectangle, Color.White);
            sprite.Draw(Texture, DestinationRectangle, FogOfWarSourceRectangle, new Color(new Vector4(1, 1, 1, 0.6f)));

            // Draw the gold 
            Rectangle goldMineDest;
            goldMineDest.Width = (int)(GoldMinePointerFactor * DestinationRectangle.Width);
            goldMineDest.Height = goldMineDest.Width;
            Point? goldMinePointerPosition;
            foreach (Vector3 position in GoldMineList.Values)
            { 
                goldMinePointerPosition = WorldToMap(position);
                if(goldMinePointerPosition == null)
                    throw new InvalidOperationException("Gold Mine position out of range.");
                goldMineDest.X = goldMinePointerPosition.Value.X - goldMineDest.Width / 2;
                goldMineDest.Y = goldMinePointerPosition.Value.Y - goldMineDest.Height / 2;
                sprite.Draw(GoldMinePointerTexture, goldMineDest, goldMineSourceRectangle, Color.White); 
            }

            // Draw the entities
            DrawEntitySign(gameTime, sprite);

            // Draw the sight region rectangle
            DrawSightRegion(gameTime, sprite);
        }

        /// <summary>
        /// Draw the sight region
        /// </summary>
        void DrawSightRegion(GameTime gameTime, SpriteBatch sprite)
        {
            Point[] sightRegion = new Point[4];

            Plane xyPlane = new Plane(0, 0, 1, 0);
            Ray ray = game.Unproject(0, 0);
            float? dist = ray.Intersects(xyPlane);
            if (dist == null)
                return;
            Vector3 intersectPoint = ray.Position + Vector3.Normalize(ray.Direction) * dist.Value;
            sightRegion[0] = WorldPositionToMapPointNegativeAllowed(intersectPoint);

            ray = game.Unproject(game.ScreenWidth, 0);
            dist = ray.Intersects(xyPlane);
            if (dist == null)
                return;
            intersectPoint = ray.Position + Vector3.Normalize(ray.Direction) * dist.Value;
            sightRegion[1] = WorldPositionToMapPointNegativeAllowed(intersectPoint);

            ray = game.Unproject(game.ScreenWidth, game.ScreenHeight);
            dist = ray.Intersects(xyPlane);
            if (dist == null)
                return;
            intersectPoint = ray.Position + Vector3.Normalize(ray.Direction) * dist.Value;
            sightRegion[2] = WorldPositionToMapPointNegativeAllowed(intersectPoint);

            ray = game.Unproject(0, game.ScreenHeight);
            dist = ray.Intersects(xyPlane);
            if (dist == null)
                return;
            intersectPoint = ray.Position + Vector3.Normalize(ray.Direction) * dist.Value;
            sightRegion[3] = WorldPositionToMapPointNegativeAllowed(intersectPoint);

            Graphics2D.DrawLine(sightRegion[0], sightRegion[1], Color.White);
            Graphics2D.DrawLine(sightRegion[1], sightRegion[2], Color.White);
            Graphics2D.DrawLine(sightRegion[2], sightRegion[3], Color.White);
            Graphics2D.DrawLine(sightRegion[3], sightRegion[0], Color.White);
        }


        /// <summary>
        /// Draw a game object on the minimap
        /// </summary>
        public void DrawGameObject(Vector3 position, float size, Color color)
        {
            EntitySign sign;
            
            Point mapPoint = WorldPositionToMapPointNegativeAllowed(position);

            sign.Tint = color;
            sign.Destination = new Rectangle(mapPoint.X - (int)(size / 2),
                                             mapPoint.Y - (int)(size / 2), (int)size, (int)size);

            entitySigns.Add(sign);
        }

        struct EntitySign
        {
            public Rectangle Destination;
            public Color Tint;
        }

        List<EntitySign> entitySigns = new List<EntitySign>();

        /// <summary>
        /// Draw the entites
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="sprite"></param>
        void DrawEntitySign(GameTime gameTime, SpriteBatch sprite)
        {
            foreach (EntitySign sign in entitySigns)
                Graphics2D.DrawRectangle(sign.Destination, sign.Tint);

            entitySigns.Clear();
        }



        public override void Update(GameTime gameTime)
        {
            if (camera == null)
                camera = game.Camera as GameCamera;

            if (draging)
            {
                Vector2 dist = new Vector2(game.Input.MousePosition.X - Center.X,
                                           game.Input.MousePosition.Y - Center.Y);
                if (dist.X * dist.X + dist.Y * dist.Y <= 
                    RadiusFactor * DestinationRectangle.Width * RadiusFactor * DestinationRectangle.Width)
                {
                    Vector3 position = MapPointToWorldPositionNegativeAllowed(game.Input.MousePosition);
                    if (position != null)
                        camera.FlyTo(position, true);
                }
                else
                {
                    Vector2 offset = new Vector2(dist.X, dist.Y);
                    offset =  RadiusFactor * DestinationRectangle.Width * Vector2.Normalize(offset);
                    Mouse.SetPosition((int)offset.X + Center.X, (int)offset.Y + Center.Y);
                }
            }
        }

        bool draging = false;

        /// <summary>
        /// Event handler
        /// </summary>
        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (Enabled && Visible)
            {
                Input input = sender as Input;
                Keys? key = tag as Keys?;
                Vector2 dist = new Vector2(input.MousePosition.X - Center.X,
                                            input.MousePosition.Y - Center.Y);
                if (type == EventType.LeftButtonDown &&
                    dist.X * dist.X + dist.Y * dist.Y <=
                    RadiusFactor * DestinationRectangle.Width * RadiusFactor * DestinationRectangle.Width)
                {
                    Point pointTo = new Point();
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
                    if (point .HasValue)
                        Player.LocalPlayer.PerformAction(point.Value);

                    return EventResult.Handled;
                }
            }

            return EventResult.Unhandled;
        }
    }
}
