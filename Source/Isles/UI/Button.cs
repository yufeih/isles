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
using MouseButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Isles.Graphics;
using Isles.Engine;
using Isles;


namespace Isles.UI
{
    #region Button
    public class Button : UIElement
    {
        #region Fields
        /// <summary>
        /// Gets or sets button hot key
        /// </summary>
        public Keys HotKey
        {
            get { return hotKey; }
            set { hotKey = value; }
        }

        protected Keys hotKey;

        /// <summary>
        /// Button element state
        /// </summary>
        protected enum ButtonState
        {
            Normal, Hover, Press,
        }

        protected ButtonState state = ButtonState.Normal;

        /// <summary>
        /// The source rectangle for pressed-down button
        /// </summary>
        private Rectangle pressed;

        /// <summary>
        /// Gets or sets the source rectangle for pressed-down button
        /// </summary>
        public Rectangle Pressed
        {
            get { return pressed; }
            set { pressed = value; }
        }

        private Rectangle disabled;

        /// <summary>
        /// Gets or sets the source rectangle for disabled button
        /// </summary>
        public Rectangle Disabled
        {
            get { return disabled; }
            set { disabled = value; }
        }

        Rectangle hovered;

        /// <summary>
        /// Gets or sets the source rectangle for hovered button
        /// </summary>
        public Rectangle Hovered
        {
            get { return hovered; }
            set { hovered = value; }
        }

        Input input = BaseGame.Singleton.Input;

        /// <summary>
        /// Button click event
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Button right clicked event
        /// </summary>
        public event EventHandler RightClick;

        /// <summary>
        /// Button double clicked event
        /// </summary>
        public event EventHandler DoubleClick;

        /// <summary>
        /// Called when the mouse enters the region of this button
        /// </summary>
        public event EventHandler Enter;

        /// <summary>
        /// Called when the mouse leaves the region of this button
        /// </summary>
        public event EventHandler Leave;

        /// <summary>
        /// Used when a group of buttons are set
        /// </summary>
        public int Index = 0;

        private bool ignoreMessage = false;

        /// <summary>
        /// A fake button handles no messages
        /// </summary>
        public bool IgnoreMessage
        {
            get { return ignoreMessage; }
            set { ignoreMessage = value; }
        }
        #endregion

        #region Methods
        public Button()
        {
        }

        public Button(Rectangle area)
            : base(area)
        {
        }

        public Button(Texture2D texture,
            Rectangle area, Rectangle srcRectangle, Keys hotKey)
            : base(area)
        {
            Texture = texture;
            SourceRectangle = srcRectangle;
            hovered = srcRectangle;
            pressed = srcRectangle;
            disabled = srcRectangle;
            HotKey = hotKey;
        }

        /// <summary>
        /// For generating enter & leave events
        /// </summary>
        bool cursorInButton = false;

        protected override void OnVisibleChanged()
        {
            if (!Visible && input.MouseInBox(DestinationRectangle))
                if (Leave != null)
                    Leave(this, null);
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (!Visible && !Enabled)
                return;

            bool overlaps = input.MouseInBox(DestinationRectangle);

            // Trigger enter & leave events
            if (cursorInButton && !overlaps)
            {
                cursorInButton = false;
                if (Leave != null)
                    Leave(this, null);
            }
            else if (!cursorInButton && overlaps)
            {
                cursorInButton = true;
                if (Enter != null)
                    Enter(this, null);
            }

            // Update button state
            state = overlaps ? ((input.Mouse.LeftButton == MouseButtonState.Pressed ||
                                 input.Mouse.RightButton == MouseButtonState.Pressed) ?
                                 ButtonState.Press : ButtonState.Hover) : ButtonState.Normal;
        }

        /// <summary>
        /// Draw UI element
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (!Visible)
            {
                return;
            }
            Rectangle sourRect = SourceRectangle;
            Rectangle destRect = DestinationRectangle;
            if (Enabled)
            {
                switch (state)
                {
                    case ButtonState.Normal:
                        sourRect = SourceRectangle;
                        break;
                    case ButtonState.Hover:
                        sourRect = Hovered;
                        break;
                    case ButtonState.Press:
                        sourRect = Pressed;
                        break;
                }
            }
            else
            {
                sourRect = Disabled;
            }
            sprite.Draw(Texture, destRect, sourRect, Color.White);
        }

