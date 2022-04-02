// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public enum MessageType
{
    None,
    Warning,
    Hint,
    Unavailable,
}

public enum MessageStyle
{
    None,
    BubbleUp,
    FlyAway,
}

public interface ISelectable : IEventListener
{
    bool Highlighted { get; set; }

    bool Selected { get; set; }
}

public class GameUI : IEventListener
{
    public const int ProfileBaseX = 440;
    public const int ProfileWidth = 40;
    public const int ProfileSpace = 4;
    private readonly BaseGame game;
    private readonly UIElement[,] elements = new UIElement[3, 5];
    private readonly GameWorld world;

    public UIDisplay Display { get; private set; }

    private readonly List<SpellButton> profileButtons = new();
    private int profileNextX = ProfileBaseX;

    public static GameUI Singleton { get; private set; }

    private UI.Panel snapShot;
    private UI.Panel profilePanel;

    public UI.Panel ControlPanel { get; private set; }
    public UI.Panel ResourcePanel { get; private set; }
    public UI.Panel TipBoxContainer { get; private set; }

    public MiniMap Minimap { get; private set; }

    private Texture2D panelsTexture;

    /// <summary>
    /// Screen border fadeout texture.
    /// </summary>
    private Texture2D borderFadeout;

    private TextField snapShotName;
    private readonly Texture2D[] focusAnimation = new Texture2D[8];
    private Vector3 focusPosition;
    private Color focusColor = Color.Green;
    private double focusElapsedTime;
    private TextField lumberTextField;
    private TextField goldTextField;
    private TextField foodTextField;
    private readonly Rectangle ResourcePanelSourceRectangle = new(0, 0, 885, 70);
    private readonly Rectangle ControlPanelSourceRectangle = new(0, 70, 1718, 622);
    private readonly Rectangle GoldMineSourceRectangle = new(885, 0, 41, 41);
    private readonly Rectangle MapSourceRectangle = new(0, 692, 429, 429);
    private readonly Rectangle ResourcePanelDestinationRectangle = new(526, 1, 284, 23);
    private readonly Rectangle ControlPanelDestinationRectangle = new(0, 442, 442, 160);
    private readonly Rectangle ControlPanelEffectiveRegion = new(0, 480, 442, 122);
    private readonly Rectangle MapDestinationRectangle = new(26, 38, 120, 120);
    private readonly Rectangle SnapshotDestinationRectangle = new(314, 44, 85, 85);
    private readonly Rectangle SnapShotNameDestination = new(0, 78, 85, 30);
    private const int SpellButtonBaseX = 145;
    private const int SpellButtonBaseY = 53;
    private const int SpellButtonWidth = 29;
    private const int SpellButtonShorterHeight = 22;
    private const int SpellButtonFullHeight = 34;
    private const int SpellButtonWidthSpace = 1;

    public GameUI(BaseGame game, GameWorld world)
    {
        // Setup singleton
        Singleton = this;

        this.game = game;
        this.world = world;

        LoadContent();
    }

