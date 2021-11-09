// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.UI;

public class Panel : UIElement
{
    protected BroadcastList<UIElement, List<UIElement>> elements = new();

    public IEnumerable<UIElement> Elements => elements;

    private Rectangle effectiveRegion;

    /// <summary>
    /// Gets or sets the region that takes effect
    /// under reference resolution.
    /// </summary>
    public Rectangle EffectiveRegion
    {
        get => effectiveRegion;
        set => effectiveRegion = value;
    }

    private Rectangle actualEffectiveRegion;

    public Rectangle ActualEffectiveRegion
    {
        get
        {
            if (IsDirty)
            {
                actualEffectiveRegion = GetRelativeRectangle(effectiveRegion);
            }

            return actualEffectiveRegion;
        }
    }

    public override Rectangle DestinationRectangle
    {
        get
        {
            if (IsDirty)
            {
                ResetDestinationRectangle();
            }

            return base.DestinationRectangle;
        }
    }

    public override void ResetDestinationRectangle()
    {
        base.ResetDestinationRectangle();
        actualEffectiveRegion = GetRelativeRectangle(effectiveRegion);
    }

    /// <summary>
    /// Create a panel.
    /// </summary>
    /// <param name="area"></param>
    public Panel(Rectangle area)
        : base(area)
    {
        effectiveRegion = Area;
    }

    /// <summary>
    /// Adds an UI element to the panel.
    /// </summary>
    /// <param name="element"></param>
    public virtual void Add(UIElement element)
    {
        element.Parent = this;
        elements.Add(element);
    }

    /// <summary>
    /// Removes an UI elment from the panel.
    /// </summary>
    /// <param name="element"></param>
    public virtual void Remove(UIElement element)
    {
        if (element != null)
        {
            element.Parent = null;
            elements.Remove(element);
        }
    }

    public virtual void Clear()
    {
        foreach (var element in elements)
        {
            element.Parent = null;
        }

        elements.Clear();
    }

    protected override void OnEnableStateChanged()
    {
        foreach (var element in elements)
        {
            element.Enabled = Enabled;
        }
    }

    /// <summary>
    /// Update all UI elements.
    /// </summary>
    /// <param name="gameTime"></param>
    public override void Update(GameTime gameTime)
    {
        foreach (var element in elements)
        {
            element.Update(gameTime);
        }
    }

    /// <summary>
    /// Draw all UI elements.
    /// </summary>
    /// <param name="gameTime"></param>
    public override void Draw(GameTime gameTime, SpriteBatch sprite)
    {
        if (Visible)
        {
            if (Texture != null)
            {
                sprite.Draw(Texture, DestinationRectangle, SourceRectangle, Color.White);
            }

            foreach (var element in elements)
            {
                element.Draw(gameTime, sprite);
            }
        }
    }

    public override EventResult HandleEvent(EventType type, object sender, object tag)
    {
        if (Visible && Enabled)
        {
            var input = sender as Input;

            foreach (var element in elements)
            {
                if (element.Enabled &&
                    element.HandleEvent(type, sender, tag) == EventResult.Handled)
                {
                    return EventResult.Handled;
                }
            }

            // Block mouse events
            if ((type == EventType.LeftButtonDown || type == EventType.RightButtonDown ||
                 type == EventType.DoubleClick || type == EventType.MiddleButtonDown) &&
                 input.MouseInBox(ActualEffectiveRegion))
            {
                return EventResult.Handled;
            }
        }

        return EventResult.Unhandled;
    }
}

/// <summary>
/// Game scroll panel.
/// </summary>
public class ScrollPanel : Panel
{
    /// <summary>
    /// Index of the left most UIElement shown currently.
    /// </summary>
    private int current;

    /// <summary>
    /// Max number of UIElement visible.
    /// </summary>
    private int max;
    private readonly int buttonWidth;
    private readonly int scrollButtonWidth;
    private readonly int buttonHeight;

    public Button Left;
    public Button Right;

