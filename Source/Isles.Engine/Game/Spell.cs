//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
{
    /// <summary>
    /// A rectangle on a texture.
    /// Currently all icons are placed in the same texture for simplicity.
    /// </summary>
    public struct Icon
    {
        /// <summary>
        /// Gets or sets the rectangle region on the texture
        /// </summary>
        public Rectangle Region;

        /// <summary>
        /// For easy creation of icons
        /// </summary>
        public Icon(Rectangle region)
        {
            Region = region;
        }

        /// <summary>
        /// Build an icon
        /// </summary>
        public static Icon FromTileTexture(
            int n, int xCount, int yCount, int width, int height)
        {
            int x = n % xCount;
            int y = n / yCount;
            int w = width / xCount;
            int h = height / yCount;

            return new Icon(new Rectangle(x * w, y * h, w, h));
        }
    }
    
    /// <summary>
    /// Controls how a spell is been casted
    /// </summary>
    public abstract class Spell
    {
        /// <summary>
        /// Delegation used to create a spell
        /// </summary>
        public delegate Spell Creator(GameWorld world);

        /// <summary>
        /// Spell creators
        /// </summary>
        static Dictionary<string, Creator> creators = new Dictionary<string,Creator>();

        /// <summary>
        /// Register a new spell
        /// </summary>
        /// <param name="spellType"></param>
        /// <param name="creator"></param>
        public static void Register(Type spellType, Creator creator)
        {
            creators.Add(spellType.Name, creator);
        }

        public static Spell Create(Type spellType, GameWorld world)
        {
            return Create(spellType.Name, world);
        }

        /// <summary>
        /// Create a new game spell
        /// </summary>
        /// <param name="spellTypeName"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public static Spell Create(string spellTypeName, GameWorld world)
        {
            Creator creator;

            if (!creators.TryGetValue(spellTypeName, out creator))
                throw new Exception("Failed create spell, unknown spell type: " + spellTypeName);

            return creator(world);
        }

        /// <summary>
        /// Game world
        /// </summary>
        protected GameWorld world;

        public Spell()
        {

        }
        
        public Spell(GameWorld world)
        {
            this.world = world;
        }

        /// <summary>
        /// Gets the name of the spell
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description for the spell
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the spell icon.
        /// </summary>
        public virtual Icon? Icon
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the freeze time after the spell has been casted
        /// </summary>
        public virtual float FreezeTime
        {
            get { return 0; }
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime)
        {
        }

        /// <summary>
        /// Draw the spell
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Draw(GameTime gameTime)
        {
        }

        /// <summary>
        /// Called when the user clicked the spell icon
        /// (or pressed the spell hot key).
        /// </summary>
        /// <returns>
        /// Whether the spell wants to receive BeginCast event
        /// </returns>
        public virtual bool Trigger(Hand hand)
        {
            return true;
        }

        /// <summary>
        /// Called when every frame after TriggerCast returns true.
        /// </summary>
        /// <param name="hand"></param>
        public virtual void Locating(Hand hand)
        {
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
        public virtual bool BeginCast(Hand hand, bool leftButton)
        {
            return true;
        }

        /// <summary>
        /// Called after BeginCast and EndCast every frame
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="leftButton"></param>
        public virtual void Casting(Hand hand, bool leftButton)
        {
        }

        /// <summary>
        /// Called after the user has just release the button
        /// when this spell is being casting.
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="leftButton"></param>
        /// <returns>
        /// Whether the user will keep on casting this spell.
        /// </returns>
        public virtual bool EndCast(Hand hand, bool leftButton)
        {
            return false;
        }

        /// <summary>
        /// Perform the actual cast
        /// </summary>
        /// <returns>Success of not</returns>
        public virtual bool Cast()
        {
            return false;
        }

        /// <summary>
        /// Cast the spell to a specified position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual bool Cast(Vector3 position)
        {
            return false;
        }

        /// <summary>
        /// Cast the spell to the target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual bool Cast(Entity target)
        {
            return false;
        }
    }
}