    public void LoadContent()
    {
        // Create a new UI display
        Display = new UIDisplay(BaseGame.Singleton);

        // Load UI textures
        borderFadeout = game.TextureLoader.LoadTexture("data/ui/Fadeout.png");
        panelsTexture = game.TextureLoader.LoadTexture("data/ui/Panels.png");

        for (var i = 0; i < focusAnimation.Length; i++)
        {
            focusAnimation[i] = game.TextureLoader.LoadTexture($"data/ui/Focus/{i + 1}.png");
        }

        Rectangle relativeRect = UIElement.GetRelativeRectangle(new Rectangle
                                        ((int)StartLineForMessage.X, (int)StartLineForMessage.Y, 0, 0),
                                        Display, ScaleMode.ScaleX, Anchor.TopLeft);
        TipBoxContainer = new UI.Panel(Display.Area)
        {
            ScaleMode = ScaleMode.Stretch,
            EffectiveRegion = Rectangle.Empty,
        };

        profilePanel = new UI.Panel(Display.Area)
        {
            ScaleMode = ScaleMode.Stretch,
            EffectiveRegion = Rectangle.Empty,
        };

        RelativeMessageStartLine.X = relativeRect.X;
        RelativeMessageStartLine.Y = relativeRect.Y;
        RelativeMessageStep.Y = StepForOneMessage.Y * Display.Area.Height / Display.DestinationRectangle.Height;
        RelativeMessageStep.X = 0;

        ControlPanel = new UI.Panel(ControlPanelDestinationRectangle)
        {
            Anchor = Anchor.BottomLeft,
            SourceRectangle = ControlPanelSourceRectangle,
            Texture = panelsTexture,
            EffectiveRegion = ControlPanelEffectiveRegion,
        };

        snapShot = new UI.Panel(SnapshotDestinationRectangle)
        {
            Anchor = Anchor.BottomLeft,
            EffectiveRegion = Rectangle.Empty,
        };

        snapShotName = new TextField("Name", 17f / 23, Color.Gold, SnapShotNameDestination, Color.Black)
        {
            Centered = true,
        };

        snapShot.Add(snapShotName);

        ControlPanel.Add(snapShot);

        ResourcePanel = new UI.Panel(ResourcePanelDestinationRectangle)
        {
            Anchor = Anchor.TopRight,
            SourceRectangle = ResourcePanelSourceRectangle,
            Texture = panelsTexture,
        };

        Color color = Color.White;
        ResourcePanel.Add(lumberTextField = new TextField(Player.LocalPlayer.Lumber.ToString(),
                            17f / 23, color, new Rectangle(72, 2, 150, 20)));
        ResourcePanel.Add(goldTextField = new TextField(Player.LocalPlayer.Gold.ToString(),
                            17f / 23, color, new Rectangle(128, 2, 150, 20)));
        if (Player.LocalPlayer.Food > Player.LocalPlayer.FoodCapacity)
        {
            color = Color.Red;
        }

        ResourcePanel.Add(foodTextField = new TextField(Player.LocalPlayer.Food.ToString()
                            + "/" + Player.LocalPlayer.FoodCapacity,
                            17f / 23, color, new Rectangle(184, 2, 150, 20)));

        Minimap = new MiniMap(game, world)
        {
            Texture = panelsTexture,
            SourceRectangle = MapSourceRectangle,
            GoldMinePointerTexture = panelsTexture,
            GoldMineSourceRectangle = GoldMineSourceRectangle,
            Area = MapDestinationRectangle,
            ScaleMode = ScaleMode.ScaleY,
            Anchor = Anchor.BottomRight,
        };

        Display.Add(profilePanel);
        Display.Add(ControlPanel);
        Display.Add(ResourcePanel);
        ControlPanel.Add(Minimap);
        Display.Add(TipBoxContainer);

        // Init rendering of fog of war
        fogOfWarVertices = new VertexPositionTexture[4]
        {
                new VertexPositionTexture(Vector3.Zero, new Vector2(0, 0)),
                new VertexPositionTexture(Vector3.Zero, new Vector2(0, 1)),
                new VertexPositionTexture(Vector3.Zero, new Vector2(1, 0)),
                new VertexPositionTexture(Vector3.Zero, new Vector2(1, 1)),
        };
    }

    /// <summary>
    /// Adds a new spell to the scroll panel.
    /// </summary>
    public void SetUIElement(int x, bool specialItem, UIElement element)
    {
        if (!specialItem && (x < 0 || x > 4))
        {
            return;
        }

        if (specialItem && (x < 0 || x > 9))
        {
            return;
        }

        var y = 0;
        if (specialItem)
        {
            y = x / 5 + 1;
            x %= 5;
        }

        if (elements[y, x] != null)
        {
            ControlPanel.Remove(elements[y, x]);
        }

        // Remove old one
        if (element != null)
        {
            var area = new Rectangle(SpellButtonBaseX + x * (SpellButtonWidth + SpellButtonWidthSpace),
                                            SpellButtonBaseY + y * SpellButtonFullHeight,
                                            SpellButtonWidth, SpellButtonShorterHeight);
            if (y > 0)
            {
                area.Height = SpellButtonWidth;
            }

            if (y == 1)
            {
                area.Y--;
            }

            // Adjust element position
            element.Area = area;

            // Adjust element anchor and scale mode
            element.Anchor = Anchor.BottomRight;
            element.ScaleMode = ScaleMode.ScaleX;

            // Add it to the ui
            ControlPanel.Add(element);
        }

        TipBoxContainer.Clear();

        // Store it in the array
        elements[y, x] = element;
    }

