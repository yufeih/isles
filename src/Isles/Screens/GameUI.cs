//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.UI;
namespace Isles
{
    #region Enums
    public enum MessageType
    {
        None, Warning, Hint, Unavailable
    }

    public enum MessageStyle
    {
        None, BubbleUp, FlyAway,
    }

    public enum ProgressStyle
    {
        Untextured, Textured
    }
    #endregion

    #region ISelectable
    public interface ISelectable : IEventListener
    {
        bool Highlighted { get; set; }
        bool Selected { get; set;}
    }
    #endregion

    #region GameUI
    /// <summary>
    /// This is the in-game UI
    /// </summary>
    /// <remarks>
    /// Guess we are about to implement a configurable game UI,
    /// maybe XML based or whatever. For now just hardcode most
    /// of our game UI.
    /// </remarks>
    public class GameUI : IEventListener
    {
        #region Fields
        public const int ButtonWidth = 80;
        public const int ButtonHeight = 40;
        public const int ScrollButtonWidth = 40;
        public const int IconTextureRowCount = 8;
        public const int IconTextureColumnCount = 4;
        public const int ProfileBaseX = 440;
        public const int ProfileWidth = 40;
        public const int ProfileSpace = 4;

        BaseGame        game;
        UIDisplay       ui;
        IUIElement[,]   elements = new IUIElement[3,5];
        GameWorld       world;

        public UIDisplay Display
        {
            get { return ui; }
        }

        List<SpellButton> profileButtons = new List<SpellButton>();
        int profileNextX = ProfileBaseX;
        
        #region Singleton
        public static GameUI Singleton
        {
            get { return singleton; }
        }

        static GameUI singleton;
        #endregion

        #region Panels

        Isles.UI.Panel controlPanel, snapShot, tipBoxContainer, profilePanel;

        /// <summary>
        /// Gets the control panel in the game
        /// </summary>
        public Isles.UI.Panel ControlPanel
        {
            get { return controlPanel; }
        }


        Isles.UI.Panel resourcePanel;

        /// <summary>
        /// Gets the resource panel
        /// </summary>
        public Isles.UI.Panel ResourcePanel
        {
            get { return resourcePanel; }
        }

        /// <summary>
        /// Gets the tip box container
        /// </summary>
        public Isles.UI.Panel TipBoxContainer
        {
            get { return tipBoxContainer; } 
        }

        MiniMap minimap;

        public MiniMap Minimap
        {
            get { return minimap; }
        }


        #endregion
        
        Texture2D dialogTexture, panelsTexture;

        /// <summary>
        /// Screen border fadeout texture
        /// </summary>
        Texture2D borderFadeout;

        /// <summary>
        /// Distortion pic
        /// </summary>
        Texture2D distortion;

        /// <summary>
        /// For the disappearing effect
        /// </summary>
        Texture2D LoadingDisplayFinished;


        TextField snapShotName;


        Texture2D[] focusAnimation = new Texture2D[8];
        Vector3 focusPosition;
        Color focusColor = Color.Green;
        double focusElapsedTime = 0;

        TextField lumberTextField, goldTextField, foodTextField;

        readonly float[] StatisticsTextX = {4.6f / 24.25f, 11.6f / 24.25f, 18.3f / 24.25f};

        readonly Rectangle StatisticsDestination = new Rectangle(400, 2, 400, 36);
        readonly Rectangle StatisticsSource = new Rectangle(0, 0, 690, 64);
        readonly Rectangle StatusDestination = new Rectangle(5, 495, 150, 120);
        readonly Rectangle StatusSource = new Rectangle(0, 62, 256, 256);
        readonly Rectangle SignSource = new Rectangle(246, 80, 236, 226);
        readonly Rectangle StatusRectangle = new Rectangle(5, 400, 400, 100);


        readonly Rectangle ResourcePanelSourceRectangle = new Rectangle(0, 0, 885, 70);
        readonly Rectangle ControlPanelSourceRectangle = new Rectangle(0, 70, 1718, 622);
        readonly Rectangle GoldMineSourceRectangle = new Rectangle(885, 0, 41, 41);
        readonly Rectangle MapSourceRectangle = new Rectangle(0, 692, 429, 429);