        /// <summary>
        /// Whether this button overlaps the cursor when left button is down
        /// </summary>
        bool clickThis = false;

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (ignoreMessage)
                return EventResult.Unhandled;
            
            if (Enabled && Visible)
            {
                Input input = sender as Input;
                Keys? key = tag as Keys?;

                // Handle left click
                if (type == EventType.LeftButtonDown && input.MouseInBox(DestinationRectangle))
                {
                    clickThis = true;
                    state = ButtonState.Press;
                    return EventResult.Handled;
                }

                if ((type == EventType.LeftButtonUp && input.MouseInBox(DestinationRectangle) && clickThis) ||
                    (type == EventType.KeyDown && key.Value == hotKey))
                {
                    state = ButtonState.Normal;
                    clickThis = false;
                    if (Click != null)
                        Click(this, null);
                    return EventResult.Handled;
                }

                // Handle right click
                if (type == EventType.RightButtonDown && input.MouseInBox(DestinationRectangle))
                {
                    clickThis = true;
                    state = ButtonState.Press;
                    return EventResult.Handled;
                }

                if (type == EventType.RightButtonUp && input.MouseInBox(DestinationRectangle) && clickThis)
                {
                    state = ButtonState.Normal;
                    clickThis = false;
                    if (RightClick != null)
                        RightClick(this, null);
                    return EventResult.Handled;
                }

                // Handle double click event
                if (type == EventType.DoubleClick && input.MouseInBox(DestinationRectangle))
                {
                    if (DoubleClick != null)
                        DoubleClick(this, null);
                    return EventResult.Handled;
                }
            }

            return EventResult.Unhandled;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Texture != null)
                    Texture.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion
    }
    #endregion

    #region TextButton
    public class TextButton : Button
    {

        TextField textField;

        /// <summary>
        /// Gets or sets the text for the button
        /// </summary>
        public String Text
        {
            get { return textField.Text; }
            set { textField.Text = value; }
        }

        private Color normalColor;

        /// <summary>
        /// Gets or sets the color of the text in 
        /// normal state
        /// </summary>
        public Color NormalColor
        {
            get { return normalColor; }
            set { normalColor = value; }
        }


        private Color highlightColor;


        /// <summary>
        /// Gets or sets the color of the text in
        /// Highlight state
        /// </summary>
        public Color HighlightColor
        {
            get { return highlightColor; }
            set { highlightColor = value; }
        }


        private Color pressDownColor;

        /// <summary>
        /// Gets or sets the color of the text in
        /// pressed-down state
        /// </summary>
        public Color PressDownColor
        {
            get { return pressDownColor; }
            set { pressDownColor = value; }
        }
	

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text"></param>
        public TextButton(String text, float fontSize, Color normalColor, Rectangle area)
        {
            this.Area = area;
            this.textField = new TextField(text, fontSize, normalColor,
                                 new Rectangle(0, 0, area.Width, area.Height));
            this.normalColor = normalColor;
            Vector4 color = normalColor.ToVector4();
            this.highlightColor = new Color(color + (Vector4.One - color) * 0.6f);
            this.pressDownColor = normalColor;
            this.textField.Parent = this;
            this.textField.Centered = true;
        }


        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Enabled)
                switch (state)
                {
                    case ButtonState.Normal:
                        textField.Color = normalColor;
                        break;
                    case ButtonState.Hover:
                        textField.Color = highlightColor;
                        break;
                    case ButtonState.Press:
                        textField.Color = pressDownColor;
                        break;
                }
            else
                this.textField.Color = new Color(normalColor.ToVector4() * 0.6f);
        }

        /// <summary>
        /// Draw
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="sprite"></param>
        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (state == ButtonState.Press)
            {
                this.textField.Y++;
                this.textField.X++;
            }

            this.textField.Draw(gameTime, sprite);

            if (state == ButtonState.Press)
            {
                this.textField.Y--;
                this.textField.X--;
            }          
        }
    }
    #endregion
}

