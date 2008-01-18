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
using Isles.Engine;

namespace Isles.UI
{
    #region IUIElement
    /// <summary>
    /// Interface for implementing a every UI element
    /// </summary>
    public interface IUIElement : IDisposable
    {
        /// <summary>
        /// Gets the reference area of an UI element.
        /// The reference area is used to determine the final
        /// area of the UI element duo to resolusion changes
        /// </summary>
        Rectangle Area { get; set; }

        /// <summary>
        /// Gets or sets the area that the UI element
        /// will be rendered to.
        /// </summary>
        Rectangle DestinationRectangle { get; }

        /// <summary>
        /// Gets or sets the parent of an UI element
        /// </summary>
        IUIElement Parent { get; set; }

        /// <summary>
        /// Gets or sets UI element visibility
        /// </summary>
        bool Visible { get; set; }

        /// <summary>
        /// Enable/Disable an UI element
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Update UI element
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Draw UI element
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime, SpriteBatch sprite);
    }
    #endregion

    #region Enumeration types
    /// <summary>
    /// Enumeration type used to set the location of an UI element
    /// </summary>
    public enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// how UI element scales when resolution changed
    /// </summary>
    public enum ScaleMode
    {
        /// <summary>
        /// Size of the UI element is alway fixed
        /// </summary>
        Fixed,
        
        /// <summary>
        /// Size changes based on width, but width/height is fixed
        /// </summary>
        ScaleX,

        /// <summary>
        /// Size changes based on height, but width/height is fixed
        /// </summary>
        ScaleY,
        
        /// <summary>
        /// Width and height all changes 
        /// </summary>
        Stretch
    }

    /// <summary>
    /// UI element state
    /// </summary>
    public enum UIState
    {
        Normal, Hover, Press,
    }
    #endregion

    #region UIElement
    /// <summary>
    /// Basic UI element
    /// </summary>
    public class UIElement : IUIElement
    {
        #region Fields
        protected Rectangle area;
        protected IUIElement parent;
        protected Anchor anchor = Anchor.TopLeft;
        protected ScaleMode scaleMode = ScaleMode.ScaleY;
        protected UIState state = UIState.Normal;
        protected bool visible = true;
        protected bool enabled = true;

        /// <summary>
        /// Texture used to draw the button
        /// </summary>
        protected Texture2D texture;

        /// <summary>
        /// Button hot region
        /// </summary>
        protected Rectangle destinationRectangle;

        /// <summary>
        /// Button hot region
        /// </summary>
        protected Rectangle sourceRectangle;

        /// <summary>
        /// Gets the reference area of the UI element
        /// </summary>
        public Rectangle ReferenceArea
        {
            get { return area; }
        }

        /// <summary>
        /// Gets or sets button area
        /// </summary>
        public Rectangle Area
        {
            get { return area; }
            set { area = value; }
        }

        /// <summary>
        /// Gets button destination rectangle
        /// </summary>
        public Rectangle DestinationRectangle
        {
            get { return destinationRectangle; }
        }

        /// <summary>
        /// Gets or sets button source rectangle
        /// </summary>
        public Rectangle SourceRectangle
        {
            get { return sourceRectangle; }
            set { sourceRectangle = value; }
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
        /// Gets or sets whether an UI element is enabled
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; OnEnableStateChanged(); }
        }

        protected virtual void OnEnableStateChanged()
        {

        }

        /// <summary>
        /// Gets current UI state
        /// </summary>
        public UIState State
        {
            get { return state; }
        }

        /// <summary>
        /// Gets or sets the texture used to draw the button
        /// </summary>
        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        /// <summary>
        /// Gets or sets UI element x position
        /// </summary>
        public int X
        {
            get { return area.X; }
            set { area.X = value; }
        }

        /// <summary>
        /// Gets or sets UI element x position
        /// </summary>
        public int Y
        {
            get { return area.Y; }
            set { area.Y = value; }
        }

        /// <summary>
        /// Gets or sets the anchor mode of the UI element
        /// </summary>
        public Anchor Anchor
        {
            get { return anchor; }
            set { anchor = value; }
        }

        /// <summary>
        /// Gets or sets the scale mode of the UI element
        /// </summary>
        public ScaleMode ScaleMode
        {
            get { return scaleMode; }
            set { scaleMode = value; }
        }

        /// <summary>
        /// Gets or sets the parent of this UI element
        /// </summary>
        public IUIElement Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        /// <summary>
        /// Gets or sets a tag
        /// </summary>
        public object Tag;
        #endregion

        #region Methods
        public UIElement()
        {
        }

        /// <summary>
        /// Create the UI element 
        /// </summary>
        /// <param name="area"></param>
        public UIElement(Rectangle area)
        {
            this.area = area;

            this.destinationRectangle = GetRelativeRectangle(area);
        }

        /// <summary>
        /// Update UI element
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime)
        {
            this.destinationRectangle = GetRelativeRectangle(area);
        }

        /// <summary>
        /// Draw UI element
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Draw(GameTime gameTime, SpriteBatch sprite)
        {
        }

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
        }
        #endregion

        /// <summary>
        /// Gets the relative rectangle based on current anchor,
        /// scale mode and parent reference rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle in standard resolution</param>
        /// <return>Rectangle in current resolution</return>
        public Rectangle GetRelativeRectangle(Rectangle rectangle)
        {
            return UIDisplay.GetRelativeRectangle(rectangle, parent, scaleMode, anchor);
        }
    }
    #endregion

    #region Panel
    public class Panel : UIElement
    {
        /// <summary>
        /// All UI elements in this panel
        /// </summary>
        protected List<IUIElement> elements = new List<IUIElement>();

        public Panel()
        {
        }

        /// <summary>
        /// Create a panel
        /// </summary>
        /// <param name="area"></param>
        public Panel(Rectangle area) : base(area)
        {
        }

        /// <summary>
        /// Adds an UI element to the panel
        /// </summary>
        /// <param name="element"></param>
        public virtual void Add(IUIElement element)
        {
            element.Parent = this;
            elements.Add(element);
        }

        /// <summary>
        /// Removes an UI elment from the panel
        /// </summary>
        /// <param name="element"></param>
        public virtual void Remove(IUIElement element)
        {
            element.Parent = null;
            elements.Remove(element);
        }

        public virtual void Clear()
        {
            foreach (IUIElement element in elements)
                element.Parent = null;

            elements.Clear();
        }

        protected override void OnEnableStateChanged()
        {
            foreach (IUIElement element in elements)
                element.Enabled = this.enabled;
        }

        /// <summary>
        /// Update all UI elements
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (!visible)
                return;

            base.Update(gameTime);

            foreach (IUIElement element in elements)
                element.Update(gameTime);
        }

        /// <summary>
        /// Draw all UI elements
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (!visible)
                return;

            if (texture != null)
                sprite.Draw(texture, destinationRectangle, sourceRectangle, Color.White);

            foreach (IUIElement element in elements)
                if (element.Visible)
                    element.Draw(gameTime, sprite);
        }
    }
    #endregion

    #region Scroll Panel
    /// <summary>
    /// Game scroll panel
    /// </summary>
    public class ScrollPanel : Panel
    {
        int current, max;
        int buttonWidth, scrollButtonWidth, buttonHeight;

        public Button LeftScroll;
        public Button RightScroll;

        public ScrollPanel(Rectangle area, int buttonWidth, int scrollButtonWidth)
            : base(area)
        {
            this.buttonWidth = buttonWidth;
            this.buttonHeight = destinationRectangle.Height;
            this.scrollButtonWidth = scrollButtonWidth;

            current = 0;

            LeftScroll = new Button(new Rectangle(
                0, 0, scrollButtonWidth, buttonHeight));

            RightScroll = new Button(new Rectangle(
                scrollButtonWidth, 0, scrollButtonWidth, buttonHeight));

            LeftScroll.Enabled = false;

            LeftScroll.Parent = RightScroll.Parent = this;

            LeftScroll.Anchor = RightScroll.Anchor = Anchor.BottomLeft;

            LeftScroll.ScaleMode = ScaleMode.ScaleY;
            RightScroll.ScaleMode = ScaleMode.ScaleY;

            LeftScroll.Click += new EventHandler(LeftScroll_Click);
            RightScroll.Click += new EventHandler(RightScroll_Click);
        }

        void RightScroll_Click(object sender, EventArgs e)
        {
            if (enabled)
            {
                if (current < elements.Count - max)
                {
                    current++;
                    LeftScroll.Enabled = true;

                    if (current == elements.Count - max)
                        RightScroll.Enabled = false;
                }
            }
        }

        void LeftScroll_Click(object sender, EventArgs e)
        {
            if (enabled)
            {
                if (current > 0)
                {
                    current--;
                    RightScroll.Enabled = true;

                    if (current == 0)
                        LeftScroll.Enabled = false;
                }
            }
        }

        public override void Add(IUIElement element)
        {
            UIElement e = element as UIElement;

            if (e != null)
            {
                // Reset element area
                Rectangle rect = new Rectangle(
                    scrollButtonWidth + elements.Count * buttonWidth, 0,
                    buttonWidth, buttonHeight);
                
                e.Area = rect;
                e.Anchor = Anchor.BottomLeft;
                e.ScaleMode = ScaleMode.ScaleY;

                if (elements.Count < max)
                {
                    rect.X += buttonWidth;
                    RightScroll.Enabled = false;
                }
                else
                {
                    RightScroll.Enabled = true;
                }

                rect.Width = scrollButtonWidth;

                RightScroll.Area = rect;
            }

            base.Add(element);
        }

        public override void Clear()
        {
            current = 0;
            max = (destinationRectangle.Width - scrollButtonWidth * 2) / buttonWidth;

            LeftScroll.Enabled = RightScroll.Enabled = false;

            RightScroll.Area = new Rectangle(
                scrollButtonWidth, 0, scrollButtonWidth, buttonHeight);

            base.Clear();
        }

        public override void Update(GameTime gameTime)
        {
            if (!visible)
                return;

            LeftScroll.Update(gameTime);
            RightScroll.Update(gameTime);

            Rectangle rect;

            rect.X = scrollButtonWidth - buttonWidth * current;
            rect.Y = 0;
            rect.Width = buttonWidth;
            rect.Height = buttonHeight;

            int n = current + max;
            if (n > elements.Count)
                n = elements.Count;

            for (int i = 0; i < elements.Count; i++)
            {
                if (i >= current && i < n)
                {
                    // Reset element area
                    elements[i].Visible = true;
                    elements[i].Area = rect;
                }
                else
                {
                    elements[i].Visible = false;
                }
                
                rect.X += buttonWidth;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (!visible)
                return;

            LeftScroll.Draw(gameTime, sprite);
            RightScroll.Draw(gameTime, sprite);

            base.Draw(gameTime, sprite);
        }
    }
    #endregion

    #region Button
    public class Button : UIElement
    {
        #region Fields
        /// <summary>
        /// Button hot key
        /// </summary>
        protected Keys hotKey;

        /// <summary>
        /// Gets or sets button hot key
        /// </summary>
        public Keys HotKey
        {
            get { return hotKey; }
            set { hotKey = value; }
        }

        /// <summary>
        /// Button click event
        /// </summary>
        public event EventHandler Click;

        #endregion

        #region Methods
        public Button()
        {
        }

        public Button(Rectangle destRect)
            : base(destRect)
        {
        }

        public Button(Texture2D texture,
            Rectangle destRect, Rectangle srcRect, Keys hotKey)
            : base(destRect)
        {
            this.texture = texture;
            this.destinationRectangle = destRect;
            this.sourceRectangle = srcRect;
            this.hotKey = hotKey;
        }

        public override void Update(GameTime gameTime)
        {
            if (!visible)
                return;

            base.Update(gameTime);

            state = Input.MouseInBox(destinationRectangle) ? UIState.Hover : UIState.Normal;

            if (enabled &&
               ((state == UIState.Hover && Input.MouseLeftButtonJustPressed) ||
                Input.KeyboardKeyJustPressed(hotKey)))
            {
                // Suppress mouse and keyboard events,
                // No one else will receive those events in this frame
                Input.SuppressMouseEvent();
                Input.SuppressKeyboardEvent();

                state = UIState.Press;
                if (Click != null)
                    Click(this, null);
            }
        }

        /// <summary>
        /// Draw UI element
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (texture != null && visible)
            {
                Color color = Color.White;

                if (state == UIState.Normal)
                    color = Color.White;
                else
                    color = Color.Yellow;

                if (!enabled)
                    color = Color.Gray;

                sprite.Draw(texture, destinationRectangle, sourceRectangle, color);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (texture != null)
                    texture.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion
    }
    #endregion
}
