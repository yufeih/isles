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
using Isles.Graphics;
using Isles.UI;

namespace Isles.Engine
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
        /// Current level
        /// </summary>
        ILevel currentLevel;

        /// <summary>
        /// Loading bar
        /// </summary>
        Loading loadContext;

        /// <summary>
        /// Graphcis device
        /// </summary>
        GraphicsDeviceManager graphics;

        /// <summary>
        /// Cursor position in 3D space
        /// </summary>
        Vector3 cursorPosition;

        /// <summary>
        /// Cursor radius from eye in 3D space
        /// </summary>
        float cursorDistance = 500.0f;

        /// <summary>
        /// Only one spell can be casted at a time
        /// </summary>
        Spell currentSpell;
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

        /// <summary>
        /// Gets current level
        /// </summary>
        public ILevel CurrentLevel
        {
            get { return currentLevel; }
        }

        /// <summary>
        /// Gets game cursor position in 3D space
        /// </summary>
        public Vector3 CursorPosition
        {
            get { return cursorPosition; }
        }

        /// <summary>
        /// Gets the distance from cursor to eye in 3D space
        /// </summary>
        public float CursorDistance
        {
            get { return cursorDistance; }
        }

        /// <summary>
        /// Gets or sets current spell
        /// </summary>
        public Spell CurrentSpell
        {
            get { return currentSpell; }
            set { currentSpell = value; }
        }

        /// <summary>
        /// Each level has a unique name stored in this dictionary
        /// </summary>
        Dictionary<string, ILevel> LevelDictionary = new Dictionary<string, ILevel>();
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

            loadContext.Refresh(0, "Unloading...");

            // Unload current level
            if (currentLevel != null)
                currentLevel.Unload();

            // Reset everything
            Reset();

            loadContext.Refresh(0, "Loading...");

            // Read XML scene content
            XmlDocument doc = new XmlDocument();
            doc.Load(levelFile);

            if (doc.DocumentElement.Name != "World")
                throw new Exception("Invalid world format.");

            // Load game world
            world = new GameWorld();
            world.Load(doc.DocumentElement, loadContext);
            
            // Find ILevel from level attribute
            ILevel newLevel;
            string levelName = doc.DocumentElement.Attributes["Level"].InnerText;

            if (levelName != null && LevelDictionary.ContainsKey(levelName))
                newLevel = LevelDictionary[levelName];
            else
                newLevel = new Level();

            // Load new level
            newLevel.Load(this, loadContext);

            // Set new level
            currentLevel = newLevel;

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

            // Initialize hand
            hand = new Hand(this);

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
            if (currentLevel != null)
                currentLevel.Unload();
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

            // Update level
            if (currentLevel != null)
                currentLevel.Update(gameTime);

            // Update world
            if (world != null)
                world.Update(gameTime);

            // Update 3D cursor
            UpdateCursor();

            // Update our big hand
            if (hand != null)
                hand.Update(gameTime);

            // Update current spell
            if (currentSpell != null)
                currentSpell.Update(gameTime);
        }

        private void UpdateCursor()
        {
            // Update game cursor position
            Nullable<Vector3> hitPoint = world.Landscape.Intersects(game.PickRay);
            if (hitPoint.HasValue)
            {
                cursorPosition = hitPoint.Value;
                cursorDistance = Vector3.Subtract(
                    hitPoint.Value, game.PickRay.Position).Length();
            }
            else
            {
                cursorPosition = game.PickRay.Position +
                                 game.PickRay.Direction * cursorDistance;
            }
        }

        /// <summary>
        /// Handle game draw event
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            // Draw game world
            world.Draw(gameTime);

            // Draw current spell
            if (currentSpell != null)
                currentSpell.Draw(gameTime);

            //DrawGridOwner();
            //DrawGameStates();

            // Draw level
            if (currentLevel != null)
                currentLevel.Draw(gameTime);

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
