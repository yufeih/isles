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
    #region Spell

    /// <summary>
    /// Respresents a spell in the game
    /// </summary>
    public abstract class Spell
    {
        #region Fields
        /// <summary>
        /// Settings of this spell
        /// </summary>
        protected SpellSettings settings;

        /// <summary>
        /// Game screen
        /// </summary>
        protected GameScreen screen;
        #endregion
        
        #region Methods
        /// <summary>
        /// Create a new spell
        /// </summary>
        public Spell(GameScreen gameScreen, SpellSettings settings)
        {
            this.screen = gameScreen;
            this.settings = settings;
        }

        /// <summary>
        /// Factory method to create a spell based on spell settings
        /// </summary>
        /// <param name="spellClass"></param>
        /// <returns></returns>
        public static Spell Create(GameScreen screen, SpellSettings settings)
        {
            switch (settings.Class)
            {
                case "Fireball":
                    return new FireballSpell(screen, settings);
                default:
                    return null;
            }
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

        #region Casting
        /// <summary>
        /// Called when the user clicked the spell icon
        /// (or pressed the spell hot key).
        /// </summary>
        /// <returns>
        /// Whether the spell wants to receive BeginCast event
        /// </returns>
        public virtual bool TriggerCast(Hand hand)
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
        #endregion

        #endregion
    }

    #endregion

    #region SpellSettings
    /// <summary>
    /// Settings for a single tree
    /// </summary>
    [Serializable()]
    public class SpellSettings
    {
        #region Variables
        /// <summary>
        /// Name of the tree
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Description of the tree
        /// </summary>
        public string Description = "";

        /// <summary>
        /// Class of the spell, used to create a spell from spell factory
        /// </summary>
        public string Class = "";

        /// <summary>
        /// Spell icon
        /// </summary>
        public int Icon;

        /// <summary>
        /// Hot key of this spell
        /// </summary>
        public Keys Hotkey;
        #endregion
    }

    /// <summary>
    /// Settings for all trees
    /// </summary>
    [Serializable()]
    public class SpellSettingsCollection : ICollection
    {
        List<SpellSettings> settings = new List<SpellSettings>();

        public SpellSettings this[int index]
        {
            get { return settings[index]; }
        }

        public void CopyTo(Array a, int index)
        {
            settings.CopyTo((SpellSettings[])a, index);
        }

        public int Count
        {
            get { return settings.Count; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public IEnumerator GetEnumerator()
        {
            return settings.GetEnumerator();
        }

        public void Add(SpellSettings newTree)
        {
            settings.Add(newTree);
        }
    }
    #endregion
}
