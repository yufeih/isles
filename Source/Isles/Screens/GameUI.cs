//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;
using Isles.UI;

namespace Isles
{
    /// <summary>
    /// This is the in-game UI
    /// </summary>
    /// <remarks>
    /// Guess we are about to implement a configurable game UI,
    /// maybe XML based or whatever. For now just hardcode most
    /// of our game UI.
    /// </remarks>
    public class GameUI : IGameUI
    {
        #region Fields
        public const int ButtonWidth = 80;
        public const int ButtonHeight = 40;
        public const int ScrollButtonWidth = 40;
        public const int IconTextureRowCount = 8;
        public const int IconTextureColumnCount = 4;

        /// <summary>
        /// Gets or sets game world
        /// </summary>
        public GameWorld World;

        /// <summary>
        /// Gets or sets the hand
        /// </summary>
        public Hand Hand;

        /// <summary>
        /// Game screen ui
        /// </summary>
        UIDisplay ui;

        /// <summary>
        /// Game scroll panel
        /// </summary>
        ScrollPanel scrollPanel;

        /// <summary>
        /// Click the buildButton to enter build panel
        /// </summary>
        Button buildButton;

        /// <summary>
        /// Buttons for all buildings
        /// </summary>
        List<Button> buildingButtons = new List<Button>();

        /// <summary>
        /// Buttons for all spells
        /// </summary>
        List<Button> spellButtons = new List<Button>();

        /// <summary>
        /// UI textures
        /// </summary>
        Texture2D iconTexture, uiTexture;

        readonly float[] statisticsTextX =
        {
            4.6f / 24.25f, 11.6f / 24.25f, 18.3f / 24.25f
        };

        readonly Rectangle statisticsDestination = new Rectangle(
            400, 2, 400, 36);

        readonly Rectangle statisticsSource = new Rectangle(
            0, 0, 690, 64);

        readonly Rectangle statusDestination = new Rectangle(
                5, 495, 150, 120);

        readonly Rectangle statusSource = new Rectangle(
            0, 62, 256, 256);

        readonly Rectangle signSource = new Rectangle(
            246, 80, 236, 226);

        readonly Color textColor = Color.Black;//new Color(80, 42, 0);
        readonly Color textColorDark = Color.Black;

        /// <summary>
        /// Gets UI icon texture
        /// </summary>
        public Texture2D IconTexture
        {
            get { return iconTexture; }
        }

        /// <summary>
        /// Rectangle where entity status is drawed
        /// </summary>
        readonly Rectangle statusRectangle = new Rectangle(5, 400, 400, 100);
        #endregion

        #region Methoes
        /// <summary>
        /// Creates a new game user interface
        /// </summary>
        public GameUI(Game game)
        {
            // Create a new UI display
            ui = new UIDisplay(BaseGame.Singleton);

            // Load UI textures
            iconTexture = game.Content.Load<Texture2D>("UI/Icons");
            uiTexture = game.Content.Load<Texture2D>("UI/ui");

            Reset(null, null);
        }

        Rectangle GetIconRectangle(int n)
        {
            if (iconTexture == null)
                throw new InvalidOperationException();

            int x = n % IconTextureColumnCount;
            int y = n / IconTextureColumnCount;
            int w = iconTexture.Width / IconTextureColumnCount;
            int h = iconTexture.Height / IconTextureRowCount;

            return new Rectangle(x * w, y * h, w, h);
        }

        /// <summary>
        /// Gets the default icon rectangle
        /// </summary>
        /// <returns></returns>
        public static Rectangle GetDefaultIconRectangle(int n)
        {
            int x = n % IconTextureColumnCount;
            int y = n / IconTextureColumnCount;
            int w = 1024 / IconTextureColumnCount;
            int h = 1024 / IconTextureRowCount;

            return new Rectangle(x * w, y * h, w, h);
        }

        /// <summary>
        /// Reset the game UI 
        /// </summary>
        public void Reset(GameWorld world, Hand hand)
        {
            this.World = world;
            this.Hand = hand;

            ui.Clear();
            ui.Add(scrollPanel = new ScrollPanel(new Rectangle(
                160, 580 - ButtonHeight, 600, ButtonHeight),
                ButtonWidth, ScrollButtonWidth));

            // Init scroll panel
            scrollPanel.Anchor = Anchor.BottomLeft;
            scrollPanel.ScaleMode = ScaleMode.Stretch;

            scrollPanel.Left.Texture =
            scrollPanel.Right.Texture = iconTexture;

            Rectangle scrollRect = GetIconRectangle(16);
            scrollRect.Width = scrollRect.Width / 2;
            scrollPanel.Left.SourceRectangle = scrollRect;
            scrollRect.X += scrollRect.Width;
            scrollPanel.Right.SourceRectangle = scrollRect;

            SelectNull();
        }

