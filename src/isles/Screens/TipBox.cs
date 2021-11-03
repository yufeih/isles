// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Isles.Engine;
using Isles.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Isles
{
    public class TipBox : Panel
    {
        private static readonly Texture2D white = BaseGame.Singleton.TextureLoader.LoadTexture("data/ui/Panels.png");
        private static readonly Rectangle DialogHigherHorizontalLine = new(50, 0, 50, 50);
        private static readonly Rectangle DialogLowerHorizontalLine = new(50, 380, 50, 50);
        private static readonly Rectangle DialogLeftVerticalLine = new(0, 50, 50, 50);
        private static readonly Rectangle DialogRightVerticalLine = new(408, 50, 50, 50);
        private static readonly Rectangle DialogLeftTopCorner = new(0, 0, 50, 50);
        private static readonly Rectangle DialogRightTopCorner = new(408, 0, 50, 50);
        private static readonly Rectangle DialogLeftBottomCorner = new(0, 380, 50, 50);
        private static readonly Rectangle DialogRightBottomCorner = new(408, 380, 50, 50);
        private static readonly Rectangle DialogContent = new(50, 50, 50, 50);
        private static readonly Rectangle whiteTextureSource = new(1000, 800, 1, 1);
        private static readonly Texture2D DialogTexture = BaseGame.Singleton.TextureLoader.LoadTexture("data/ui/TipBox.png");

        public int DialogCornerWidth = 6;

        public bool Mask;

        public bool TrackCursor { get; }

        /// <summary>
        /// Construct a fixed-location tipbox.
        /// </summary>
        public TipBox(Rectangle area)
            : base(area)
        {
            TrackCursor = false;
            ScaleMode = ScaleMode.Fixed;
        }

        /// <summary>
        /// Construct a tipbox that follows the cursor.
        /// </summary>
        public TipBox(int width, int height)
            : base(Rectangle.Empty)
        {
            TrackCursor = true;
            ScaleMode = ScaleMode.Fixed;
            Area = new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, width, height);
            setPositionToCursor();
        }

        public void DrawDialog(Rectangle rectangle, SpriteBatch sprite)
        {
            // Actual Width and Height for corner
            var width = DialogCornerWidth;
            var height = DialogCornerWidth;
            if (rectangle.Width < 2 * width)
            {
                width = rectangle.Width / 2;
            }

            if (rectangle.Height < 2 * height)
            {
                height = rectangle.Height / 2;
            }

            Rectangle destRect = rectangle;
            destRect.Width = width;
            destRect.Height = height;

            sprite.Draw(DialogTexture, destRect, DialogLeftTopCorner, Color.White);

            destRect.X = rectangle.Right - width;
            sprite.Draw(DialogTexture, destRect, DialogRightTopCorner, Color.White);

            destRect.Y = rectangle.Bottom - height;
            sprite.Draw(DialogTexture, destRect, DialogRightBottomCorner, Color.White);

            destRect.X = rectangle.X;
            sprite.Draw(DialogTexture, destRect, DialogLeftBottomCorner, Color.White);

            destRect.Y = rectangle.Y + height;
            destRect.Height = rectangle.Height - 2 * height;
            sprite.Draw(DialogTexture, destRect, DialogLeftVerticalLine, Color.White);

            destRect.X = rectangle.Right - width;
            sprite.Draw(DialogTexture, destRect, DialogRightVerticalLine, Color.White);

            destRect.X = rectangle.X + width;
            destRect.Y = rectangle.Y;
            destRect.Width = rectangle.Width - 2 * width;
            destRect.Height = height;
            sprite.Draw(DialogTexture, destRect, DialogHigherHorizontalLine, Color.White);

            destRect.Y = rectangle.Bottom - height;
            sprite.Draw(DialogTexture, destRect, DialogLowerHorizontalLine, Color.White);

            destRect.X = rectangle.X + width;
            destRect.Y = rectangle.Y + height;
            destRect.Width = rectangle.Width - 2 * width;
            destRect.Height = rectangle.Height - 2 * height;
            sprite.Draw(DialogTexture, destRect, DialogContent, Color.White);
        }

        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (Mask)
            {
                sprite.Draw(white, new Rectangle(0, 0, BaseGame.Singleton.ScreenWidth, BaseGame.Singleton.ScreenHeight),
                             whiteTextureSource, new Color(0, 0, 0, 180));
            }

            DrawDialog(DestinationRectangle, sprite);
            base.Draw(gameTime, sprite);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (TrackCursor)
            {
                setPositionToCursor();
                foreach (var element in elements)
                {
                    if (element is UIElement)
                    {
                        (element as UIElement).ResetDestinationRectangle();
                    }
                }
            }
        }

        private void setPositionToCursor()
        {
            X = Mouse.GetState().X + 15;
            Y = Mouse.GetState().Y - DestinationRectangle.Height;
            if (Y < 0)
            {
                Y = Mouse.GetState().Y;
            }

            if (Mouse.GetState().X + 15 + DestinationRectangle.Width > BaseGame.Singleton.ScreenWidth)
            {
                X = Mouse.GetState().X - DestinationRectangle.Width;
            }
        }
    }
}