        readonly Rectangle ResourcePanelDestinationRectangle = new Rectangle(526, 1, 284, 23);
        readonly Rectangle ControlPanelDestinationRectangle = new Rectangle(0, 442, 442, 160);
        readonly Rectangle ControlPanelEffectiveRegion = new Rectangle(0, 480, 442, 122);
        readonly Rectangle MapDestinationRectangle = new Rectangle(26, 38, 120, 120);
        readonly Rectangle SnapshotDestinationRectangle = new Rectangle(314, 44, 85, 85);
        readonly Rectangle EnvironmentIndicatorSource= new Rectangle(960, 0, 46, 46);
        readonly Rectangle EnvironmentIndicatorDestination = new Rectangle(760, 6, 12, 12);
        readonly Rectangle SnapShotNameDestination = new Rectangle(0, 78, 85, 30);

        const int SpellButtonBaseX = 145;
        const int SpellButtonBaseY = 53;
        const int SpellButtonWidth = 29;
        const int SpellButtonShorterHeight = 22;
        const int SpellButtonFullHeight = 34;
        const int SpellButtonWidthSpace = 1;


        readonly Rectangle[] ElementAreas = new Rectangle[]
        {
            new Rectangle(664, 528, 64, 32), new Rectangle(732, 528, 64, 32),
            new Rectangle(664, 564, 64, 32), new Rectangle(732, 564, 64, 32),
        };

        readonly Color TextColor = Color.White;
        readonly Color TextColorDark = Color.Black;


        const double DisappearingTime = 3;
        double startTime = 0;

        Effect disappearEffect;

        #endregion

        #region Methods
        /// <summary>
        /// Creates a new game user interface
        /// </summary>
        public GameUI(BaseGame game, Texture2D loadingFinished, GameWorld world)
        {
            // Setup singleton
            singleton = this;

            this.game = game;
            this.world = world;
            LoadingDisplayFinished = loadingFinished;

            LoadContent();
        }        

        public void LoadContent()
        {
            // Create a new UI display
            this.ui = new UIDisplay(BaseGame.Singleton);

            // Load UI textures
            borderFadeout = game.ZipContent.Load<Texture2D>("Textures/Fadeout");
            disappearEffect = game.ZipContent.Load<Effect>("Effects/DisortionDisappear");
            distortion = game.ZipContent.Load<Texture2D>("Textures/Distortion");
            panelsTexture = game.ZipContent.Load<Texture2D>("UI/Panels");
            dialogTexture = game.ZipContent.Load<Texture2D>("UI/Tipbox");

             
            for (int i = 0; i < focusAnimation.Length; i++)
                focusAnimation[i] = game.ZipContent.Load<Texture2D>("UI/Focus/" + (i + 1));
            Rectangle relativeRect = UIElement.GetRelativeRectangle(new Rectangle
                                            ((int)(StartLineForMessage.X), (int)(StartLineForMessage.Y), 0, 0),
                                            ui, ScaleMode.ScaleX, Anchor.TopLeft);
            tipBoxContainer = new Isles.UI.Panel(ui.Area);
            tipBoxContainer.ScaleMode = ScaleMode.Stretch;
            tipBoxContainer.EffectiveRegion = Rectangle.Empty;

            profilePanel = new Isles.UI.Panel(ui.Area);
            profilePanel.ScaleMode = ScaleMode.Stretch;
            profilePanel.EffectiveRegion = Rectangle.Empty;

            RelativeMessageStartLine.X = relativeRect.X;
            RelativeMessageStartLine.Y = relativeRect.Y;
            RelativeMessageStep.Y = StepForOneMessage.Y * ui.Area.Height / ui.DestinationRectangle.Height;
            RelativeMessageStep.X = 0;

            controlPanel = new Isles.UI.Panel(ControlPanelDestinationRectangle);
            controlPanel.Anchor = Anchor.BottomLeft;
            controlPanel.SourceRectangle = ControlPanelSourceRectangle;
            controlPanel.Texture = panelsTexture;
            controlPanel.EffectiveRegion = ControlPanelEffectiveRegion;

            snapShot = new Isles.UI.Panel(SnapshotDestinationRectangle);
            snapShot.Anchor = Anchor.BottomLeft;
            snapShot.EffectiveRegion = Rectangle.Empty;


            snapShotName = new TextField("Name", 17f / 23, Color.Gold, SnapShotNameDestination, Color.Black);//NameDestination);
            snapShotName.Centered = true;

            snapShot.Add(snapShotName);

            controlPanel.Add(snapShot);

            resourcePanel = new Isles.UI.Panel(ResourcePanelDestinationRectangle);
            resourcePanel.Anchor = Anchor.TopRight;
            resourcePanel.SourceRectangle = ResourcePanelSourceRectangle;
            resourcePanel.Texture = panelsTexture;

            Color color = Color.White;
            resourcePanel.Add(  lumberTextField = new TextField(Player.LocalPlayer.Lumber.ToString(), 
                                17f/23, color, new Rectangle(72, 2, 150, 20)));
            resourcePanel.Add(  goldTextField = new TextField(Player.LocalPlayer.Gold.ToString(),
                                17f / 23, color, new Rectangle(128, 2, 150, 20)));
            if (Player.LocalPlayer.Food > Player.LocalPlayer.FoodCapacity)
                color = Color.Red;
            resourcePanel.Add(  foodTextField = new TextField(Player.LocalPlayer.Food.ToString() 
                                + "/" + Player.LocalPlayer.FoodCapacity,
                                17f / 23, color, new Rectangle(184, 2, 150, 20)));


            minimap = new MiniMap(game, world);
            minimap.Texture = panelsTexture;
            minimap.SourceRectangle = MapSourceRectangle;
            minimap.GoldMinePointerTexture = panelsTexture;
            minimap.GoldMineSourceRectangle = GoldMineSourceRectangle;
            minimap.Area = MapDestinationRectangle;
            minimap.ScaleMode = ScaleMode.ScaleY;
            minimap.Anchor = Anchor.BottomRight;

            ui.Add(profilePanel);
            ui.Add(controlPanel);
            ui.Add(resourcePanel);
            controlPanel.Add(minimap);
            ui.Add(tipBoxContainer);
            randomOffset = Vector2.Normalize(new Vector2((float)rand.NextDouble(), (float)rand.NextDouble()));

            // Init rendering of fog of war
            fogOfWarDeclaration = new VertexDeclaration(game.GraphicsDevice, VertexPositionTexture.VertexElements);
            fogOfWarVertices = new VertexPositionTexture[4]
            {
                new VertexPositionTexture(Vector3.Zero, new Vector2(0, 0)),
                new VertexPositionTexture(Vector3.Zero, new Vector2(0, 1)),
                new VertexPositionTexture(Vector3.Zero, new Vector2(1, 1)),
                new VertexPositionTexture(Vector3.Zero, new Vector2(1, 0)),
            };
        }