    public void ClearUIElement()
    {
        foreach (UIElement e in elements)
        {
            if (e != null)
            {
                ControlPanel.Remove(e);
            }
        }

        TipBoxContainer.Clear();
    }

    /// <summary>
    /// Present Messages in the message queue.
    /// </summary>
    /// <param name="gameTime"></param>
    private void PresentSideBarMessages(GameTime gameTime)
    {
        while (messageQueue.Count != 0 && messageQueue.Peek().PushTime
            + MessagePresentingPeriodLength + MessageDisappearingPeriodLength < gameTime.TotalGameTime.TotalSeconds)
        {
            messageQueue.Dequeue();
        }

        var count = messageQueue.Count;
        double remainingTime;
        Vector2 actualBaseLine = RelativeMessageStartLine;
        remainingTime = lastPushTime + PushingPeriodLength - gameTime.TotalGameTime.TotalSeconds;
        if (remainingTime > 0)
        {
            actualBaseLine -= (float)(remainingTime / PushingPeriodLength) * RelativeMessageStep;
        }

        if (messageQueue.Count == 1)
        {
            GameMessage message = messageQueue.Peek();
            if (gameTime.TotalGameTime.TotalSeconds < lastPushTime + MessageEmergingPeriodLength)
            {
                var transparentColor = new Color(message.Color.R, message.Color.G, message.Color.B,
                                (byte)(message.Color.A * ((gameTime.TotalGameTime.TotalSeconds - lastPushTime) / MessageEmergingPeriodLength)));
                var transparentShadowColor = new Color(0, 0, 0,
                    (byte)(message.Color.A * ((gameTime.TotalGameTime.TotalSeconds - lastPushTime) / MessageEmergingPeriodLength)));
                game.Graphics2D.DrawShadowedString(message.Message, MessageFontSize, RelativeMessageStartLine, transparentColor, transparentShadowColor);
            }
            else if (gameTime.TotalGameTime.TotalSeconds < message.PushTime + MessageEmergingPeriodLength + MessagePresentingPeriodLength)
            {
                game.Graphics2D.DrawShadowedString(message.Message, MessageFontSize, RelativeMessageStartLine, message.Color, Color.Black);
            }
            else if (gameTime.TotalGameTime.TotalSeconds < message.PushTime + MessageEmergingPeriodLength + MessagePresentingPeriodLength + MessageDisappearingPeriodLength)
            {
                remainingTime = message.PushTime + MessagePresentingPeriodLength + MessageEmergingPeriodLength + MessageDisappearingPeriodLength - gameTime.TotalGameTime.TotalSeconds;
                var transparentColor = new Color(message.Color.R,
                                message.Color.G, message.Color.B, (byte)(message.Color.A * (1 + remainingTime / MessageDisappearingPeriodLength)));
                var transparentShadowColor = new Color(0, 0, 0,
                    (byte)(message.Color.A * (1 + remainingTime / MessageDisappearingPeriodLength)));
                game.Graphics2D.DrawShadowedString(message.Message, MessageFontSize, RelativeMessageStartLine, transparentColor, transparentShadowColor);
            }

            return;
        }

        foreach (GameMessage message in messageQueue)
        {
            // Length of time PeriodLength remain for this message to be presented
            remainingTime = message.PushTime + MessagePresentingPeriodLength - gameTime.TotalGameTime.TotalSeconds;

            // Last line
            if (count == 1)
            {
                var pushFinishedTime = lastPushTime + PushingPeriodLength;
                // Not stable
                if (gameTime.TotalGameTime.TotalSeconds < pushFinishedTime + MessageEmergingPeriodLength)
                {
                    // Emerging
                    if (gameTime.TotalGameTime.TotalSeconds > pushFinishedTime)
                    {
                        var transparentColor = new Color(message.Color.R, message.Color.G, message.Color.B,
                            (byte)(message.Color.A * ((gameTime.TotalGameTime.TotalSeconds - pushFinishedTime) / MessageEmergingPeriodLength)));
                        var transparentShadowColor = new Color(0, 0, 0,
                            (byte)(message.Color.A * ((gameTime.TotalGameTime.TotalSeconds - pushFinishedTime) / MessageEmergingPeriodLength)));
                        game.Graphics2D.DrawShadowedString(message.Message, MessageFontSize, RelativeMessageStartLine + RelativeMessageStep * (count - 1), transparentColor, transparentShadowColor);
                    }

                    return;
                }
            }

            if (remainingTime < 0)
            {
                if (-remainingTime < MessageDisappearingPeriodLength)
                {
                    var transparentColor = new Color(message.Color.R,
                                message.Color.G, message.Color.B, (byte)(message.Color.A * (1 + remainingTime / MessageDisappearingPeriodLength)));
                    var transparentShadowColor = new Color(0, 0, 0,
                        (byte)(message.Color.A * (1 + remainingTime / MessageDisappearingPeriodLength)));
                    game.Graphics2D.DrawShadowedString(message.Message, MessageFontSize, actualBaseLine + RelativeMessageStep * (count - 1), transparentColor, transparentShadowColor);
                }
            }
            else
            {
                game.Graphics2D.DrawShadowedString(message.Message, MessageFontSize, actualBaseLine + RelativeMessageStep * (count - 1), message.Color, Color.Black);
            }

            count--;
        }
    }