        void EnterBuildMenu(object sender, EventArgs e)
        {
            if (null == World)
                throw new InvalidOperationException();
            
            scrollPanel.Clear();

            XmlElement xml;

            // Initialize building buttons
            // TODO: Optimize this by caching the buttons or whatever
            foreach (string buildingName in World.GameLogic.AvailableBuildings)
            {
                // Find Icon & Hotkey property from game default
                if (GameDefault.Singleton.WorldObjectDefaults.TryGetValue(
                    buildingName, out xml))
                {
                    int icon = 0;
                    int.TryParse(xml.GetAttribute("Icon"), out icon);

                    Keys hotkey = Keys.None;
                    if (xml.HasAttribute("Hotkey"))
                        hotkey = (Keys)Enum.Parse(typeof(Keys), xml.GetAttribute("Hotkey"));

                    // Create a new button
                    Button button = new Button(iconTexture,
                        Rectangle.Empty, GetIconRectangle(icon), hotkey);

                    button.Anchor = Anchor.BottomLeft;
                    button.ScaleMode = ScaleMode.ScaleY;
                    button.Tag = buildingName;

                    button.Click += new EventHandler(PerformBuild);

                    // Add to building panel
                    scrollPanel.Add(button);
                }
            }
        }

        void PerformBuild(object sender, EventArgs e)
        {
            if (Hand.StopActions())
            {
                string type = (sender as Button).Tag as string;

                Entity building = World.Create(type) as Entity;

                if (building != null)
                    Hand.Drag(building);
            }
        }

        /// <summary>
        /// Resets game UI to default state
        /// </summary>
        void SelectNull()
        {
            scrollPanel.Clear();

            // Add build button
            scrollPanel.Add(buildButton = new Button(
                iconTexture, Rectangle.Empty, GetIconRectangle(4), Keys.B));

            buildButton.Click += new EventHandler(EnterBuildMenu);
            
            // Add spell buttons
            if (World != null)
                AddSpells(World.GameLogic.CurrentSpells);
        }

        /// <summary>
        /// Adds a list of spells to the scroll panel
        /// </summary>
        void AddSpells(IEnumerable<Spell> spells)
        {
            Icon icon;
            Button spellButton;

            foreach (Spell spell in spells)
            {
                if (spell.Icon.HasValue)
                    icon = spell.Icon.Value;
                else
                    icon.Region = GetIconRectangle(0);

                scrollPanel.Add(spellButton = new Button(
                    iconTexture, Rectangle.Empty, icon.Region, spell.Hotkey));

                spellButton.Tag = spell;

                spellButton.Click += new EventHandler(delegate(object sender, EventArgs e)
                {
                    Spell s = (sender as Button).Tag as Spell;

                    if (s == null)
                        throw new InvalidOperationException();

                    // Cast the spell
                    Hand.Cast(s);
                });
            }
        }

        public void Update(GameTime gameTime)
        {
            ui.Update(gameTime);
        }

        public void Draw(GameTime gameTime)
        {
            // Draw UI
            ui.Draw(gameTime);

            // Draw status
            //if (entityManager.Selected != null)
            //    entityManager.Selected.DrawStatus(
            //        UIDisplay.GetRelativeRectangle(
            //            statusRectangle, ui, ScaleMode.ScaleY, Anchor.BottomLeft));

            // Draw statistics
            Rectangle dest = UIDisplay.GetRelativeRectangle(
                statisticsDestination, ui, ScaleMode.ScaleX, Anchor.TopRight);
            float y = dest.Y + dest.Height / 2 - 10;

            Rectangle status = UIDisplay.GetRelativeRectangle(
                statusDestination, ui, ScaleMode.ScaleX, Anchor.BottomLeft);

            ui.Sprite.Begin();

            ui.Sprite.Draw(uiTexture, dest, statisticsSource, Color.White);
            ui.Sprite.Draw(uiTexture, status, statusSource, Color.White); 

            ui.Sprite.DrawString(Text.Font, World.GameLogic.Wood.ToString(),
                new Vector2(dest.X + dest.Width * statisticsTextX[0], y), textColor);
            ui.Sprite.DrawString(Text.Font, World.GameLogic.Gold.ToString(),
                new Vector2(dest.X + dest.Width * statisticsTextX[1], y), textColor);
            ui.Sprite.DrawString(Text.Font, World.GameLogic.Food.ToString(),
                new Vector2(dest.X + dest.Width * statisticsTextX[2], y), textColor);

            if (World.Selected.Count == 1)
            {
                ui.Sprite.DrawString(Text.Font, World.Selected[0].Name,
                    new Vector2(status.X + 8, status.Y + 8), textColor,
                    0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);

                string desc = Text.FormatString(
                    World.Selected[0].Description, (int)(status.Width / 0.6f), status.Height);

                ui.Sprite.DrawString(Text.Font, desc,
                    new Vector2(status.X + 8, status.Y + 28), textColor,
                    0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
            }
            else
            {
                ui.Sprite.Draw(uiTexture, status, signSource, Color.White);
            }

            ui.Sprite.End();
        }
        #endregion

        #region IGameUI
        /// <summary>
        /// Called when an entity is selected. Game UI should refresh
        /// itself to match the new entities, e.g., status and spells.
        /// </summary>
        public void Select(Entity entity)
        {
            if (null == entity)
            {
                SelectNull();
            }
            else
            {
                scrollPanel.Clear();
                AddSpells(entity.Spells);
            }
        }

        /// <summary>
        /// Called when multiple entities are selected.
        /// </summary>
        public void SelectMultiple(IEnumerable<Entity> entities)
        {
        }

        /// <summary>
        /// Popup a message
        /// </summary>
        public void ShowMessage(MessageType type, string message, Vector2 position, Color color)
        {
        }
        #endregion
    }
}