    public ScrollPanel(Rectangle area, int buttonWidth, int scrollButtonWidth)
        : base(area)
    {
        this.buttonWidth = buttonWidth;
        buttonHeight = DestinationRectangle.Height;
        this.scrollButtonWidth = scrollButtonWidth;

        current = 0;

        Left = new Button(new Rectangle(
            0, 0, scrollButtonWidth, buttonHeight));

        Right = new Button(new Rectangle(
            scrollButtonWidth, 0, scrollButtonWidth, buttonHeight));

        Left.Parent = Right.Parent = this;
        Left.Enabled = Right.Enabled = false;
        Left.Anchor = Right.Anchor = Anchor.BottomLeft;
        Left.ScaleMode = Right.ScaleMode = ScaleMode.ScaleY;

        Left.Click += LeftScroll_Click;
        Right.Click += RightScroll_Click;
    }

    private void RightScroll_Click(object sender, EventArgs e)
    {
        if (Enabled)
        {
            if (current < elements.Count - max)
            {
                current++;
                Left.Enabled = true;

                if (current == elements.Count - max)
                {
                    Right.Enabled = false;
                }
            }
        }
    }

    private void LeftScroll_Click(object sender, EventArgs e)
    {
        if (Enabled)
        {
            if (current > 0)
            {
                current--;
                Right.Enabled = true;

                if (current == 0)
                {
                    Left.Enabled = false;
                }
            }
        }
    }

    public override void Add(UIElement element)
    {
        // Scroll panel works only with UIElement
        if (element is not UIElement e)
        {
            throw new ArgumentException();
        }

        // Reset element area
        var rect = new Rectangle(
            scrollButtonWidth + (elements.Count - current) * buttonWidth,
            0, buttonWidth, buttonHeight);

        e.Area = rect;
        e.Anchor = Anchor.BottomLeft;
        e.ScaleMode = ScaleMode.ScaleY;

        Right.Enabled = elements.Count >= current + max;

        rect.X += buttonWidth;
        rect.Width = scrollButtonWidth;

        Right.Area = rect;

        base.Add(element);
    }

    public override void Remove(UIElement element)
    {
        base.Remove(element);

        throw new NotImplementedException();
    }

    public override void Clear()
    {
        current = 0;
        max = (DestinationRectangle.Width - scrollButtonWidth * 2) / buttonWidth;

        Left.Enabled = Right.Enabled = false;

        Right.Area = new Rectangle(
            scrollButtonWidth, 0, scrollButtonWidth, buttonHeight);

        base.Clear();
    }

    public override void Update(GameTime gameTime)
    {
        if (!Visible)
        {
            return;
        }

        Left.Update(gameTime);
        Right.Update(gameTime);

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch sprite)
    {
        if (!Visible)
        {
            return;
        }

        Rectangle rect;

        rect.X = scrollButtonWidth;
        rect.Y = 0;
        rect.Width = buttonWidth;
        rect.Height = buttonHeight;

        var n = current + max - 1;
        if (n > elements.Count)
        {
            n = elements.Count;
        }

        for (var i = 0; i < elements.Count; i++)
        {
            if (i >= current && i < n)
            {
                // Reset element area
                elements.Elements[i].Visible = true;
                elements.Elements[i].Area = rect;
                rect.X += buttonWidth;
            }
            else
            {
                elements.Elements[i].Visible = false;
            }
        }

        rect.Width /= 2;
        Right.Area = rect;

        Left.Draw(gameTime, sprite);
        Right.Draw(gameTime, sprite);

        base.Draw(gameTime, sprite);
    }
}

public class TextField : Panel
{
    private string text;

