//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.UI
{
    /// <summary>
    /// Manages all UI elements
    /// </summary>
    public class UIDisplay : IUIElement
    {
        /// <summary>
        /// Standard screen width and height, all UI elements
        /// are scaled relative to this resolution
        /// </summary>
        public const int ReferenceScreenWidth = 800;
        public const int ReferenceScreenHeight = 600;

        #region Variables
        /// <summary>
        /// UI content manager
        /// </summary>
        ContentManager content;

        /// <summary>
        /// Sprite batch used to draw all UI elements
        /// </summary>
        SpriteBatch sprite;

        /// <summary>
        /// Default UI font
        /// </summary>
        SpriteFont font;

        /// <summary>
        /// UI effect
        /// </summary>
        Effect effect;

        /// <summary>
        /// Area of the UI dialog
        /// </summary>
        Rectangle area;

        /// <summary>
        /// UI Elements
        /// </summary>
        List<IUIElement> elements = new List<IUIElement>();

        /// <summary>
        /// Visible
        /// </summary>
        bool visible = true;

        /// <summary>
        /// Enable
        /// </summary>
        bool enabled = true;

        /// <summary>
        /// Reference area
        /// </summary>
        readonly Rectangle referenceArea =
            new Rectangle(0, 0, ReferenceScreenWidth, ReferenceScreenHeight);
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the sprite batch used to draw all UI elements
        /// </summary>
        public SpriteBatch Sprite
        {
            get { return sprite; }
            set { sprite = value; }
        }

        /// <summary>
        /// Gets or sets the default UI font
        /// </summary>
        public SpriteFont Font
        {
            get { return font; }
            set { font = value; }
        }

        /// <summary>
        /// Gets or sets the default effect to render UI elements
        /// </summary>
        public Effect Effect
        {
            get { return effect; }
            set { effect = value; }
        }

        /// <summary>
        /// Gets or sets the area of the dialog
        /// </summary>
        public Rectangle Area
        {
            get { return referenceArea; }
            set { }
        }

        public Rectangle DestinationRectangle
        {
            get { return area; }
            set { }
        }

        /// <summary>
        /// Gets or sets the parent of an UI element
        /// </summary>
        public IUIElement Parent
        {
            get { return null; }

            // Can't set the parent of a display
            set { }
        }

        /// <summary>
        /// Gets or sets UI element visibility
        /// </summary>
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        /// <summary>
        /// Enable/Disable an UI element
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }
        #endregion

        #region Methods
        public UIDisplay(Game game)
            : this(game, "Fonts/Default")
        {
        }

        /// <summary>
        /// Create an UI display
        /// </summary>
        /// <param name="game"></param>
        public UIDisplay(Game game, string defaultFont)
        {
            content = game.Content;
            sprite = new SpriteBatch(game.GraphicsDevice);
            effect = new BasicEffect(game.GraphicsDevice, null);
            font = content.Load<SpriteFont>(defaultFont);

            GraphicsDevice_DeviceReset(game.GraphicsDevice, null);
            game.GraphicsDevice.DeviceReset += new EventHandler(GraphicsDevice_DeviceReset);
        }

        void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            GraphicsDevice device = sender as GraphicsDevice;

            area.X = area.Y = 0;
            area.Width = device.Viewport.Width;
            area.Height = device.Viewport.Height;
        }

        /// <summary>
        /// Adds an UI element to the dialog
        /// </summary>
        /// <param name="element"></param>
        public void Add(IUIElement element)
        {
            element.Parent = this;
            elements.Add(element);
        }

        /// <summary>
        /// Removes an UI elment from the dialog
        /// </summary>
        /// <param name="element"></param>
        public void Remove(IUIElement element)
        {
            element.Parent = null;
            elements.Remove(element);
        }

        /// <summary>
        /// Clear all UI elements
        /// </summary>
        public void Clear()
        {
            foreach (IUIElement element in elements)
                element.Parent = null;

            elements.Clear();
        }

        /// <summary>
        /// Update all UI elements
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            if (!enabled)
                return;

            foreach (IUIElement element in elements)
                element.Update(gameTime);
        }

        /// <summary>
        /// Draw all UI elements
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            Draw(gameTime, sprite);
        }

        /// <summary>
        /// Draw all UI elements
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (!visible)
                return;

            sprite.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);

            foreach (IUIElement element in elements)
                if (element.Visible)
                    element.Draw(gameTime, sprite);

            sprite.End();
        }
        #endregion

        #region GetRelativeRectangle
        /// <summary>
        /// Gets the relative rectangle based on current anchor,
        /// scale mode and parent reference rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle in standard resolution</param>
        /// <return>Rectangle in current resolution</return>
        public static Rectangle GetRelativeRectangle(
            Rectangle rectangle, IUIElement parent, ScaleMode scaleMode, Anchor anchor)
        {
            if (parent == null)
                return rectangle;

            Rectangle relativeRectangle;

            // scale
            if (scaleMode == ScaleMode.Stretch)
            {
                relativeRectangle.Width = rectangle.Width *
                    parent.DestinationRectangle.Width / parent.Area.Width;
                relativeRectangle.Height = rectangle.Height *
                    parent.DestinationRectangle.Height / parent.Area.Height;
            }
            else if (scaleMode == ScaleMode.ScaleY)
            {
                relativeRectangle.Width = rectangle.Width *
                    parent.DestinationRectangle.Height / parent.Area.Height;
                relativeRectangle.Height = rectangle.Height *
                    parent.DestinationRectangle.Height / parent.Area.Height;
            }
            else if (scaleMode == ScaleMode.ScaleX)
            {
                relativeRectangle.Width = rectangle.Width *
                    parent.DestinationRectangle.Width / parent.Area.Width;
                relativeRectangle.Height = rectangle.Height *
                    parent.DestinationRectangle.Width / parent.Area.Width;
            }
            else
            {
                relativeRectangle.Width = rectangle.Width;
                relativeRectangle.Height = rectangle.Height;
            }

            // anchor
            if (anchor == Anchor.TopLeft || anchor == Anchor.BottomLeft)
            {
                relativeRectangle.X = parent.DestinationRectangle.Left +
                    rectangle.Left * relativeRectangle.Width / rectangle.Width;
            }
            else
            {
                relativeRectangle.X = parent.DestinationRectangle.Right +
                    (rectangle.Right - parent.Area.Width) *
                        relativeRectangle.Width / rectangle.Width -
                            relativeRectangle.Width;
            }

            if (anchor == Anchor.TopLeft || anchor == Anchor.TopRight)
            {
                relativeRectangle.Y = parent.Area.Top +
                    rectangle.Top * relativeRectangle.Height / rectangle.Height;
            }
            else
            {
                relativeRectangle.Y = parent.DestinationRectangle.Bottom +
                    (rectangle.Bottom - parent.Area.Height) *
                        relativeRectangle.Height / rectangle.Height -
                            relativeRectangle.Height;
            }

            return relativeRectangle;
        }
        #endregion

        #region Dispose

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (sprite != null)
                    sprite.Dispose();

                if (effect != null)
                    effect.Dispose();

                foreach (IUIElement element in elements)
                    element.Dispose();
            }
        }

        #endregion
    }
}
