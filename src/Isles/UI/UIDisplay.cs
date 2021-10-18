// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.UI
{
    /// <summary>
    /// Manages all UI elements.
    /// </summary>
    public sealed class UIDisplay : IUIElement
    {
        /// <summary>
        /// Standard screen width and height, all UI elements
        /// are scaled relative to this resolution.
        /// </summary>
        public const int ReferenceScreenWidth = 800;
        public const int ReferenceScreenHeight = 600;

        /// <summary>
        /// UI content manager.
        /// </summary>
        private readonly BaseGame game;

        /// <summary>
        /// Sprite batch used to draw all UI elements.
        /// </summary>
        private SpriteBatch sprite;

        /// <summary>
        /// Default UI font.
        /// </summary>
        private SpriteFont font;

        /// <summary>
        /// UI effect.
        /// </summary>
        private Effect effect;

        /// <summary>
        /// Area of the UI dialog.
        /// </summary>
        private Rectangle area;

        /// <summary>
        /// UI Elements.
        /// </summary>
        private readonly BroadcastList<IUIElement, List<IUIElement>> elements = new
();

        /// <summary>
        /// Visible.
        /// </summary>
        private bool visible = true;

        /// <summary>
        /// Enable.
        /// </summary>
        private bool enabled = true;

        /// <summary>
        /// Reference area.
        /// </summary>
        private readonly Rectangle referenceArea =
            new(0, 0, ReferenceScreenWidth, ReferenceScreenHeight);

        /// <summary>
        /// Gets or sets the sprite batch used to draw all UI elements.
        /// </summary>
        public SpriteBatch Sprite
        {
            get => sprite;
            set => sprite = value;
        }

        /// <summary>
        /// Gets or sets the default UI font.
        /// </summary>
        public SpriteFont Font
        {
            get => font;
            set => font = value;
        }

        /// <summary>
        /// Gets or sets the default effect to render UI elements.
        /// </summary>
        public Effect Effect
        {
            get => effect;
            set => effect = value;
        }

        /// <summary>
        /// Gets or sets the area of the dialog.
        /// </summary>
        public Rectangle Area
        {
            get => referenceArea;
            set { }
        }

        public Rectangle DestinationRectangle
        {
            get => area;
            set { }
        }

        /// <summary>
        /// Gets or sets the parent of an UI element.
        /// </summary>
        public IUIElement Parent
        {
            get => null;

            // Can't set the parent of a display
            set { }
        }

        /// <summary>
        /// Gets or sets UI element anchor.
        /// </summary>
        public Anchor Anchor
        {
            get => Anchor.TopLeft;
            set { }
        }

        /// <summary>
        /// Gets or sets UI element scale mode.
        /// </summary>
        public ScaleMode ScaleMode
        {
            get => ScaleMode.Fixed;
            set { }
        }

        /// <summary>
        /// Gets or sets UI element visibility.
        /// </summary>
        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        /// <summary>
        /// Enable/Disable an UI element.
        /// </summary>
        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        public UIDisplay(BaseGame game)
        {
            this.game = game;

            LoadContent();
        }

        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            var device = sender as GraphicsDevice;

            area.X = area.Y = 0;
            area.Width = device.Viewport.Width;
            area.Height = device.Viewport.Height;
        }

        public void LoadContent()
        {
            sprite = new SpriteBatch(game.GraphicsDevice);
            effect = new BasicEffect(game.GraphicsDevice, null);
            font = game.ZipContent.Load<SpriteFont>("Fonts/Default");

            GraphicsDevice_DeviceReset(game.GraphicsDevice, null);
            game.GraphicsDevice.DeviceReset += new EventHandler(GraphicsDevice_DeviceReset);
        }

        /// <summary>
        /// Adds an UI element to the dialog.
        /// </summary>
        /// <param name="element"></param>
        public void Add(IUIElement element)
        {
            element.Parent = this;
            elements.Add(element);
        }

        /// <summary>
        /// Removes an UI elment from the dialog.
        /// </summary>
        /// <param name="element"></param>
        public void Remove(IUIElement element)
        {
            element.Parent = null;
            elements.Remove(element);
        }

        /// <summary>
        /// Clear all UI elements.
        /// </summary>
        public void Clear()
        {
            foreach (IUIElement element in elements)
            {
                element.Parent = null;
            }

            elements.Clear();
        }

        public IEnumerable<IUIElement> Elements => elements;

        /// <summary>
        /// Update all UI elements.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            if (!enabled)
            {
                return;
            }

            foreach (IUIElement element in elements)
            {
                element.Update(gameTime);
            }
        }

        /// <summary>
        /// Draw all UI elements.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            Draw(gameTime, sprite);
        }

        /// <summary>
        /// Draw all UI elements.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (!visible)
            {
                return;
            }

            sprite.Begin();

            foreach (IUIElement element in elements)
            {
                if (element.Visible)
                {
                    element.Draw(gameTime, sprite);
                }
            }

            sprite.End();
        }

        /// <summary>
        /// Handle UI input.
        /// </summary>
        public EventResult HandleEvent(EventType type, object sender, object tag)
        {
            for (var i = elements.Count - 1; i >= 0; i--)
            {
                if (elements.Elements[i].Enabled &&
                    elements.Elements[i].HandleEvent(type, sender, tag) == EventResult.Handled)
                {
                    return EventResult.Handled;
                }
            }

            return EventResult.Unhandled;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (sprite != null)
            {
                sprite.Dispose();
            }

            if (effect != null)
            {
                effect.Dispose();
            }

            foreach (IUIElement element in elements)
            {
                element.Dispose();
            }

            Clear();

            GC.SuppressFinalize(this);
        }
    }
}
