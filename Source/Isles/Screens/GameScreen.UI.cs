//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;
using Isles.UI;

namespace Isles
{
    /// <summary>
    /// This part of the GameScreen deals with in-game UI
    /// </summary>
    public partial class GameScreen : IGameUI
    {
        //#region Fields
        //public const int ButtonWidth = 80;
        //public const int ButtonHeight = 40;
        //public const int ScrollButtonWidth = 40;

        ///// <summary>
        ///// Game screen ui
        ///// </summary>
        //UIDisplay ui;

        ///// <summary>
        ///// Game scroll panel
        ///// </summary>
        //ScrollPanel scrollPanel;

        ///// <summary>
        ///// Whether the scroll panel is restored
        ///// </summary>
        //bool scrollPanelRestored;

        ///// <summary>
        ///// Pending request
        ///// </summary>
        //List<UIElement> addedToScrollPanel = new List<UIElement>();

        ///// <summary>
        ///// Click the buildButton to enter build panel
        ///// </summary>
        //Button buildButton;

        ///// <summary>
        ///// Buttons for all buildings
        ///// </summary>
        //List<Button> buildingButtons = new List<Button>();

        ///// <summary>
        ///// Buttons for all spells
        ///// </summary>
        //List<Button> spellButtons = new List<Button>();

        ///// <summary>
        ///// UI texture
        ///// </summary>
        //Texture2D iconTexture;

        ///// <summary>
        ///// Gets UI icon texture
        ///// </summary>
        //public Texture2D IconTexture
        //{
        //    get { return iconTexture; }
        //}

        ///// <summary>
        ///// Rectangle where entity status is drawed
        ///// </summary>
        //readonly Rectangle statusRectangle = new Rectangle(5, 400, 400, 100);
        //#endregion

        //#region Setup User Interface
        ///// <summary>
        ///// Reset game screen user interface
        ///// </summary>
        //void ResetUserInterface()
        //{
        //    // Create a new UI display
        //    ui.Clear();
        //    ui.Add(scrollPanel = new ScrollPanel(new Rectangle(
        //        20, 580 - ButtonHeight, 640, ButtonHeight),
        //        ButtonWidth, ScrollButtonWidth));

        //    // Init scroll panel
        //    scrollPanel.Anchor = Anchor.BottomLeft;
        //    scrollPanel.ScaleMode = ScaleMode.Stretch;

        //    scrollPanel.LeftScroll.Texture =
        //    scrollPanel.RightScroll.Texture = iconTexture;

        //    Rectangle scrollRect = GetIcon(16);
        //    scrollRect.Width = scrollRect.Width / 2;
        //    scrollPanel.LeftScroll.SourceRectangle = scrollRect;
        //    scrollRect.X += scrollRect.Width;
        //    scrollPanel.RightScroll.SourceRectangle = scrollRect;

        //    // Reset ui
        //    scrollPanel.Clear();
        //    buildingButtons.Clear();
        //    spellButtons.Clear();

        //    // Add build button
        //    scrollPanel.Add(buildButton = new Button(
        //        iconTexture, Rectangle.Empty, GetIcon(4), Keys.B));

        //    buildButton.Click += new EventHandler(buildButton_Click);

        //    // Initialize building buttons
        //    #region Building Buttons
        //    for (int i = 0; i < BuildingSettings.Count; i++)
        //    {
        //        // Create a new button
        //        Button button = new Button(iconTexture, Rectangle.Empty,
        //            GetIcon(BuildingSettings[i].Icon), BuildingSettings[i].Hotkey);

        //        button.Anchor = Anchor.BottomLeft;
        //        button.ScaleMode = ScaleMode.ScaleY;
        //        button.Tag = BuildingSettings[i];

        //        // Add to building panel
        //        buildingButtons.Add(button);

        //        // Building
        //        button.Click += new EventHandler(delegate(object sender, EventArgs e)
        //        {
        //            // Make sure we're not doing anything else
        //            if (hand.StopActions())
        //            {
        //                // Get building settings
        //                BuildingSettings settings = (sender as Button).Tag as BuildingSettings;

        //                // Check dependencies
        //                if (world.GameLogic.CheckDependency(settings.Dependencies))
        //                {
        //                    // Create a building
        //                    Building building = entityManager.CreateBuilding(settings);

        //                    // Drag the building to place it
        //                    hand.Drag(building);
        //                }
        //            }

        //            ResetScrollPanelElements();
        //        });
        //    }
        //    #endregion

        //    // Initialize spell buttons
        //    #region Spell Buttons
        //    for (int i = 0; i < world.GameLogic.SpellSettings.Count; i++)
        //    {
        //        // Create a new button
        //        Button button = new Button(iconTexture, Rectangle.Empty,
        //            GetIcon(world.GameLogic.SpellSettings[i].Icon),
        //            world.GameLogic.SpellSettings[i].Hotkey);

        //        button.Anchor = Anchor.BottomLeft;
        //        button.ScaleMode = ScaleMode.ScaleY;
        //        button.Tag = world.GameLogic.SpellSettings[i];

        //        // Add to spell panel
        //        spellButtons.Add(button);

        //        // Handle click event
        //        button.Click += new EventHandler(delegate(object sender, EventArgs e)
        //        {
        //            // Make sure we're not doing anything else
        //            if (!hand.Idle)
        //                return;

        //            // Get spell settings
        //            SpellSettings settings = (sender as Button).Tag as SpellSettings;

        //            // Add to spells
        //            Spell spell = Spell.Create(this, settings);

        //            if (spell != null)
        //            {
        //                // Cast the spell
        //                hand.Cast(spell);
        //            }
        //        });
        //    }
        //    #endregion

        //    ResetScrollPanelElements();
        //}

        //public void ClearScrollPanelElements()
        //{
        //    scrollPanelRestored = false;
        //    addedToScrollPanel.Clear();
        //}

        //public void AddScrollPanelElement(UIElement element)
        //{
        //    scrollPanelRestored = false;
        //    addedToScrollPanel.Add(element);
        //}

        //public void ResetScrollPanelElements()
        //{
        //    if (!scrollPanelRestored)
        //    {
        //        ClearScrollPanelElements();

        //        AddScrollPanelElement(buildButton);

        //        foreach (Button button in spellButtons)
        //            AddScrollPanelElement(button);

        //        scrollPanelRestored = true;
        //    }
        //}

        //void buildButton_Click(object sender, EventArgs e)
        //{
        //    foreach (Button button in buildingButtons)
        //        AddScrollPanelElement(button);
        //}
        //#endregion

        //#region Update and Draw

        public void UpdateUI(GameTime gameTime)
        {
            //ui.Update(gameTime);
        }

        void DrawUI(GameTime gameTime)
        {
            //// Draw UI
            //ui.Draw(gameTime);

            //if (entityManager.Selected != null)
            //    entityManager.Selected.DrawStatus(
            //        UIDisplay.GetRelativeRectangle(
            //            statusRectangle, ui, ScaleMode.ScaleY, Anchor.BottomLeft));
        }
        //#endregion

        #region IGameUI
        /// <summary>
        /// Called when an entity is selected. Game UI should refresh
        /// itself to match the new entities, e.g., status and spells.
        /// </summary>
        public void Select(Entity entity)
        {
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
