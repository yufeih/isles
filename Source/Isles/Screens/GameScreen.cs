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
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;


namespace Isles
{
    /// <summary>
    /// Represents a game screen
    /// </summary>
    public class GameScreen : IScreen
    {
        #region Variables
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
        /// Game screen UI
        /// </summary>
        GameUI ui;

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
        /// <summary>
        /// Creates a new game screen.
        /// NOTE: You can only create a game screen after graphics device
        /// is created and initialized.
        /// </summary>
        public GameScreen()
        {
            game = BaseGame.Singleton;
            graphics = game.Graphics;

            if (graphics == null)
                throw new InvalidOperationException();

            loadContext = new Loading(graphics.GraphicsDevice);

            ui = new GameUI(BaseGame.Singleton);

            hand = new Hand(null, "Models/Hand",
                Matrix.CreateScale(0.02f) *
                Matrix.CreateRotationX(MathHelper.ToRadians(30)) *
                Matrix.CreateRotationY(MathHelper.ToRadians(20)) *
                Matrix.CreateTranslation(0, -12, 0));
        }

        /// <summary>
        /// Starts a new level
        /// </summary>
        /// <param name="newLevel"></param>
        public void StartLevel(Stream levelFile)
        {
            // Reset loading context
            loadContext.Reset();

            loadContext.Refresh(0, "Loading...");

            // Read XML scene content
            XmlDocument doc = new XmlDocument();
            doc.Load(levelFile);

            // Load game world
            world = new GameWorld();
            world.Load(doc.DocumentElement, loadContext);
            world.SelectionTexture =
                BaseGame.Singleton.Content.Load<Texture2D>("Textures/SpellAreaOfEffect");
            world.UI = ui;

            // Reset everything
            Reset();

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
                hand.Reset(world);

            if (ui != null)
                ui.Reset(world, hand);
        }

        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        public void LoadContent() { }

        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        /// <param name="unloadAllContent">Which type of content to unload.</param>
        public void UnloadContent() { }
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
            if (ui != null)
                ui.Update(gameTime);

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
            if (world != null)
                world.Draw(gameTime);

            // Force all billboards to be drawed before our UI is rendered
            game.Billboard.Present(gameTime);

            // Force all point sprites to be drawed
            game.PointSprite.Present(gameTime);

            if (ui != null)
                ui.Draw(gameTime);

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

            }
        }
        #endregion
    }
}
