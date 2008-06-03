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
    #region TipBox
    public class TipBox : Panel
    {
        static Texture2D white = BaseGame.Singleton.ZipContent.Load<Texture2D>("UI/Panels");

        static readonly Rectangle DialogHigherHorizontalLine = new Rectangle(50, 0, 50, 50);
        static readonly Rectangle DialogLowerHorizontalLine = new Rectangle(50, 380, 50, 50);
        static readonly Rectangle DialogLeftVerticalLine = new Rectangle(0, 50, 50, 50);
        static readonly Rectangle DialogRightVerticalLine = new Rectangle(408, 50, 50, 50);
        static readonly Rectangle DialogLeftTopCorner = new Rectangle(0, 0, 50, 50);
        static readonly Rectangle DialogRightTopCorner = new Rectangle(408, 0, 50, 50);
        static readonly Rectangle DialogLeftBottomCorner = new Rectangle(0, 380, 50, 50);
        static readonly Rectangle DialogRightBottomCorner = new Rectangle(408, 380, 50, 50);
        static readonly Rectangle DialogContent = new Rectangle(50, 50, 50, 50);
        static readonly Rectangle whiteTextureSource = new Rectangle(1000, 800, 1, 1);

        static Texture2D DialogTexture = BaseGame.Singleton.ZipContent.Load<Texture2D>("UI/TipBox");


        public int DialogCornerWidth = 6;


        public bool Mask = false;

        /// <summary>
        /// Whather to track the cursor
        /// </summary>
        bool trackCursor = false;
        public bool TrackCursor
        {
            get { return trackCursor; }
        }

        /// <summary>
        /// Construct a fixed-location tipbox
        /// </summary>
        public TipBox(Rectangle area)
            : base(area)
        {
            trackCursor = false;
            this.ScaleMode = ScaleMode.Fixed;

        }

        /// <summary>
        /// Construct a tipbox that follows the cursor
        /// </summary>
        /// <param name="trackCursor"></param>
        /// <param name="text"></param>
        /// <param name="fontSize"></param>
        public TipBox(int width, int height)
            : base(Rectangle.Empty)
        {
            trackCursor = true;
            this.ScaleMode = ScaleMode.Fixed;
            Area = new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, width, height);
            setPositionToCursor();
        }

        /// <summary>
        /// Draw a dialog
        /// </summary>
        public void DrawDialog(Rectangle rectangle, SpriteBatch sprite)
        {
            // Actual Width and Height for corner
            int width = DialogCornerWidth;
            int height = DialogCornerWidth;
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
            DrawDialog(this.DestinationRectangle, sprite);
            base.Draw(gameTime, sprite);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (trackCursor)
            {
                setPositionToCursor();
                foreach (IUIElement element in elements)
                {
                    if (element is UIElement)
                        (element as UIElement).ResetDestinationRectangle();
                }
            }
        }

        void setPositionToCursor()
        {
            this.X = Mouse.GetState().X + 15;
            this.Y = Mouse.GetState().Y - this.DestinationRectangle.Height;
            if (this.Y < 0)
            {
                this.Y = Mouse.GetState().Y;
            }
            if (Mouse.GetState().X + 15 + this.DestinationRectangle.Width > BaseGame.Singleton.ScreenWidth)
            {
                this.X = Mouse.GetState().X - this.DestinationRectangle.Width;
            }
        }

    }
    #endregion
}