    // Message Queues
    private readonly Queue<GameMessage> BubbleUpMessageQueue = new();
    private readonly Queue<GameMessage> FlyAwayMessageQueue = new();
    private readonly Queue<GameMessage> NoneStyleMessageQueue = new();
    private readonly Queue<GameMessage> messageQueue = new();

    // Constants for wherever messages
    private const double BubbleUpSustainingPeriodLength = 1.5;
    private const double BubbleUpPeriodLength = 1.5;
    private const int BubbleUpSustainingHeight = 10;
    private const int BubbleUpHeight = 15;
    private const double NoneStyleMessageShowingPeriodLength = 2;

    // Constants for side bar message
    private const double MessagePresentingPeriodLength = 12;
    private const double MessageDisappearingPeriodLength = 5;
    private const double MessageEmergingPeriodLength = 0.2;
    private const double PushingPeriodLength = 0.3;

    // Side bar message line-height controls
    private static readonly Vector2 StartLineForMessage = new(30, 350);
    private static readonly Vector2 StepForOneMessage = new(0, -20);
    private Vector2 RelativeMessageStartLine;
    private Vector2 RelativeMessageStep;

    // Message Font Sizes
    private const float BubbleUpMessageFontSize = 15f / 23;
    private const float NoneStyleMessageFontSize = 17f / 23;
    private const float MessageFontSize = 18f / 23;

    // Switcher
    public bool BubbleUpMessageOn = true;
    public bool FlyAwayMessageOn = true;
    public bool NoneStyleMessageOn = true;
    public bool SideBarMessageOn = true;

    // Time when the last push happened
    private double lastPushTime;

    /// <summary>
    ///  Represent a message in the game.
    /// </summary>
    private struct GameMessage
    {
        public string Message;
        public MessageType Type;
        public Color Color;
        public double PushTime;
        public Vector3 Position;

        public GameMessage(string message, MessageType type, Color color, double pushTime)
        {
            Message = message;
            Type = type;
            Color = color;
            PushTime = pushTime;
            Position = new Vector3(0, 0, 0);
        }

