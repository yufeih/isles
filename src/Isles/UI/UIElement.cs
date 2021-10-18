//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;

namespace Isles.UI
{
    #region IUIElement
    /// <summary>
    /// Interface for implementing UI element
    /// </summary>
    public interface IUIElement : IDisposable, IEventListener
    {
        /// <summary>
        /// Gets the reference area of an UI element.
        /// The reference area is used to determine the final
        /// area of the UI element due to resolusion changes
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
        /// Gets or sets UI element anchor
        /// </summary>
        Anchor Anchor { get; set; }

        /// <summary>
        /// Gets or sets UI element scale mode
        /// </summary>
        ScaleMode ScaleMode { get; set; }

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

    // The middle control panel should use ScaleMode.Stretch 
    // & Anchor.BottomLeft.
    // Other control panels and Menu panels use ScaleMode.ScaleY 
    // including little map. Their anchor mode is trivial.
    // All buttons and progress bars use ScaleMode.ScaleY &
    // Anchor.TopLeft.
    // 
    // Exceptions : The Anchor Mode of the UIElements in the 
    // middle control panel may be a little more complex.
    // 

    /// <summary>
    /// Enumeration type used to set the location of an UI element
    /// </summary>
    public enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
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
        /// This scale mode is supposed to be used for most UIElement
        /// </summary>
        ScaleY,
        
        /// <summary>
        /// Width and height all changes 
        /// </summary>
        Stretch
    }
    #endregion

    #region UIElement
    /// <summary>
    /// Basic UI element
    /// </summary>
    public abstract class UIElement : IUIElement
    {
        #region Fields
        /// <summary>
        /// Gets or sets UIElement area
        /// </summary>
        virtual public Rectangle Area
        {
            get { return area; }
            set { area = value; IsDirty = true; }
        }

        Rectangle area;


        /// <summary>
        /// Gets button destination rectangle
        /// </summary>
        virtual public Rectangle DestinationRectangle
        {
            get
            {
                if (IsDirty)
                {
                    IsDirty = false;
                    ResetDestinationRectangle();
                }

                return destinationRectangle;
            }
        }

        virtual public void ResetDestinationRectangle()
        {
            destinationRectangle = GetRelativeRectangle(area);
        }

        public Graphics2D Graphics2D = BaseGame.Singleton.Graphics2D;
        
        /// <summary>
        /// Whether destination rectangle is dirty
        /// </summary>
        public bool IsDirty = true;

        Rectangle destinationRectangle;


        /// <summary>
        /// Gets or sets button source rectangle
        /// </summary>
        public Rectangle SourceRectangle
        {
            get { return sourceRectangle; }
            set { sourceRectangle = value; }
        }

        Rectangle sourceRectangle;


        /// <summary>
        /// Gets or sets UI element visibility
        /// </summary>
        public bool Visible
        {
            get { return visible; }
            set { visible = value; OnVisibleChanged(); }
        }

        bool visible = true;

        protected virtual void OnVisibleChanged() { }

