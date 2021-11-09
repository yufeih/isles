// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Screens;

public class ReadmePanel : TipBox
{
    private readonly List<TextField> titles;
    private readonly List<List<TextField>> contents;
    private readonly int pages;
    private int currentPageIndex;

    public int CurrentPageIndex
    {
        get => currentPageIndex;
        set
        {
            if (value >= 0 && value < pages)
            {
                currentPageIndex = value;
            }
        }
    }

    private readonly Button previousPage;
    private readonly Button nextPage;

    public Button OK { get; }

    public ReadmePanel(Rectangle area)
        : base(area)
    {
        DialogCornerWidth = 6;

        var preButtonArea = new Rectangle(area.Width / 7, area.Height * 6 / 7,
                                                area.Width / 7, area.Width / 21);
        var nextButtonArea = new Rectangle(area.Width * 5 / 7, area.Height * 6 / 7,
                                                area.Width / 7, area.Width / 21);
        var okButtonArea = new Rectangle(area.Width * 3 / 7, area.Height * 6 / 7,
                                                area.Width / 7, area.Width / 21);

        previousPage = new TextButton("Previous", 21f / 23, Color.Gold, preButtonArea);
        nextPage = new TextButton("Next", 21f / 23, Color.Gold, nextButtonArea);
        OK = new TextButton("OK", 21f / 23, Color.Gold, okButtonArea);

        previousPage.HotKey = Keys.Left;
        nextPage.HotKey = Keys.Right;
        OK.HotKey = Keys.Space;

        Add(previousPage);
        Add(nextPage);
        Add(OK);

        previousPage.Click += (o, e) =>
        {
            Audios.Play("OK");

            CurrentPageIndex--;
            foreach (UIElement ui in elements)
            {
                if (ui is TextField)
                {
                    Remove(ui);
                }
            }

            Add(titles[CurrentPageIndex]);
            titles[CurrentPageIndex].ResetDestinationRectangle();
            foreach (TextField t in contents[currentPageIndex])
            {
                Add(t);
                t.ResetDestinationRectangle();
            }
        };

        nextPage.Click += (o, e) =>
        {
            Audios.Play("OK");

            CurrentPageIndex++;
            foreach (UIElement ui in elements)
            {
                if (ui is TextField)
                {
                    Remove(ui);
                }
            }

            Add(titles[CurrentPageIndex]);
            foreach (TextField t in contents[currentPageIndex])
            {
                Add(t);
            }
        };

        Mask = true;

        // Set page title and content
        titles = new List<TextField>();
        contents = new List<List<TextField>>();
        var title = true;
        Color color = Color.White;
        var fontSize = 16;
        var heightOffset = 0;
        TextField currentTitle = null, currentContent = null;
        var currentContentList = new List<TextField>();
        var contentArea = new Rectangle(area.Width / 8, area.Height / 6,
                                                area.Width * 3 / 4, area.Height * 9 / 14);

        var titleArea = new Rectangle(area.Width / 20, area.Height / 20,
                                            area.Width * 11 / 12, 30);

        foreach (var aLine in File.ReadAllLines("data/readme.txt"))
        {
            var line = aLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("$FontSize$"))
            {
                line = line.Substring(10);
                fontSize = int.Parse(line);
            }
            else if (line.StartsWith("$Color$"))
            {
                line = line.Substring(7);
                line.Trim();
                var colorElements = line.Split(new char[] { '(', ')', ',' },
                                                StringSplitOptions.RemoveEmptyEntries);
                if (colorElements.Length != 3 && colorElements.Length != 4)
                {
                    // Ill-formatted color tag, use white as defalut
                    color = Color.White;
                }

                var r = byte.Parse(colorElements[0]);
                var g = byte.Parse(colorElements[1]);
                var b = byte.Parse(colorElements[2]);
                color = colorElements.Length == 3 ? new Color(r, g, b) : new Color(r, g, b, byte.Parse(colorElements[3]));
            }
            else if (line.StartsWith("$Title$"))
            {
                if (currentTitle != null)
                {
                    titles.Add(currentTitle);
                    if (currentContent != null)
                    {
                        currentContentList.Add(currentContent);
                    }

                    contents.Add(currentContentList);
                    currentContentList = new List<TextField>();
                    currentContent = null;
                }

                currentContent = null;
                currentTitle = new TextField(line.Substring(7), fontSize / 23f, color, titleArea)
                {
                    Anchor = Anchor.TopLeft,
                    ScaleMode = ScaleMode.ScaleX,
                    Centered = true,
                };
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

                var tempContentRect = new Rectangle(contentArea.X, contentArea.Y + heightOffset,
                                                            contentArea.Width, contentArea.Height);

                currentContent = new TextField(line.Substring(9), fontSize / 23f, color, tempContentRect)
                {
                    Anchor = Anchor.TopLeft,
                    ScaleMode = ScaleMode.ScaleX,
                };
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

        titles.Add(currentTitle);
        currentContentList.Add(currentContent);
        contents.Add(currentContentList);
        pages = titles.Count;
        if (pages > 0)
        {
            Add(titles[0]);
            foreach (TextField t in contents[0])
            {
                Add(t);
            }
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch sprite)
    {
        base.Draw(gameTime, sprite);
    }

    public override EventResult HandleEvent(EventType type, object sender, object tag)
    {
        if (Visible && Enabled)
        {
            previousPage.HandleEvent(type, sender, tag);
            nextPage.HandleEvent(type, sender, tag);
            OK.HandleEvent(type, sender, tag);
        }

        return base.HandleEvent(type, sender, tag);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        previousPage.Enabled = true;
        nextPage.Enabled = true;
        if (currentPageIndex == 0)
        {
            previousPage.Enabled = false;
        }

        if (currentPageIndex == pages - 1)
        {
            nextPage.Enabled = false;
        }
    }
}