        public GameMessage(string message, MessageType type, Color color, double pushTime, Vector3 position)
        {
            Message = message;
            Type = type;
            Color = color;
            PushTime = pushTime;
            Position = position;
        }
    }

    private void PresentWhereverMessages(GameTime gameTime)
    {
        if (BubbleUpMessageOn)
        {
            PresentBubbleUpMessage(gameTime);
        }

        if (FlyAwayMessageOn)
        {
            PresentFlyAwayMessage(gameTime);
        }

        if (NoneStyleMessageOn)
        {
            PresentNoneStyleMessage(gameTime);
        }
    }

    private void PresentBubbleUpMessage(GameTime gameTime)
    {
        while (BubbleUpMessageQueue.Count != 0 &&
            gameTime.TotalGameTime.TotalSeconds > BubbleUpMessageQueue.Peek().PushTime + BubbleUpPeriodLength + BubbleUpSustainingPeriodLength)
        {
            BubbleUpMessageQueue.Dequeue();
        }

        double passedTime;
        Vector2 position;
        foreach (GameMessage message in BubbleUpMessageQueue)
        {
            passedTime = gameTime.TotalGameTime.TotalSeconds - message.PushTime;

            // Bubbling up and disappearing
            if (passedTime > BubbleUpSustainingPeriodLength)
            {
                passedTime -= BubbleUpSustainingPeriodLength;
                position = new Vector2(game.Project(message.Position).X, game.Project(message.Position).Y) -
                            new Vector2(0, (float)(BubbleUpSustainingHeight + BubbleUpHeight * (passedTime / BubbleUpPeriodLength)));
                var textColor = new Color(message.Color.R, message.Color.G, message.Color.B,
                                (byte)(255 * (1 - passedTime / BubbleUpPeriodLength)));
                var shadowColor = new Color(0, 0, 0,
                                (byte)(255 * (1 - passedTime / BubbleUpPeriodLength)));
                game.Graphics2D.DrawShadowedString(message.Message, BubbleUpMessageFontSize, position, textColor, shadowColor);
            }

            // Bubbling up and remaining stable
            else
            {
                position = new Vector2(game.Project(message.Position).X, game.Project(message.Position).Y) -
                            new Vector2(0, (float)(BubbleUpSustainingHeight * (passedTime / BubbleUpSustainingPeriodLength)));
                game.Graphics2D.DrawShadowedString(message.Message, BubbleUpMessageFontSize, position, message.Color, Color.Black);
            }
        }
    }

    private void PresentFlyAwayMessage(GameTime gameTime)
    {
        // ! Time not finished
        while (FlyAwayMessageQueue.Count != 0 && gameTime.TotalGameTime.TotalSeconds > FlyAwayMessageQueue.Peek().PushTime)
        {
            FlyAwayMessageQueue.Dequeue();
        }
    }

    private void PresentNoneStyleMessage(GameTime gameTime)
    {
        while (NoneStyleMessageQueue.Count != 0 && gameTime.TotalGameTime.TotalSeconds > NoneStyleMessageQueue.Peek().PushTime + NoneStyleMessageShowingPeriodLength)
        {
            NoneStyleMessageQueue.Dequeue();
        }

        foreach (GameMessage message in NoneStyleMessageQueue)
        {
            game.Graphics2D.DrawShadowedString(message.Message, NoneStyleMessageFontSize, new Vector2(game.Project(message.Position).X, game.Project(message.Position).Y), message.Color, Color.Black);
        }
    }

    private void PresentMessages(GameTime gameTime)
    {
        if (SideBarMessageOn)
        {
            PresentSideBarMessages(gameTime);
        }

        PresentWhereverMessages(gameTime);
    }

