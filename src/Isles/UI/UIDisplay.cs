// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.UI
{
    public sealed class UIDisplay : IUIElement
    {
        /// <summary>
        /// Standard screen width and height, all UI elements
        /// are scaled relative to this resolution.
        /// </summary>
        public const int ReferenceScreenWidth = 800;
        public const int ReferenceScreenHeight = 600;

        private readonly BaseGame game;

        private Rectangle area;

        private readonly BroadcastList<IUIElement, List<IUIElement>> elements = new();

        private readonly Rectangle referenceArea = new(0, 0, ReferenceScreenWidth, ReferenceScreenHeight);

        public SpriteBatch Sprite { get; set; }

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

        public IUIElement Parent
        {
            get => null;

            // Can't set the parent of a display
            set { }
        }

        public Anchor Anchor
        {
            get => Anchor.TopLeft;
            set { }
        }

        public ScaleMode ScaleMode
        {
            get => ScaleMode.Fixed;
            set { }
        }

        public bool Visible { get; set; } = true;

        public bool Enabled { get; set; } = true;

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
            Sprite = new SpriteBatch(game.GraphicsDevice);

            GraphicsDevice_DeviceReset(game.GraphicsDevice, null);
            game.GraphicsDevice.DeviceReset += GraphicsDevice_DeviceReset;
        }

        public void Add(IUIElement element)
        {
            element.Parent = this;
            elements.Add(element);
        }

        public void Remove(IUIElement element)
        {
            element.Parent = null;
            elements.Remove(element);
        }

        public void Clear()
        {
            foreach (IUIElement element in elements)
            {
                element.Parent = null;
            }

            elements.Clear();
        }

        public IEnumerable<IUIElement> Elements => elements;

        public void Update(GameTime gameTime)
        {
            if (!Enabled)
            {
                return;
            }

            foreach (IUIElement element in elements)
            {
                element.Update(gameTime);
            }
        }

        public void Draw(GameTime gameTime)
        {
            Draw(gameTime, Sprite);
        }

        public void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (!Visible)
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
    }
}