        /// <summary>
        /// Adds a new spell to the scroll panel
        /// </summary>
        public void SetUIElement(int x, bool specialItem, IUIElement element)
        {
            if (!specialItem && (x < 0 || x > 4) )
                return;
            if (specialItem && (x < 0 || x > 9))
                return;
            int y = 0;
            if (specialItem)
            {
                y = x / 5 + 1;
                x %= 5;
            }
            if (elements[y, x] != null)
                controlPanel.Remove(elements[y, x]);

            // Remove old one
            if (element != null)
            {
                
                Rectangle area = new  Rectangle(SpellButtonBaseX + x * (SpellButtonWidth + SpellButtonWidthSpace), 
                                                SpellButtonBaseY + y * SpellButtonFullHeight,
                                                SpellButtonWidth, SpellButtonShorterHeight);
                if(y > 0)
                    area.Height = SpellButtonWidth;

                if (y == 1)
                    area.Y--;
                // Adjust element position
                element.Area = area;

                // Adjust element anchor and scale mode
                element.Anchor = Anchor.BottomRight;
                element.ScaleMode = ScaleMode.ScaleX;

                // Add it to the ui
                controlPanel.Add(element);
            }


            this.tipBoxContainer.Clear();

            // Store it in the array
            elements[y, x] = element;
        }


        /// <summary>
        /// Remove a UI element 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void RemoveUIElement(int x, int y)
        {
            if (elements[x, y] == null)
                return;
            controlPanel.Remove(elements[x, y]);
        }


        /// <summary>
        /// Clear all ui elements
        /// </summary>
        public void ClearUIElement()
        {
            foreach (IUIElement e in elements)
            {
                if( e != null)
                    controlPanel.Remove(e);
            }
            tipBoxContainer.Clear();

        }



        #region Messages
        /// <summary>
        /// Present Messages in the message queue
        /// </summary>
        /// <param name="gameTime"></param>
        private void PresentSideBarMessages(GameTime gameTime)
        {
            while (messageQueue.Count != 0 && messageQueue.Peek().PushTime
                + MessagePresentingPeriodLength + MessageDisappearingPeriodLength < gameTime.TotalGameTime.TotalSeconds)
            {
                messageQueue.Dequeue();
            }
            int count = messageQueue.Count;
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
                    Color transparentColor = new Color(message.Color.R, message.Color.G, message.Color.B,
                                    (byte)(message.Color.A * ((gameTime.TotalGameTime.TotalSeconds - lastPushTime) / MessageEmergingPeriodLength)));
                    Color transparentShadowColor = new Color(0, 0, 0,
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
                    Color transparentColor = new Color(message.Color.R,
                                    message.Color.G, message.Color.B, (byte)(message.Color.A * (1 + remainingTime / MessageDisappearingPeriodLength)));
                    Color transparentShadowColor = new Color(0, 0, 0,
                        (byte)(message.Color.A * (1 + remainingTime / MessageDisappearingPeriodLength)));
                    game.Graphics2D.DrawShadowedString(message.Message, MessageFontSize, RelativeMessageStartLine, transparentColor, transparentShadowColor);
                }
                return;
            }