    /// <summary>
    /// Gets or sets the text to be displayed.
    /// </summary>
    public string Text
    {
        get => text;
        set
        {
            text = value;

            if (text == null)
            {
                FormatedText = null;
                return;
            }

            // Format the input text based on text field size and font size
            FormatedText = Graphics2D.FormatString(text, DestinationRectangle.Width,
                                                         DestinationRectangle.Height,
                                                         FontSize, Graphics2D.Font);

            lines = FormatedText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public string FormatedText { get; private set; }

    public Color Color { get; set; }

    public bool Centered { get; set; }

    public float FontSize { get; set; } = 13f / 23;

    public int RealHeight => (int)(Graphics2D.Font.MeasureString(FormatedText).Y * FontSize);

    public bool Shadowed { get; set; }

    private Color ShadowColor { get; set; } = Color.Black;

    public TextField(string text, float fontSize, Color color, Rectangle area)
        : base(area)
    {
        Color = color;
        FontSize = fontSize;
        EffectiveRegion = Rectangle.Empty;
        Text = text;   // Note this upper case Text
    }

    public TextField(string text, float fontSize, Color color, Rectangle area, Color shadowColor)
        : base(area)
    {
        Shadowed = true;
        ShadowColor = shadowColor;
        Color = color;
        FontSize = fontSize;
        Text = text;
        EffectiveRegion = Rectangle.Empty;
    }

    public override EventResult HandleEvent(EventType type, object sender, object tag)
    {
        return EventResult.Unhandled;
    }

    private string[] lines;

    public override Rectangle DestinationRectangle
    {
        get
        {
            if (IsDirty && text != null)
            {
                FormatedText = Graphics2D.FormatString(text,
                                base.DestinationRectangle.Width,
                                base.DestinationRectangle.Height, FontSize,
                                Graphics2D.Font);
            }

            return base.DestinationRectangle;
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch sprite)
    {
        if (text == null)
        {
            return;
        }

        _ = DestinationRectangle.Width;

        Vector2 size = Graphics2D.Font.MeasureString(FormatedText) * FontSize;
        var heightOffset = Centered
            ? (DestinationRectangle.Height - size.Y) / 2 + DestinationRectangle.Top
            : DestinationRectangle.Top;
        foreach (var line in lines)
        {
            size = Graphics2D.Font.MeasureString(line) * FontSize;

            var position = Centered
                ? new Vector2(DestinationRectangle.Left + (DestinationRectangle.Width - size.X) / 2, heightOffset)
                : new Vector2(DestinationRectangle.Left, heightOffset);
            Graphics2D.Font.DrawString(sprite, line, position, Color, FontSize);
            if (Shadowed)
            {
                Graphics2D.Font.DrawString(sprite, line, position + Vector2.One, ShadowColor, FontSize);
            }

            heightOffset += size.Y;
        }

        base.Draw(gameTime, sprite);
    }
}

public class TextBox : TextField
{
    public int MaxCharactors { get; set; } = 20;

    public TextBox(float fontSize, Color color, Rectangle area)
        : base("", fontSize, color, area)
    {
    }

    private bool flash;
    private double flashElapsedTime;

    public override void Draw(GameTime gameTime, SpriteBatch sprite)
    {
        flashElapsedTime += gameTime.ElapsedGameTime.TotalSeconds;
        if (flashElapsedTime >= 0.5)
        {
            flashElapsedTime = 0;
            flash = !flash;
        }

        if (flash && Text.Length < MaxCharactors)
        {
            Text += "_";
            base.Draw(gameTime, sprite);
            Text = Text.Remove(Text.Length - 1);
        }
        else
        {
            base.Draw(gameTime, sprite);
        }
    }

    public override EventResult HandleEvent(EventType type, object sender, object tag)
    {
        if (type == EventType.KeyDown && tag is Keys? && sender is Input)
        {
            var input = sender as Input;
            Keys key = (tag as Keys?).Value;

            // Delete a charactor
            if (key == Keys.Back && Text.Length > 0)
            {
                Text = Text.Remove(Text.Length - 1);
                return EventResult.Handled;
            }

            // New charactor
            var upperCase = input.Keyboard.IsKeyDown(Keys.CapsLock);// input.IsShiftPressed;

            var inputChar = Input.KeyToChar(key, upperCase);

            if (Text.Length < MaxCharactors &&
               (inputChar != ' ' || (inputChar == ' ' && key == Keys.Space)))
            {
                Text += inputChar;
            }

            return EventResult.Handled;
        }

        return EventResult.Unhandled;
    }
}
