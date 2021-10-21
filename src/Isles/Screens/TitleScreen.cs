// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Isles.Engine;
using Isles.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Isles
{
    /// <summary>
    /// Screen that shows the Title.
    /// </summary>
    public class TitleScreen : IScreen
    {
        // The screen that will be loaded when
        // new game button is pressed
        private readonly GameScreen gameScreen;

        // Texture for the title screen
        private readonly Texture2D titleTexture;

        // Texture for the buttons
        private readonly Texture2D buttonsTexture;

        // Texture used for disappearing effect
        private Texture2D titleDisplayShotTexture;

        // Texture for transition to loading
        private readonly Texture2D loadingDisplayTexture;

        // Disappear effect
        private readonly Effect disappearEffect;

        // Distortion texture
        private readonly Texture2D distortion;
        private Vector2 randomOffset;
        private int highLightMoveTo;
        private double modeChangeTimeRecord;
        private bool modeChange;

        // Time for changing into the new screen
        private const double ChangingTime = 1;

        /// <summary>
        /// Use the double variable to describe the expected position
        /// of the high light panel.
        /// In this way, it can be guaranteed that highLight.X will
        /// finally miss its destination no more than 1 pixel.
        /// </summary>
        private double expectedHighlightPos;
        private double creditStartTime;
        private bool creditStarted;
        private const double CreditEmergingTime = 1;
        private const float CreditFontSize = 26f / 23;
        private readonly double creditRollingTime = 10;
        private readonly int creditStringLength = 400;

        // Buttons in array arranged in the following order:
        //      1. Campaign
        //      2. Skirmish
        //      3. Options
        //      4. Credit
        //      5. Quit
        private readonly MenuButton[] buttons = new MenuButton[5];

        // Container of all the ui element in
        // this screen
        private readonly UIDisplay ui;

        private const int buttonSourceWidth = 199;
        private const int buttonSourceHeight = 38;
        private const int buttonDestinationWidth = 100;
        private const int buttonDestinationHeight = 16;
        private readonly int buttonBias = 150;
        private const int hightLightDestinationHeight = 25;
        private readonly Panel highLight;
        private readonly Panel loadingPanel;
        private readonly Panel titlePanel;
        private readonly Panel leftMaskPanel;
        private readonly Panel rightMaskPanel;
        private readonly Panel credit;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameScreen"></param>
        public TitleScreen(GameScreen gameScreen)
        {
            this.gameScreen = gameScreen;
            ui = new UIDisplay(BaseGame.Singleton);

            titleTexture = BaseGame.Singleton.Content.Load<Texture2D>("UI/MainEntry");
            buttonsTexture = BaseGame.Singleton.Content.Load<Texture2D>("UI/Buttons");
            loadingDisplayTexture = BaseGame.Singleton.Content.Load<Texture2D>("UI/LoadingDisplay");
            distortion = BaseGame.Singleton.Content.Load<Texture2D>("Textures/Distortion");
            disappearEffect = BaseGame.Singleton.Content.Load<Effect>("Effects/Disappear");

            ui.ScaleMode = ScaleMode.Stretch;
            ui.Anchor = Anchor.TopLeft;

            var height = loadingDisplayTexture.Height * ui.Area.Width / loadingDisplayTexture.Width;
            loadingPanel = new Panel(new Rectangle(0, (int)((ui.Area.Height - height) / 2), ui.Area.Width, height))
            {
                Texture = loadingDisplayTexture,
                SourceRectangle = new Rectangle(0, 0, loadingDisplayTexture.Width, loadingDisplayTexture.Height),
                ScaleMode = ScaleMode.Stretch,
                Anchor = Anchor.Center,
            };
            ui.Add(loadingPanel);

            // Set the panel
            titlePanel = new Panel(new Rectangle(0, (int)((ui.Area.Height - height) / 2), ui.Area.Width, height))
            {
                Texture = titleTexture,
                SourceRectangle = new Rectangle(0, 0, titleTexture.Width, titleTexture.Height),
                ScaleMode = ScaleMode.Stretch,
                Anchor = Anchor.Center,
            };
            ui.Add(titlePanel);

            // Test textbox
            // titlePanel.Add(new TextBox(1, Color.White, new Rectangle(0, 0, 400, 50)));
            TextField creditSegment;
            credit = new Panel(new Rectangle(800, 430, 0, 30));
            // credit = new TextField("R", 16, Color.White, new Rectangle(800, 470, 10000, 30));
            titlePanel.Add(credit);
            var offset = 0;

            foreach (var textSegment in File.ReadAllLines("data/credits.txt"))
            {
                if (textSegment.Length == 0)
                {
                    offset += 30;
                    continue;
                }

                Color color = textSegment.EndsWith(":") ? Color.Yellow : Color.White;
                var width = (int)(BaseGame.Singleton.Graphics2D.Font.MeasureString(textSegment).X * CreditFontSize);

                creditSegment = new TextField(textSegment, CreditFontSize, color,
                                                new Rectangle(offset, 0, width, 30), Color.Black);
                offset += width;
                credit.Add(creditSegment);
            }

            creditStringLength = offset + 200;
            creditRollingTime = creditStringLength * 0.015;

            // I'll just hard code the size :(
            leftMaskPanel = new Panel(Rectangle.Empty)
            {
                Texture = BaseGame.Singleton.Content.Load<Texture2D>("UI/LeftMask"),
            };
            leftMaskPanel.SourceRectangle = new Rectangle(0, 0, leftMaskPanel.Texture.Width, leftMaskPanel.Texture.Height);
            leftMaskPanel.ScaleMode = ScaleMode.Fixed;
            leftMaskPanel.Anchor = Anchor.TopLeft;
            leftMaskPanel.Visible = false;
            leftMaskPanel.EffectiveRegion = Rectangle.Empty;
            ui.Add(leftMaskPanel);

            rightMaskPanel = new Panel(Rectangle.Empty)
            {
                Texture = BaseGame.Singleton.Content.Load<Texture2D>("UI/RightMask"),
            };
            rightMaskPanel.SourceRectangle = new Rectangle(0, 0, rightMaskPanel.Texture.Width, rightMaskPanel.Texture.Height);
            rightMaskPanel.ScaleMode = ScaleMode.Fixed;
            rightMaskPanel.Anchor = Anchor.TopLeft;
            rightMaskPanel.Visible = false;
            rightMaskPanel.EffectiveRegion = Rectangle.Empty;
            ui.Add(rightMaskPanel);

            buttonBias = titlePanel.Area.Width / 6;
            var buttonY = titlePanel.Area.Height / 8 * 7;
            highLight = new Panel(new Rectangle
                       (buttonBias, buttonY, buttonDestinationWidth, hightLightDestinationHeight))
            {
                Texture = buttonsTexture,
                SourceRectangle = new Rectangle(0, 5 * buttonSourceHeight, buttonSourceWidth, 66),
                Anchor = Anchor.TopLeft,
                ScaleMode = ScaleMode.ScaleY,
            };

            // Set buttons
            for (var i = 0; i < 5; i++)
            {
                buttons[i] = new MenuButton(buttonsTexture,
                             new Rectangle(buttonBias * i + buttonBias * 4 / 5, buttonY, buttonDestinationWidth, buttonDestinationHeight),
                             new Rectangle(0, i * 32, buttonSourceWidth, buttonSourceHeight), Keys.F1 + i, null)
                {
                    Texture = buttonsTexture,
                };
                buttons[i].Disabled = buttons[i].SourceRectangle
                    = new Rectangle(0, i * buttonSourceHeight, buttonSourceWidth, buttonSourceHeight);
                buttons[i].Pressed = buttons[i].Hovered
                    = new Rectangle(0, 263 + 38 * i, buttonSourceWidth, buttonSourceHeight);
                buttons[i].Anchor = Anchor.TopLeft;
                buttons[i].ScaleMode = ScaleMode.ScaleY;
                buttons[i].Index = i;
                buttons[i].Enter += (o, e) => HighLightMoveTo((o as MenuButton).Index);
                titlePanel.Add(buttons[i]);
            }

            buttons[0].HotKey = Keys.C; // Campaign
            buttons[1].HotKey = Keys.S; // Skirmish
            buttons[2].HotKey = Keys.O; // Options
            buttons[3].HotKey = Keys.D; // Credit
            buttons[4].HotKey = Keys.Q; // Quit

            // buttons[0].Enabled = false;
            buttons[2].Enabled = false;
            titlePanel.Add(highLight);

            // Click event for Campaign button
            buttons[0].Click += (o, e) =>
            {
                Audios.Play("OK");
                BaseGame.Singleton.StartScreen(gameScreen);
            };

            buttons[1].Click += (o, e) =>
            {
                Audios.Play("OK");
                modeChange = true;
            };

            buttons[3].Click += (o, e) =>
            {
                Event.SendMessage(EventType.Hit, this, this, null, 7);
                Audios.Play("OK");
                StartCredit();
            };

            // Click even for Exit button
            buttons[4].Click += (o, e) => BaseGame.Singleton.Exit();

            highLightMoveTo = buttons[0].Area.X;
            expectedHighlightPos = highLightMoveTo;
            var rand = new Random();
            randomOffset = new Vector2((float)rand.NextDouble(),
                                        (float)rand.NextDouble()) * 0.1f;
        }

        /// <summary>
        /// Set the high light destination.
        /// </summary>
        /// <param name="index"></param>
        private void HighLightMoveTo(int index)
        {
            highLightMoveTo = buttons[index].Area.X;
            if (index == 1)
            {
                highLightMoveTo -= (int)(10.0 / 800 * ui.DestinationRectangle.Width);
            }

            if (index == 2)
            {
                highLightMoveTo -= (int)(17.0 / 800 * ui.DestinationRectangle.Width);
            }

            if (index == 3)
            {
                highLightMoveTo -= (int)(15.0 / 800 * ui.DestinationRectangle.Width);
            }

            if (index == 4)
            {
                highLightMoveTo -= (int)(25.0 / 800 * ui.DestinationRectangle.Width);
            }
        }

        private void StartLoadingCampaign()
        {
            ui.Dispose();
            gameScreen.StartLevel("World.xml");
            BaseGame.Singleton.StartScreen(gameScreen);
        }

        private void StartCredit()
        {
            creditStarted = true;
            highLight.Visible = false;
            leftMaskPanel.Visible = true;
            rightMaskPanel.Visible = true;
            creditStartTime = BaseGame.Singleton.CurrentGameTime.TotalGameTime.TotalSeconds;
            for (var i = 0; i < 5; i++)
            {
                buttons[i].IgnoreMessage = true;
            }
        }

        private void StopCredit()
        {
            creditStarted = false;
            highLight.Visible = true;
            leftMaskPanel.Visible = false;
            rightMaskPanel.Visible = false;
            for (var i = 0; i < 5; i++)
            {
                buttons[i].IgnoreMessage = false;
                buttons[i].SetAlpha(255);
            }

            credit.X = 800;
        }

        private void UpdateCredit(GameTime gameTime)
        {
            if (!creditStarted)
            {
                return;
            }

            var elapsedTime = gameTime.TotalGameTime.TotalSeconds - creditStartTime;
            if (elapsedTime < CreditEmergingTime)
            {
                var a = (byte)((1 - elapsedTime / CreditEmergingTime) * 255);
                for (var i = 0; i < 5; i++)
                {
                    buttons[i].SetAlpha(a);
                }
            }
            else if (elapsedTime < CreditEmergingTime + creditRollingTime)
            {
                credit.X = (int)(800 - (800 + creditStringLength) /
                            creditRollingTime * (elapsedTime - CreditEmergingTime));
            }
            else if (elapsedTime < CreditEmergingTime * 2 + creditRollingTime)
            {
                var a = (byte)((elapsedTime - CreditEmergingTime - creditRollingTime) /
                                    CreditEmergingTime * 255);
                for (var i = 0; i < 5; i++)
                {
                    buttons[i].SetAlpha(a);
                }
            }
            else
            {
                StopCredit();
            }
        }

        /// <summary>
        /// Handle game updates.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            expectedHighlightPos += (highLightMoveTo - expectedHighlightPos)
                                    * 5.5 * gameTime.ElapsedGameTime.TotalSeconds;
            highLight.X = (int)expectedHighlightPos;
            foreach (TextField t in credit.Elements)
            {
                t.ResetDestinationRectangle();
            }

            ui.Update(gameTime);

            // Update mask destination rectangle
            Rectangle background = titlePanel.DestinationRectangle;

            var width = background.Width * leftMaskPanel.Texture.Width / titlePanel.Texture.Width;
            var height = background.Height * leftMaskPanel.Texture.Height / titlePanel.Texture.Height;

            var area = new Rectangle(0, background.Bottom - height, width, height);

            leftMaskPanel.Area = area;

            area.X = background.Right - width;

            rightMaskPanel.Area = area;

            UpdateCredit(gameTime);
        }

        /// <summary>
        /// Handle game draw event.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            ui.Draw(gameTime);
            if (modeChange && titleDisplayShotTexture != null)
            {
                modeChangeTimeRecord += gameTime.ElapsedGameTime.TotalSeconds;
                if (modeChangeTimeRecord > ChangingTime)
                {
                    StartLoadingCampaign();
                }
                else
                {
                    SpriteBatch spriteBatch = ui.Sprite;
                    // Begin the sprite batch.
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

                    BaseGame.Singleton.GraphicsDevice.Textures[1] = distortion;

                    // Set an effect parameter to make our overlay
                    // texture scroll in a giant circle.
                    disappearEffect.Parameters["Offset"].SetValue(randomOffset);

                    // Begin the custom effect.
                    disappearEffect.CurrentTechnique.Passes[0].Apply();

                    // Draw the sprite, passing the fade amount as the
                    // alpha of the SpriteBatch.Draw color parameter.
                    var fade = (byte)(modeChangeTimeRecord / ChangingTime * 255);
                    spriteBatch.Draw(titleDisplayShotTexture, ui.DestinationRectangle,
                                     new Color(255, 255, 255, (byte)(255 - fade)));

                    // End the sprite batch, then end our custom effect.
                    spriteBatch.End();
                }

                return;
            }

            if (modeChange)
            {
                titleDisplayShotTexture = BaseGame.Singleton.ScreenshotCapturer.Screenshot;
                ui.Remove(titlePanel);
                Draw(gameTime);
            }
        }

        /// <summary>
        /// Called when this screen is activated.
        /// </summary>
        public void Enter()
        {
            Audios.Counter = 0;
            BaseGame.Singleton.Cursor = Cursors.MenuDefault;
            Audios.PlayMusic("Menu", true, 0, 5);
        }

        /// <summary>
        /// Called when this screen is deactivated.
        /// </summary>
        public void Leave()
        {
        }

        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        public void LoadContent()
        {
        }

        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        /// <param name="unloadAllContent">Which type of content to unload.</param>
        public void UnloadContent() { }

        public EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (type == EventType.Hit && sender == this)
            {
                Audios.Play("Credit");
                return EventResult.Handled;
            }

            // Let UI handle event first
            if (type == EventType.KeyDown && (tag as Keys?).Value == Keys.Escape)
            {
                StopCredit();
            }

            return ui != null &&
                ui.HandleEvent(type, sender, tag) == EventResult.Handled
                ? EventResult.Handled
                : EventResult.Unhandled;
        }

        public void Dispose()
        {
        }
    }

    public class MenuButton : Button
    {
        // The name of the menu button
        private string name;

        /// <summary>
        ///  Gets or sets the name of the menu button.
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// Construct a new menu button.
        /// </summary>
        public MenuButton(Texture2D texture,
            Rectangle area, Rectangle srcRectangle, Keys hotKey, string name)
            : base(texture, area, srcRectangle, hotKey)
        {
            Name = name;
        }

        private float alpha = 1f;

        public void SetAlpha(byte a)
        {
            alpha = a / 255f;
        }

        /// <summary>
        /// Draw UI element.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            if (Texture != null && Visible)
            {
                Rectangle sourRect = SourceRectangle;
                Rectangle destRect = DestinationRectangle;
                var buttonColor = new Color();
                if (Enabled)
                {
                    switch (state)
                    {
                        case ButtonState.Normal:
                            sourRect = SourceRectangle; buttonColor = new Color(255, 255, 255, (byte)(160 * alpha));
                            break;
                        case ButtonState.Hover:
                            sourRect = Hovered; buttonColor = new Color(255, 255, 255, (byte)(255 * alpha));
                            break;
                        case ButtonState.Press:
                            sourRect = Pressed; buttonColor = new Color(255, 255, 255, (byte)(255 * alpha));
                            destRect.Y += 2;
                            break;
                    }
                }
                else
                {
                    sourRect = Disabled;
                    buttonColor = new Color(128, 128, 128, (byte)(128 * alpha));
                }

                sprite.Draw(Texture, destRect, sourRect, buttonColor);
            }
        }
    }
}