    public void ShowMessage(string message, Vector3 position, MessageType type,
                                                              MessageStyle style,
                                                              Color color)
    {
        switch (style)
        {
            case MessageStyle.BubbleUp:
                BubbleUpMessageQueue.Enqueue(new GameMessage
                    (message, type, color, game.CurrentGameTime.TotalGameTime.TotalSeconds, position));
                break;
            case MessageStyle.FlyAway:
                FlyAwayMessageQueue.Enqueue(new GameMessage
                    (message, type, color, game.CurrentGameTime.TotalGameTime.TotalSeconds, position));
                break;
            case MessageStyle.None:
                NoneStyleMessageQueue.Enqueue(new GameMessage
                    (message, type, color, game.CurrentGameTime.TotalGameTime.TotalSeconds, position));
                break;
        }
    }

    public void PushMessage(string message, MessageType type, Color color)
    {
        lastPushTime = BaseGame.Singleton.CurrentGameTime.TotalGameTime.TotalSeconds;
        messageQueue.Enqueue(new GameMessage(message, type, color, lastPushTime));
    }

    public void SetCursorFocus(Vector3 position, Color color)
    {
        focusElapsedTime = 0;
        focusPosition = position;
        focusColor = color;
    }

    // The size of the plain progress bar in pixel.
    // Used to show blood and magic of an Entity.
    private readonly Point progressFullSize = new(80, 5);

    /// <summary>
    /// Draw a progress bar for either style.
    /// </summary>
    /// <param name="style"></param>
    /// <param name="percentage">A float point number show the percentage between 0 and 100.</param>
    /// <param name="color">The color of the rectangle.</param>
    public void DrawProgress(Vector3 position, int yOffset, int length, float percentage, Color color)
    {
        Point position2D = game.Project(position);

        var fullRect = new Rectangle(position2D.X - 1, position2D.Y - 1 - yOffset,
                                          length + 2, progressFullSize.Y + 2);

        var percentagedRect = new Rectangle(position2D.X, position2D.Y - yOffset,
                                        (int)(length * percentage / 100), progressFullSize.Y);

        fullRect.X -= length / 2;
        percentagedRect.X -= length / 2;

        game.Graphics2D.DrawRectangle(fullRect, Color.Black);
        game.Graphics2D.DrawRectangle(percentagedRect, color);
    }

    public void Update(GameTime gameTime)
    {
        LocalPlayer player = Player.LocalPlayer;
        if (player.SelectionDirty)
        {
            player.SelectionDirty = false;
            ClearProfile();
            for (var i = 0; i < player.Groups.Count; i++)
            {
                List<GameObject> list = player.Groups[i];

                if (list.Count > 0 && list[0].ProfileButton != null)
                {
                    list[0].ProfileButton.Count = list.Count;
                    AddProfile(list[0].ProfileButton, player.CurrentGroupIndex == i);
                }
            }

            if (player.CurrentGroup != null && player.CurrentGroup.Count > 0)
            {
                snapShotName.Color = player.CurrentGroup[0].Owner == null ? Color.White : player.CurrentGroup[0].Owner is LocalPlayer ? Color.Yellow : Color.Red;

                snapShotName.Text = player.CurrentGroup[0].Name;
            }
            else
            {
                snapShotName.Text = null;
            }

            // Draw snap shot
            if (Player.LocalPlayer.CurrentGroup != null &&
                Player.LocalPlayer.CurrentGroup.Count > 0)
            {
                Icon snapshot = Player.LocalPlayer.CurrentGroup[0].SnapshotIcon;
                snapShot.Texture = snapshot.Texture;
                snapShot.SourceRectangle = snapshot.Region;
            }
            else
            {
                snapShot.Texture = null;
            }
        }

        Display.Update(gameTime);

        Player currentPlayer = Player.LocalPlayer;

        lumberTextField.Text = currentPlayer.Lumber.ToString();
        goldTextField.Text = currentPlayer.Gold.ToString();
        Color color = Color.White;
        if (currentPlayer.Food > currentPlayer.FoodCapacity)
        {
            color = Color.Red;
        }

        foodTextField.Text = currentPlayer.Food.ToString() + "/" + currentPlayer.FoodCapacity.ToString();
        foodTextField.Color = color;
    }

