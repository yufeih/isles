//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;
using Isles.UI;

namespace Isles
{
    /// <summary>
    /// Represents a game screen
    /// </summary>
    public partial class GameScreen : IScreen
    {
        #region Variables
        /// <summary>
        /// In game user interface
        /// </summary>
        GameUI ui;

        /// <summary>
        /// In game camera
        /// </summary>
        GameCamera gameCamera;

        /// <summary>
        /// Game world
        /// </summary>
        GameWorld world;

        /// <summary>
        /// God's hand
        /// </summary>
        Hand hand;

        /// <summary>
        /// Base game
        /// </summary>
        BaseGame game;

        /// <summary>
        /// Loading bar
        /// </summary>
        Loading loadContext;

        /// <summary>
        /// Graphcis device
        /// </summary>
        GraphicsDeviceManager graphics;
        #endregion

        #region Properties
        /// <summary>
        /// Gets basic game instance
        /// </summary>
        public BaseGame Game
        {
            get { return game; }
        }

        /// <summary>
        /// Gets in game ui
        /// </summary>
        public GameUI UI
        {
            get { return ui; }
        }

        /// <summary>
        /// Gets game camera
        /// </summary>
        public GameCamera Camera
        {
            get { return gameCamera; }
        }

        /// <summary>
        /// Gets game world
        /// </summary>
        public GameWorld World
        {
            get { return world; }
        }

        /// <summary>
        /// Gets the god's hand
        /// </summary>
        public Hand BigHand
        {
            get { return hand; }
        }

        #endregion

        #region Initialization

        public GameScreen()
        {
            game = BaseGame.Singleton;
            graphics = game.Graphics;
            ui = new GameUI();
        }

        /// <summary>
        /// Starts a new level
        /// </summary>
        /// <param name="newLevel"></param>
        public void StartLevel(Stream levelFile)
        {
            // Reset loading context
            loadContext.Reset();

            // Reset everything
            Reset();

            loadContext.Refresh(0, "Loading...");

            // Read XML scene content
            XmlDocument doc = new XmlDocument();
            doc.Load(levelFile);

            // Load game world
            world = new GameWorld(doc.DocumentElement, loadContext);
            world.SelectionTexture =
                BaseGame.Singleton.Content.Load<Texture2D>("Textures/SpellAreaOfEffect");

            // Initialize hand
            hand = new Hand(world, "Models/Hand",
                Matrix.CreateScale(0.2f) *
                Matrix.CreateRotationX(MathHelper.ToRadians(30)) *
                Matrix.CreateRotationY(MathHelper.ToRadians(20)) *
                Matrix.CreateTranslation(0, -12, 0));

            // Initialize camera
            gameCamera = new GameCamera(world.Landscape);

            // Set new camera
            game.Camera = gameCamera;
        }

        /// <summary>
        /// Reset everything
        /// </summary>
        public void Reset()
        {
            if (hand != null)
                hand.Reset();

            if (world != null)
                world.Reset();
        }

        #region Graphics Content
        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        public void LoadContent()
        {
            // Initialize loading context
            loadContext = new Loading(graphics.GraphicsDevice);

            // Initialize UI
            ui = new GameUI();

            // Initialize shadow mapping
            //shadow = new ShadowMapEffect(game);
        }

        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        public void UnloadContent()
        {

        }
        #endregion

        #endregion

        #region Methods
        /// <summary>
        /// Called when this screen is activated
        /// </summary>
        public void Enter()
        {
        }

        /// <summary>
        /// Called when this screen is deactivated
        /// </summary>
        public void Leave()
        {
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Handle game updates
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {            
            // Update UI first
            //UpdateUI(gameTime);

            // Update world
            if (world != null)
                world.Update(gameTime);

            // Update our big hand
            if (hand != null)
                hand.Update(gameTime);
        }

        /// <summary>
        /// Handle game draw event
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            // Draw game world
            world.Draw(gameTime);

            // Force all billboards to be drawed before our UI is rendered
            game.Billboard.Present(gameTime);

            // Force all point sprites to be drawed
            game.PointSprite.Present(gameTime);
            
            //DrawUI(gameTime);

            // Draw god's hand at last
            if (hand != null)
                hand.Draw(gameTime);
        }
        #endregion

        #region Dispose
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
            if (disposing)
            {
                //if (ui != null)
                //    ui.Dispose();
            }
        }
        #endregion
    }
}