            foreach (GameMessage message in messageQueue)
            {
                // Length of time PeriodLength remain for this message to be presented
                remainingTime = (message.PushTime + MessagePresentingPeriodLength) - gameTime.TotalGameTime.TotalSeconds;

                // Last line
                if (count == 1) 
                {
                    double pushFinishedTime = lastPushTime + PushingPeriodLength;
                    // Not stable
                    if (gameTime.TotalGameTime.TotalSeconds < pushFinishedTime + MessageEmergingPeriodLength)
                    {
                        // Emerging
                        if (gameTime.TotalGameTime.TotalSeconds > pushFinishedTime)
                        {
                            Color transparentColor = new Color(message.Color.R, message.Color.G, message.Color.B,
                                (byte)(message.Color.A * ((gameTime.TotalGameTime.TotalSeconds - pushFinishedTime) / MessageEmergingPeriodLength)));
                            Color transparentShadowColor = new Color(0,0,0,
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
                        Color transparentColor = new Color(message.Color.R,
                                    message.Color.G, message.Color.B, (byte)(message.Color.A * (1 + remainingTime / MessageDisappearingPeriodLength)));
                        Color transparentShadowColor = new Color(0, 0, 0,
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

        #region Fields for Game Messages

        // Message Queues
        Queue<GameMessage> BubbleUpMessageQueue = new Queue<GameMessage>();
        Queue<GameMessage> FlyAwayMessageQueue = new Queue<GameMessage>();
        Queue<GameMessage> NoneStyleMessageQueue = new Queue<GameMessage>();
        Queue<GameMessage> messageQueue = new Queue<GameMessage>();

        // Constants for wherever messages
        const double BubbleUpSustainingPeriodLength = 1.5;
        const double BubbleUpPeriodLength = 1.5;
        const int BubbleUpSustainingHeight = 10;
        const int BubbleUpHeight = 15;
        const double NoneStyleMessageShowingPeriodLength = 2;

        // Constants for side bar message
        const double MessagePresentingPeriodLength = 12;
        const double MessageDisappearingPeriodLength = 5;
        const double MessageEmergingPeriodLength = 0.2;
        const double PushingPeriodLength = 0.3;

        // Side bar message line-height controls
        static readonly Vector2 StartLineForMessage = new Vector2(30, 350);
        static readonly Vector2 StepForOneMessage = new Vector2(0, -20);

        Vector2 RelativeMessageStartLine;
        Vector2 RelativeMessageStep;

        // Message Font Sizes
        const float BubbleUpMessageFontSize = 15f / 23;
        const float FlyAwayMessageFontSize = 15f / 23;
        const float NoneStyleMessageFontSize = 17f / 23;
        const float MessageFontSize = 18f / 23;

        // Switcher
        public bool BubbleUpMessageOn = true;
        public bool FlyAwayMessageOn = true;
        public bool NoneStyleMessageOn = true;
        public bool SideBarMessageOn = true;

        // Time when the last push happened
        double lastPushTime;

        #endregion

        /// <summary>
        ///  Represent a message in the game
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
                this.Message = message;
                this.Type = type;
                this.Color = color;
                this.PushTime = pushTime;
                this.Position = new Vector3(0, 0, 0);
            }

            public GameMessage(string message, MessageType type, Color color, double pushTime, Vector3 position)
            {
                this.Message = message;
                this.Type = type;
                this.Color = color;
                this.PushTime = pushTime;
                this.Position = position;
            }
        }



        #region Present Wherever Messages
        /// <summary>
        /// Present messages that may appear anywhere
        /// </summary>
        /// <param name="gameTime"></param>
        private void PresentWhereverMessages(GameTime gameTime)
        {
            if(BubbleUpMessageOn)
                PresentBubbleUpMessage(gameTime);

            if(FlyAwayMessageOn)
                PresentFlyAwayMessage(gameTime);

            if(NoneStyleMessageOn)
                PresentNoneStyleMessage(gameTime);
        }

        /// <summary>
        /// Present Bubble-up Message
        /// </summary>
        /// <param name="gameTime"></param>
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
                    Color textColor = new Color(message.Color.R, message.Color.G, message.Color.B,
                                    (byte)(255 * (1 - passedTime / BubbleUpPeriodLength)));
                    Color shadowColor = new Color(0, 0, 0,
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

        /// <summary>
        /// Present Flay-away message
        /// </summary>
        /// <param name="gameTime"></param>
        private void PresentFlyAwayMessage(GameTime gameTime)
        {
            // ! Time not finished 
            while (FlyAwayMessageQueue.Count != 0 && gameTime.TotalGameTime.TotalSeconds > FlyAwayMessageQueue.Peek().PushTime )
            {
                FlyAwayMessageQueue.Dequeue();
            }
        }

        /// <summary>
        /// Present None-style Message
        /// </summary>
        /// <param name="gameTime"></param>
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

        #endregion

        /// <summary>
        /// Present all kinds of messages
        /// </summary>
        /// <param name="gameTime"></param>
        private void PresentMessages(GameTime gameTime)
        {
            if(SideBarMessageOn)
                PresentSideBarMessages(gameTime);
            PresentWhereverMessages(gameTime);
        }


        /// <summary>
        /// Shows a message at specific position
        /// </summary>
        public void ShowMessage(string message, Vector3 position, MessageType type,
                                                                  MessageStyle style,
                                                                  Color color)
        {

            switch(style)
            {
                case MessageStyle.BubbleUp : 
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

        /// <summary>
        /// Push a message into the side-bar message queue
        /// </summary>
        public void PushMessage(string message, MessageType type, Color color)
        {
            lastPushTime = BaseGame.Singleton.CurrentGameTime.TotalGameTime.TotalSeconds;
            messageQueue.Enqueue(new GameMessage(message, type, color, lastPushTime));
        }


        #endregion


        public void SetCursorFocus(Vector3 position, Color color)
        {
            focusElapsedTime = 0;
            focusPosition = position;
            focusColor = color;
        }


        // The size of the plain progress bar in pixel.
        // Used to show blood and magic of an Entity.
        readonly Point progressFullSize = new Point(80, 5);

        /// <summary>
        /// Draw a progress bar for either style
        /// </summary>
        /// <param name="style"></param>
        /// <param name="percentage">A float point number show the percentage between 0 and 100</param>
        /// <param name="color">The color of the rectangle</param>
        public void DrawProgress(Vector3 position, int yOffset, int length, float percentage, Color color)
        {
            Point position2D = game.Project(position);

            Rectangle fullRect = new Rectangle((int)position2D.X - 1, (int)position2D.Y - 1 - yOffset,
                                              length + 2, progressFullSize.Y + 2);

            Rectangle percentagedRect = new Rectangle((int)position2D.X, (int)position2D.Y - yOffset,
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
                for (int i = 0; i < player.Groups.Count; i++)
                {
                    List<GameObject> list = player.Groups[i];

                    if (list.Count > 0 && list[0].ProfileButton != null)
                    {
                        list[0].ProfileButton.Count = list.Count;
                        AddProfile(list[0].ProfileButton, player.CurrentGroupIndex == i );
                    }
                }

                if (player.CurrentGroup != null && player.CurrentGroup.Count > 0)
                {
                    if (player.CurrentGroup[0].Owner == null)
                        snapShotName.Color = Color.White;
                    else if (player.CurrentGroup[0].Owner is LocalPlayer)
                        snapShotName.Color = Color.Yellow;
                    else
                        snapShotName.Color = Color.Red;
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
                    Icon snapshot = Player.LocalPlayer.CurrentGroup[0].Snapshot;
                    snapShot.Texture = snapshot.Texture;
                    snapShot.SourceRectangle = snapshot.Region;
                }
                else
                {
                    snapShot.Texture = null;
                }

                //if (player.CurrentGroup != null &&
                //    player.CurrentGroup.Count > 0)
                //{
                //    name.Text = player.CurrentGroup[0].Name;

                //    if (player.CurrentGroup[0].Owner != null)
                //        playerName.Text = player.CurrentGroup[0].Owner.Name;
                //}
            }
            ui.Update(gameTime);

            Player currentPlayer = Player.LocalPlayer;

#if DEBUG
            if (Player.LocalPlayer.CurrentGroup != null &&
                Player.LocalPlayer.CurrentGroup.Count > 0 &&
                Player.LocalPlayer.CurrentGroup[0].Owner != null)
            {
                currentPlayer = Player.LocalPlayer.CurrentGroup[0].Owner;
            }
#endif

            lumberTextField.Text = currentPlayer.Lumber.ToString();
            goldTextField.Text = currentPlayer.Gold.ToString();
            Color color = Color.White;
            if (currentPlayer.Food > currentPlayer.FoodCapacity)
                color = Color.Red;
            foodTextField.Text = currentPlayer.Food.ToString() + "/" + currentPlayer.FoodCapacity.ToString();
            foodTextField.Color = color;
        }


        //private void ResetProfileButtonArea()
        //{
        //    int currentProfileIndex = Player.LocalPlayer.CurrentGroupIndex;
        //    Rectangle rect = new Rectangle(ProfileBaseX, 550, ProfileWidth, ProfileWidth);
        //    for (int i = 0; i < profileButtons.Count; i++)
        //    {
        //        if (i == currentProfileIndex)
        //        {
        //            profileButtons[i].Area =
        //                new Rectangle(  rect.X - ProfileWidth / 4, rect.Y - ProfileWidth / 2,
        //                                rect.Width + ProfileWidth / 2, rect.Height + ProfileWidth / 2);
        //        }
        //        else
        //        {
        //            profileButtons[i].Area = rect;
                    
        //        }
        //        rect.X += ProfileWidth + ProfileSpace;
        //        profileButtons[i].ResetDestinationRectangle();
        //    }
        //}

        public void Draw(GameTime gameTime)
        {
            PresentMessages(gameTime);
            game.Graphics2D.Present();

            // Draw screen border fadeout
            ui.Sprite.Begin();
            ui.Sprite.Draw(borderFadeout,
                new Rectangle(0, 0, game.ScreenWidth, game.ScreenHeight), Color.White);

#if DEBUG
            game.Graphics2D.DrawString(Mouse.GetState().X.ToString() + "," + Mouse.GetState().Y.ToString(),
                                    15f / 23, new Vector2(10, 30), Color.White);
#endif

            ui.Sprite.End();

            int currentProfileIndex = Player.LocalPlayer.CurrentGroupIndex;


            ui.Draw(gameTime);

            DrawFogOfWar();

            // Draw statistics
            Rectangle dest = UIElement.GetRelativeRectangle(
                ResourcePanelDestinationRectangle, ui, ScaleMode.ScaleX, Anchor.TopRight);
            float y = dest.Y + dest.Height / 2 - 10;

            Rectangle status = UIElement.GetRelativeRectangle(
                StatusDestination, ui, ScaleMode.ScaleX, Anchor.BottomLeft);


            ui.Sprite.Begin();


            Player player = Player.LocalPlayer;

#if DEBUG
            if (Player.LocalPlayer.CurrentGroup != null &&
                Player.LocalPlayer.CurrentGroup.Count > 0 &&
                Player.LocalPlayer.CurrentGroup[0].Owner != null)
            {
                player = Player.LocalPlayer.CurrentGroup[0].Owner;
            }
#endif

            // Draw environment indicator
            ui.Sprite.Draw(panelsTexture,
                UIElement.GetRelativeRectangle(EnvironmentIndicatorDestination, ui,
                                               ScaleMode.ScaleY, Anchor.TopRight),
                EnvironmentIndicatorSource, GameObject.ColorFromPercentage(1 - player.EnvironmentLevel));


            DrawCursorFocus(gameTime, ui.Sprite);

            //if (world.FogOfWar != null)
            //{
            //    ui.Sprite.Draw(world.FogOfWar.Current, new Rectangle(0, 100, 128, 128), Color.White);
            //    ui.Sprite.Draw(world.FogOfWar.Discovered, new Rectangle(0, 230, 128, 128), Color.White);
            //    ui.Sprite.Draw(world.FogOfWar.Mask, new Rectangle(0, 360, 128, 128), Color.White);
            //}

            ui.Sprite.End();

            game.Graphics2D.Present();

            DrawDisappear(gameTime);
        }

        private void DrawCursorFocus(GameTime gameTime, SpriteBatch sprite)
        {
            const double FocusDuration = 0.2f;
            const int FocusSize = 128;

            if (focusElapsedTime < FocusDuration)
            {
                int frame = (int)(focusAnimation.Length * focusElapsedTime / FocusDuration);

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

        VertexPositionTexture[] fogOfWarVertices;
        VertexDeclaration fogOfWarDeclaration;

        private void DrawFogOfWar()
        {
            if (world.FogOfWar.Mask != null)
            {
                // Setup vertices
                Rectangle destination = minimap.ActualArea;

                fogOfWarVertices[0].Position = game.Graphics2D.ScreenToEffect(destination.Left, destination.Bottom);
                fogOfWarVertices[1].Position = game.Graphics2D.ScreenToEffect(destination.Left, destination.Top);
                fogOfWarVertices[2].Position = game.Graphics2D.ScreenToEffect(destination.Right, destination.Top);
                fogOfWarVertices[3].Position = game.Graphics2D.ScreenToEffect(destination.Right, destination.Bottom);

                // Draw
                Effect effect = game.Graphics2D.Effect;

                effect.CurrentTechnique = effect.Techniques["FogOfWar"];
                effect.Parameters["BasicTexture"].SetValue(world.FogOfWar.Current);
                effect.Begin();
                effect.CurrentTechnique.Passes[0].Begin();

                game.GraphicsDevice.VertexDeclaration = fogOfWarDeclaration;
                game.GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>
                    (PrimitiveType.TriangleFan, fogOfWarVertices, 0, 2);

                effect.CurrentTechnique.Passes[0].End();
                effect.End();

                // This will be drawed next frame...
                world.Landscape.FogTexture = world.FogOfWar.Mask;
            }
        }

        /// <summary>
        /// Helper for moving a value around in a circle.
        /// </summary>
        Vector2 MoveInCircle(GameTime gameTime, double intensity)
        {
            double time = (gameTime.TotalGameTime.TotalSeconds - startTime) * intensity / DisappearingTime * 2 *Math.PI;

            float x = (float)Math.Cos(time);
            float y = (float)Math.Sin(time);

            return new Vector2(x, y);
        }

        /// <summary>
        /// Helper computes a value that oscillates over time.
        /// </summary>
        float Pulsate(GameTime gameTime)
        {
            double amount = (gameTime.TotalGameTime.TotalSeconds - startTime) / DisappearingTime * Math.PI;
            return ((float)Math.Sin(amount - Math.PI/2) + 1) / 2 * 255;
        }


        /// <summary>
        /// Set the profile button when character is selected
        /// </summary>
        /// <param name="element"></param>
        public void SetProfile(SpellButton[] buttons)
        {
            ClearProfile();
            AddProfile(buttons);
        }

        /// <summary>
        /// Add profile buttons
        /// </summary>
        /// <param name="buttons"></param>
        public void AddProfile(SpellButton[] buttons)
        {
            foreach (SpellButton button in buttons)
                AddProfile(button, false);
        }

        /// <summary>
        /// Add one profile button
        /// </summary>
        /// <param name="button"></param>
        public void AddProfile(SpellButton button, bool enlarge)
        {
            int size = enlarge ? (int)(ProfileWidth * 1.2f) : ProfileWidth;
            button.Area = new Rectangle(profileNextX, 590 - size, size, size);
            button.Anchor = Anchor.BottomLeft;
            button.ScaleMode = ScaleMode.ScaleY;
            profilePanel.Add(button);
            profileButtons.Add(button);
            profileNextX += (ProfileSpace + size);
            button.Click += delegate(object o, EventArgs e)
            {
                Player.LocalPlayer.SelectionDirty = true;
            };
        }

        /// <summary>
        /// Remove profile buttons
        /// </summary>
        /// <param name="buttons"></param>
        public void RemoveProfile(SpellButton[] buttons)
        {
            foreach (SpellButton button in buttons)
            {
                profileButtons.Remove(button);
                profilePanel.Remove(button);
            }
        }

        /// <summary>
        /// Clear profile buttons
        /// </summary>
        public void ClearProfile()
        {
            foreach (SpellButton button in profileButtons)
            {
                profilePanel.Remove(button);
            }
            profileButtons.Clear();
            profileNextX = ProfileBaseX;
        }

        Random rand = new Random();
        Vector2 randomOffset;
        void DrawDisappear(GameTime gameTime)
        {
            if (startTime == 0)
            {
                startTime = gameTime.TotalGameTime.TotalSeconds;
            }
            if (gameTime.TotalGameTime.TotalSeconds > startTime + DisappearingTime)
            {
                return;
            }

            // Draw the background image.
            SpriteBatch spriteBatch = ui.Sprite;

            // Begin the sprite batch.
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend,
                              SpriteSortMode.Immediate,
                              SaveStateMode.SaveState);

            game.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Mirror;
            game.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Mirror;
            
            game.GraphicsDevice.Textures[1] = distortion;


            // Set an effect parameter to make our overlay
            // texture scroll in a giant circle.

            disappearEffect.Parameters["OverlayScroll"].SetValue(MoveInCircle(gameTime, 1));

            disappearEffect.Parameters["Offset"].SetValue(MoveInCircle(gameTime, 0.2) + randomOffset);

            disappearEffect.Parameters["Intensity"].SetValue(MoveInCircle(gameTime, 0.6).Y);

            // Begin the custom effect.
            disappearEffect.Begin();
            disappearEffect.CurrentTechnique.Passes[0].Begin();

            // Draw the sprite, passing the fade amount as the
            // alpha of the SpriteBatch.Draw color parameter.
            byte fade = (byte)Pulsate(gameTime);
            spriteBatch.Draw(LoadingDisplayFinished, ui.DestinationRectangle, 
                                new Rectangle(0, 0, LoadingDisplayFinished.Width, LoadingDisplayFinished.Height),
                             //MoveInCircle(gameTime, LoadingDisplayFinished, 1),
                             new Color(255, 255, 255, (byte)(255 - fade)));

            // End the sprite batch, then end our custom effect.
            spriteBatch.End();

            disappearEffect.CurrentTechnique.Passes[0].End();
            disappearEffect.End();
        }


        /// <summary>
        /// Gets whether the UI overlaps the specified point on the screen
        /// </summary>
        public bool Overlaps(Point p)
        {
            if (controlPanel.ActualEffectiveRegion.Contains(p) || resourcePanel.ActualEffectiveRegion.Contains(p))
                return true;
            else
                return false;
        }
        #endregion

        #region HandleEvent
        public EventResult HandleEvent(EventType type, object sender, object tag)
        {

            if (ui != null &&
                ui.HandleEvent(type, sender, tag) == EventResult.Handled)
                return EventResult.Handled;
            return EventResult.Unhandled;
        }
        #endregion
    }
    #endregion

    #region Cursors
    public static class Cursors
    {
        public static Cursor StoredCursor;

        public static Cursor MenuDefault
        {
            get 
            {
                if (menuDefaultCursor == null)
                    menuDefaultCursor = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/NormalCursor.cur");
                return menuDefaultCursor;
            }
        }
        public static Cursor MenuHighlight
        {
            get
            {
                if (menuHighlightCursor == null)
                    menuHighlightCursor = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/LightedCursor.cur");
                return menuHighlightCursor;
            }
        }
        public static Cursor Default
        {
            get
            {
                if (defaultCursor == null)
                    defaultCursor = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/default.ani");
                return defaultCursor;
            }
        }

        public static Cursor Attack
        {
            get
            {
                if (attack == null)
                    attack = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/attack.ani");
                return attack;
            }
        }

        public static Cursor Gather
        {
            get
            {
                if (gather == null)
                    gather = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/gather.ani");
                return gather;
            }
        }

        public static Cursor TargetRed
        {
            get
            {
                if (targetRed == null)
                    targetRed = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/target_red.ani");
                return targetRed;
            }
        }

        public static Cursor TargetGreen
        {
            get
            {
                if (targetGreen == null)
                    targetGreen = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/target_green.ani");
                return targetGreen;
            }
        }

        public static Cursor TargetNeutral
        {
            get
            {
                if (targetNeutral == null)
                    targetNeutral = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/target_neutral.ani");
                return targetNeutral;
            }
        }

        public static Cursor TargetDisable
        {
            get
            {
                if (targetDisable == null)
                    targetDisable = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/target_disable.ani");
                return targetDisable;
            }
        }

        public static Cursor Top
        {
            get
            {
                if (top == null)
                    top = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/screen_top.cur");
                return top;
            }
        }

        public static Cursor Bottom
        {
            get
            {
                if (bottom == null)
                    bottom = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/screen_bottom.cur");
                return bottom;
            }
        }

        public static Cursor Left
        {
            get
            {
                if (left == null)
                    left = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/screen_left.cur");
                return left;
            }
        }

        public static Cursor Right
        {
            get
            {
                if (right == null)
                    right = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/screen_right.cur");
                return right;
            }
        }

        public static Cursor Move
        {
            get
            {
                if (move == null)
                    move = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/screen_move.cur");
                return move;
            }
        }

        public static Cursor Rotate
        {
            get
            {
                if (rotate == null)
                    rotate = BaseGame.Singleton.ZipContent.LoadCursor("Content/Cursors/screen_rotate.cur");
                return rotate;
            }
        }

        static Cursor menuDefaultCursor;
        static Cursor menuHighlightCursor;
        static Cursor defaultCursor;
        static Cursor attack;
        static Cursor gather;
        static Cursor targetRed;
        static Cursor targetGreen;
        static Cursor targetNeutral;
        static Cursor targetDisable;
        static Cursor top;
        static Cursor bottom;
        static Cursor left;
        static Cursor right;
        static Cursor move;
        static Cursor rotate;
    }
    #endregion
}