        /// <summary>
        /// Gets or sets whether an UI element is enabled
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; OnEnableStateChanged(); }
        }

        bool enabled = true;

        protected virtual void OnEnableStateChanged() { }


        /// <summary>
        /// Gets or sets the texture used to draw the button
        /// </summary>
        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        Texture2D texture;


        /// <summary>
        /// Gets or sets UI element x position
        /// </summary>
        public int X
        {
            get { return area.X; }
            set { area.X = value; IsDirty = true; }
        }


        /// <summary>
        /// Gets or sets UI element x position
        /// </summary>
        public int Y
        {
            get { return area.Y; }
            set { area.Y = value; IsDirty = true; }
        }


        /// <summary>
        /// Gets or sets the anchor mode of the UI element
        /// </summary>
        public Anchor Anchor
        {
            get { return anchor; }
            set { anchor = value; IsDirty = true; }
        }

        Anchor anchor = Anchor.TopLeft;


        /// <summary>
        /// Gets or sets the scale mode of the UI element
        /// </summary>
        public ScaleMode ScaleMode
        {
            get { return scaleMode; }
            set { scaleMode = value; IsDirty = true; }
        }

        ScaleMode scaleMode = ScaleMode.ScaleY;


        /// <summary>
        /// Gets or sets the parent of this UI element
        /// </summary>
        public IUIElement Parent
        {
            get { return parent; }
            set { parent = value; IsDirty = true; }
        }

        IUIElement parent;


        /// <summary>
        /// Gets or sets a tag
        /// </summary>
        public object Tag;
        #endregion

        #region Methods
        public UIElement() { }

        /// <summary>
        /// Create the UI element 
        /// </summary>
        /// <param name="area"></param>
        public UIElement(Rectangle area)
        {
            Area = area;
        }

        /// <summary>
        /// Update UI element
        /// </summary>
        /// <param name="gameTime"></param>
        public abstract void Update(GameTime gameTime);

        /// <summary>
        /// Draw UI element
        /// </summary>
        /// <param name="gameTime"></param>
        public abstract void Draw(GameTime gameTime, SpriteBatch sprite);

        /// <summary>
        /// Handle input events
        /// </summary>
        public abstract EventResult HandleEvent(EventType type, object sender, object tag);

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
        protected virtual void Dispose(bool disposing) { }
        #endregion

        #region GetRelativeRectangle
        /// <summary>
        /// Gets the relative rectangle based on current anchor,
        /// scale mode and parent reference rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle in standard resolution</param>
        /// <return>Rectangle in current resolution</return>
        public Rectangle GetRelativeRectangle(Rectangle rectangle)
        {
            return GetRelativeRectangle(rectangle, parent, scaleMode, anchor);
        }


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

            double heightScale, widthScale;

            // scale
            if (scaleMode == ScaleMode.Stretch)
            {
                widthScale = (double)parent.DestinationRectangle.Width / parent.Area.Width;
                relativeRectangle.Width = rectangle.Width *
                    parent.DestinationRectangle.Width / parent.Area.Width;
                heightScale = (double)parent.DestinationRectangle.Height / parent.Area.Height;
                relativeRectangle.Height = rectangle.Height *
                    parent.DestinationRectangle.Height / parent.Area.Height;
            }
            else if (scaleMode == ScaleMode.ScaleY)
            {
                widthScale = (double)parent.DestinationRectangle.Height / parent.Area.Height;
                relativeRectangle.Width = rectangle.Width *
                    parent.DestinationRectangle.Height / parent.Area.Height;
                heightScale = (double)parent.DestinationRectangle.Height / parent.Area.Height;
                relativeRectangle.Height = rectangle.Height *
                    parent.DestinationRectangle.Height / parent.Area.Height;
            }
            else if (scaleMode == ScaleMode.ScaleX)
            {
                heightScale = widthScale = (double)parent.DestinationRectangle.Width / parent.Area.Width;
                relativeRectangle.Width = rectangle.Width *
                    parent.DestinationRectangle.Width / parent.Area.Width;
                relativeRectangle.Height = rectangle.Height *
                    parent.DestinationRectangle.Width / parent.Area.Width;
            }
            else
            {
                heightScale = widthScale = 1;
                relativeRectangle.Width = rectangle.Width;
                relativeRectangle.Height = rectangle.Height;
            }


            // anchor

            if (anchor == Anchor.TopLeft || anchor == Anchor.BottomLeft)
            {
                relativeRectangle.X = (int)(parent.DestinationRectangle.Left +
                    rectangle.Left * widthScale);
            }
            else
            {
                relativeRectangle.X = (int)(parent.DestinationRectangle.Right +
                    (rectangle.Right - parent.Area.Width) *
                        widthScale - relativeRectangle.Width);
            }

            if (anchor == Anchor.TopLeft || anchor == Anchor.TopRight)
            {
                relativeRectangle.Y = (int)(parent.DestinationRectangle.Top +
                    rectangle.Top * heightScale);
            }
            else
            {
                relativeRectangle.Y = (int)(parent.DestinationRectangle.Bottom +
                    (rectangle.Bottom - parent.Area.Height) *
                        heightScale -
                            relativeRectangle.Height);
            }

            if (anchor == Anchor.Center)
            {
                if (scaleMode == ScaleMode.Stretch)
                {
                    widthScale = (double)parent.DestinationRectangle.Width / rectangle.Width;
                    heightScale = (double)parent.DestinationRectangle.Height / rectangle.Height;
                }
                //else
                //{
                //    widthScale = (double)parent.DestinationRectangle.Width / parent.Area.Width;
                //    heightScale = (double)parent.DestinationRectangle.Height / parent.Area.Height;
                //}
                if (heightScale > widthScale)
                {
                    relativeRectangle.Width = (int)(widthScale * rectangle.Width);
                    relativeRectangle.Height = (int)(widthScale * rectangle.Height);

                    relativeRectangle.X = (int)((parent.DestinationRectangle.Left + parent.DestinationRectangle.Right
                                            - rectangle.Width * widthScale) / 2);
                    relativeRectangle.Y = (int)((parent.DestinationRectangle.Top + parent.DestinationRectangle.Bottom
                                            - rectangle.Height * widthScale) / 2); 
                }
                else
                {
                    relativeRectangle.Width = (int)(heightScale * rectangle.Width);
                    relativeRectangle.Height = (int)(heightScale * rectangle.Height);
                    relativeRectangle.X = (int)((parent.DestinationRectangle.Left + parent.DestinationRectangle.Right
                                            - rectangle.Width * heightScale) / 2);
                    relativeRectangle.Y = (int)((parent.DestinationRectangle.Top + parent.DestinationRectangle.Bottom
                                            - rectangle.Height * heightScale) / 2); 
                }
            }
            

            return relativeRectangle;
        }
        #endregion
    }
    #endregion
}