    public void Draw(GameTime gameTime)
    {
        PresentMessages(gameTime);
        game.Graphics2D.Present();

        // Draw screen border fadeout
        Display.Sprite.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
        Display.Sprite.Draw(borderFadeout,
            new Rectangle(0, 0, game.ScreenWidth, game.ScreenHeight), Color.White);

        Display.Sprite.End();

        Display.Draw(gameTime);

        DrawFogOfWar();

        // Draw statistics
        Display.Sprite.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);

        DrawCursorFocus(gameTime, Display.Sprite);

        Display.Sprite.End();

        game.Graphics2D.Present();
    }

    private void DrawCursorFocus(GameTime gameTime, SpriteBatch sprite)
    {
        const double FocusDuration = 0.2f;
        const int FocusSize = 128;

        if (focusElapsedTime < FocusDuration)
        {
            var frame = (int)(focusAnimation.Length * focusElapsedTime / FocusDuration);

            if (frame < focusAnimation.Length)
            {
                Rectangle destination;
                Point p = game.Project(focusPosition);
                destination.Width = FocusSize;
                destination.Height = FocusSize;
                destination.X = p.X - FocusSize / 2;
                destination.Y = p.Y - FocusSize / 2;

                sprite.Draw(focusAnimation[frame], destination, focusColor);

                focusElapsedTime += gameTime.ElapsedGameTime.TotalSeconds;
            }
        }
    }

    private VertexPositionTexture[] fogOfWarVertices;

    private void DrawFogOfWar()
    {
        if (world.FogOfWar.Mask != null)
        {
            // Setup vertices
            Rectangle destination = Minimap.ActualArea;

            fogOfWarVertices[0].Position = game.Graphics2D.ScreenToEffect(destination.Left, destination.Bottom);
            fogOfWarVertices[1].Position = game.Graphics2D.ScreenToEffect(destination.Left, destination.Top);
            fogOfWarVertices[2].Position = game.Graphics2D.ScreenToEffect(destination.Right, destination.Bottom);
            fogOfWarVertices[3].Position = game.Graphics2D.ScreenToEffect(destination.Right, destination.Top);

            // Draw
            Effect effect = game.Graphics2D.Effect;

            effect.CurrentTechnique = effect.Techniques["FogOfWar"];
            effect.Parameters["BasicTexture"].SetValue(world.FogOfWar.Mask);

            effect.CurrentTechnique.Passes[0].Apply();

            game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, fogOfWarVertices, 0, 2);

            // This will be drawed next frame...
            world.Landscape.FogTexture = world.FogOfWar.Mask;
        }
    }

    public void AddProfile(SpellButton button, bool enlarge)
    {
        var size = enlarge ? (int)(ProfileWidth * 1.2f) : ProfileWidth;
        button.Area = new Rectangle(profileNextX, 590 - size, size, size);
        button.Anchor = Anchor.BottomLeft;
        button.ScaleMode = ScaleMode.ScaleY;
        profilePanel.Add(button);
        profileButtons.Add(button);
        profileNextX += ProfileSpace + size;
        button.Click += (o, e) => Player.LocalPlayer.SelectionDirty = true;
    }

    public void ClearProfile()
    {
        foreach (SpellButton button in profileButtons)
        {
            profilePanel.Remove(button);
        }

        profileButtons.Clear();
        profileNextX = ProfileBaseX;
    }

    /// <summary>
    /// Gets whether the UI overlaps the specified point on the screen.
    /// </summary>
    public bool Overlaps(Point p)
    {
        return ControlPanel.ActualEffectiveRegion.Contains(p) || ResourcePanel.ActualEffectiveRegion.Contains(p);
    }

    public EventResult HandleEvent(EventType type, object sender, object tag)
    {
        return Display != null &&
            Display.HandleEvent(type, sender, tag) == EventResult.Handled
            ? EventResult.Handled
            : EventResult.Unhandled;
    }
}
