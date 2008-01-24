//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;

namespace Isles
{
    #region Fireball
    /// <summary>
    /// Fireball entity
    /// </summary>
    public class Fireball : BaseEntity
    {
        /// <summary>
        /// Whether the fireball is exploding
        /// </summary>
        bool explode = false;

        /// <summary>
        /// Fire ball animation texture
        /// </summary>
        Texture2D[] texture;

        /// <summary>
        /// Number of texture frames
        /// </summary>
        const int FireBallTextureFrames = 23;

        /// <summary>
        /// Animation speed
        /// </summary>
        const double FrameRate = 30;

        /// <summary>
        /// Current frame
        /// </summary>
        double frame;

        /// <summary>
        /// Create a fireball entity
        /// </summary>
        /// <param name="screen"></param>
        public Fireball(GameWorld world)
            : base(world)
        {
            texture = new Texture2D[FireBallTextureFrames];

            for (int i = 0; i < FireBallTextureFrames; i++)
            {
                texture[i] = world.Content.Load<Texture2D>(
                    "Spells/Fireball/areaeffect_" + (i + 1));
            }

            BaseGame.Singleton.Sound.Play("cast", this);
        }

        public override void Update(GameTime gameTime)
        {
            if (explode)
            {
                // Explode
                frame += gameTime.ElapsedGameTime.TotalSeconds * FrameRate;
            }
            else
            {
                // Add a little gravity, since this is fire ball, we
                // reduce the effect of gravity.
                velocity += world.GameLogic.Gravity * 0.08f *
                    (float)gameTime.ElapsedGameTime.TotalSeconds;

                position += velocity;

                // Destroy anyway if we're too far away
                if (Position.LengthSquared() > 1e7)
                    world.Destroy(this);
            }

            // Hit test
            float height = world.Landscape.GetHeight(Position.X, Position.Y);
            if (height > Position.Z)
            {
                //position.Z = height;
                explode = true;
            }

            // Destroy
            if (explode && (int)frame >= FireBallTextureFrames)
            {
                frame = 0;
                world.Destroy(this);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // When exploding, perform an ray test with the terrain,
            // if we can't see the fireball, then nothing will be drawed since
            // we turned off depth buffer
            //if (explode)
            //{
            //    Ray ray;

            //    ray.Position = screen.Game.Eye;
            //    Vector3 v = position - ray.Position;
            //    ray.Direction = Vector3.Normalize(v);

            //    Vector3? result = screen.Landscape.Intersects(ray);

            //    // The billboard won't be drawed if we can't see it
            //    if (result.HasValue &&
            //        result.Value.LengthSquared() < v.LengthSquared())
            //    {
            //        return;
            //    }
            //}

            //screen.Game.PointSprite.Draw(texture[(int)frame], Position, 128);

            // It's not accurate to use point sprite to draw the fireball,
            // so use center oriented billboard instead.
            Billboard billboard;

            billboard.Texture = texture[(int)frame];
            billboard.Normal = Vector3.Zero;
            billboard.Position = position;
            billboard.Size.X = billboard.Size.Y = 128;
            billboard.SourceRectangle = Billboard.DefaultSourceRectangle;

            // Turn off depth buffer when exploding
            if (explode)
                billboard.Type = BillboardType.CenterOriented;
            else
                billboard.Type = BillboardType.CenterOriented | BillboardType.DepthBufferEnable;

            BaseGame.Singleton.Billboard.Draw(billboard);
        }
    }
    #endregion

    #region Fireball Spell
    /// <summary>
    /// Fireball spell
    /// </summary>
    public class FireballSpell : Spell
    {
        /// <summary>
        /// Aim texture
        /// </summary>
        Texture2D aim;

        /// <summary>
        /// Spell hotkey
        /// </summary>
        Keys hotkey;

        /// <summary>
        /// Spell name
        /// </summary>
        string name = "";

        /// <summary>
        /// Spell description
        /// </summary>
        string description = "";

        /// <summary>
        /// Create a new spell. TODO: pass in a hand
        /// </summary>
        public FireballSpell(GameWorld world)
            : base(world)
        {
            XmlElement xml;

            if (GameDefault.Singleton.SpellDefaults.TryGetValue("Fireball", out xml))
            {
                name = xml.GetAttribute("Name");
                description = xml.GetAttribute("Description");

                hotkey = (Keys)Enum.Parse(typeof(Keys), xml.GetAttribute("Hotkey"));
            }

            aim = world.Content.Load<Texture2D>("Textures/SpellAreaOfEffect");
        }

        /// <summary>
        /// Gets the name of the spell
        /// </summary>
        public override string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets the description of the spell
        /// </summary>
        public override string Description
        {
            get { return description; }
        }

        public override Keys Hotkey
        {
            get { return hotkey; }
        }

        /// <summary>
        /// Draw the spell
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            // Draw the aim texture

            Vector2 point, size;

            point.X = hand.CursorPosition.X;
            point.Y = hand.CursorPosition.Y;

            size.X = size.Y = 128;

            world.Landscape.DrawSurface(aim, point, size);
        }

        /// <summary>
        /// Called when the user just pressed the button
        /// after this spell is triggered.
        /// </summary>
        /// <param name="hand"></param>
        /// <returns>
        /// Return true if the spell wants to receive
        /// Casting/EndCast events.
        /// </returns>
        public override bool BeginCast(Hand hand, bool leftButton)
        {
            // Right click to exit
            if (!leftButton)
            {
                hand.StopCast();
                return false;
            }

            Fireball fireball = new Fireball(world);

            fireball.Position = hand.Position;

            Vector3 speed = Vector3.Normalize(
                hand.CursorPosition - fireball.Position) * 20;

            // Add a little random factor
            Random random = new Random();

            speed.X += ((float)random.NextDouble() - 0.5f) / 2;
            speed.Y += ((float)random.NextDouble() - 0.5f) / 2;
            speed.Z += ((float)random.NextDouble() - 0.5f) / 2;

            fireball.Velocity = speed;

            world.Add(fireball);

            return false;
        }
    }
    #endregion
}
