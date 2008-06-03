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
using System.IO;
using Isles.Graphics;
using Isles.Engine;
using Isles.UI;



namespace Isles.Screens
{
    public class ReadmePanel : TipBox
    {
        /// <summary>
        /// Page titles
        /// </summary>
        List<TextField> titles;

        /// <summary>
        /// Page contents
        /// </summary>
        List<List<TextField>> contents;

        /// <summary>
        /// Tatol page num
        /// </summary>
        int pages = 0;

        /// <summary>
        /// Current page index
        /// </summary>
        int currentPageIndex = 0;

        public int CurrentPageIndex
        {
            get { return currentPageIndex;}
            set 
            { 
                if(value >= 0 && value < pages)
                    currentPageIndex = value;
            }
        }

        Button previousPage, nextPage;
        Button ok;

        public Button OK
        {
            get { return ok; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pageTextures">Textures for pages</param>
        /// <param name="area">area</param>
        public ReadmePanel(Stream readmeText, Rectangle area)
            : base(area)
        {
            this.DialogCornerWidth = 6;

            Rectangle preButtonArea = new Rectangle(area.Width / 7, area.Height * 6 / 7,
                                                    area.Width / 7, area.Width / 21);
            Rectangle nextButtonArea = new Rectangle(area.Width * 5 / 7, area.Height * 6 / 7,
                                                    area.Width / 7, area.Width / 21);
            Rectangle okButtonArea = new Rectangle(area.Width * 3 / 7, area.Height * 6 / 7,
                                                    area.Width / 7, area.Width / 21);

            #region deleted
            //previousPage = new MenuButton(  BaseGame.Singleton.Content.Load<Texture2D>("UI/ReadmeButtons"),
            //                                 preButtonArea, new Rectangle(0, 123, 390, 123),Keys.P, null);
            //previousPage.Hightlighted = new Rectangle(390, 123, 390, 123);
            //previousPage.Pressed = new Rectangle(780, 123, 390, 123);

            //nextPage = new MenuButton(  BaseGame.Singleton.Content.Load<Texture2D>("UI/ReadmeButtons"),
            //                                 nextButtonArea, new Rectangle(0, 246, 390, 123), Keys.N, null);
            //nextPage.Hightlighted = new Rectangle(390, 246, 390, 123);
            //nextPage.Pressed = new Rectangle(780, 246, 390, 123);

            //ok = new MenuButton(  BaseGame.Singleton.Content.Load<Texture2D>("UI/ReadmeButtons"),
            //                                 okButtonArea, new Rectangle(0, 0, 390, 123), Keys.X, null);
            //ok.Hightlighted = new Rectangle(390, 0, 390, 123);
            //ok.Pressed = new Rectangle(780, 0, 390, 123);
            #endregion

            previousPage = new TextButton("Previous", 21f / 23, Color.Gold, preButtonArea);
            nextPage = new TextButton("Next", 21f / 23, Color.Gold, nextButtonArea);
            ok = new TextButton("OK", 21f / 23, Color.Gold, okButtonArea);

            previousPage.HotKey = Keys.Left;
            nextPage.HotKey = Keys.Right;
            ok.HotKey = Keys.Space;

            this.Add(previousPage);
            this.Add(nextPage);
            this.Add(ok);

            previousPage.Click += delegate(object o, EventArgs e)
            {
                Audios.Play("OK");

                CurrentPageIndex--;
                foreach (UIElement ui in this.elements)
                {
                    if (ui is TextField)
                        this.Remove(ui);
                }
                this.Add(titles[CurrentPageIndex]);
                titles[CurrentPageIndex].ResetDestinationRectangle();
                foreach (TextField t in contents[currentPageIndex])
                {
                    this.Add(t);
                    t.ResetDestinationRectangle();
                }

            };

            nextPage.Click += delegate(object o, EventArgs e)
            {
                Audios.Play("OK");

                CurrentPageIndex++;
                foreach (UIElement ui in this.elements)
                {
                    if (ui is TextField)
                        this.Remove(ui);
                }
                this.Add(titles[CurrentPageIndex]);
                foreach (TextField t in contents[currentPageIndex])
                {
                    this.Add(t);
                }
            };

            this.Mask = true;

            // Set page title and content

            titles = new List<TextField>();
            contents = new List<List<TextField>>();
            bool title = true;
            Color color = Color.White;
            int fontSize = 16;
            int heightOffset = 0;
            TextField currentTitle = null, currentContent = null;
            List<TextField> currentContentList = new List<TextField>();
            Rectangle contentArea = new Rectangle(area.Width / 8, area.Height / 6,
                                                    area.Width * 3 / 4, area.Height * 9 / 14);

            Rectangle titleArea = new Rectangle(area.Width / 20, area.Height / 20,
                                                area.Width * 11 / 12, 30);

            IOException ex = new IOException("The readme text is not well-formated.");
            using(StreamReader sr = new StreamReader(readmeText))
            {
                String line;
                while (null != (line = sr.ReadLine()))
                {
                    line.Trim();
                    if (line.Length == 0)
                        continue;
                    if (line.StartsWith("$FontSize$"))
                    {
                        line = line.Substring(10);
                        fontSize = Int32.Parse(line);
                    }
                    else if(line.StartsWith("$Color$"))
                    {
                        line = line.Substring(7);
                        line.Trim();
                        string[] colorElements = line.Split(new char[] {'(', ')', ','}, 
                                                        StringSplitOptions.RemoveEmptyEntries);
                        if (colorElements.Length != 3 && colorElements.Length != 4)
                        {
                            // Ill-formatted color tag, use white as defalut
                            color = Color.White;
                        }
                        byte r = byte.Parse(colorElements[0]);
                        byte g = byte.Parse(colorElements[1]);
                        byte b = byte.Parse(colorElements[2]);
                        if (colorElements.Length == 3)
                        {
                            color = new Color(r, g, b);
                        }
                        else
                        {
                            color = new Color(r, g, b, byte.Parse(colorElements[3]));
                        }
                    }
                    else if (line.StartsWith("$Title$"))
                    {
                        if (currentTitle != null)
                        {
                            titles.Add(currentTitle);
                            if (currentContent != null)
                                currentContentList.Add(currentContent);
                            contents.Add(currentContentList);
                            currentContentList = new List<TextField>();
                            currentContent = null;
                        }
                        currentContent = null;
                        currentTitle = new TextField(line.Substring(7), fontSize / 23f, color, titleArea);
                        currentTitle.Anchor = Anchor.TopLeft;
                        currentTitle.ScaleMode = ScaleMode.ScaleX;
                        currentTitle.Centered = true;
                        title = true;
                        heightOffset = 0;
                    }
                    else if (line.StartsWith("$Content$"))
                    {
                        if (currentContent != null)
                        {
                            heightOffset += currentContent.RealHeight + 10;
                            currentContentList.Add(currentContent);
                        }
                        Rectangle tempContentRect = new Rectangle(contentArea.X, contentArea.Y + heightOffset,
                                                                    contentArea.Width, contentArea.Height);

                        currentContent = new TextField(line.Substring(9), fontSize / 23f, color, tempContentRect);
                        currentContent.Anchor = Anchor.TopLeft;
                        currentContent.ScaleMode = ScaleMode.ScaleX;
                        title = false;
                    }
                    else if (title)
                    {
                        currentTitle.Text += "\n" + line;
                    }
                    else
                    {
                        currentContent.Text += "\n" + line;
                    }

                }
            }
            titles.Add(currentTitle);
            currentContentList.Add(currentContent);
            contents.Add(currentContentList);
            pages = titles.Count;
            if (pages > 0)
            {
                this.Add(titles[0]);
                foreach (TextField t in contents[0])
                {
                    this.Add(t);
                }
            }
        }

        /// <summary>
        /// Draw
        /// </summary>
        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            base.Draw(gameTime, sprite);
        }


        /// <summary>
        /// Event Handler
        /// </summary>
        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (Visible && Enabled)
            {
                previousPage.HandleEvent(type, sender, tag);
                nextPage.HandleEvent(type, sender, tag);
                ok.HandleEvent(type, sender, tag);
            }
            return base.HandleEvent(type, sender, tag);
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            previousPage.Enabled = true;
            nextPage.Enabled = true;
            if (currentPageIndex == 0)
                previousPage.Enabled = false;
            if (currentPageIndex == pages - 1)
                nextPage.Enabled = false;            
        }
    }
}
