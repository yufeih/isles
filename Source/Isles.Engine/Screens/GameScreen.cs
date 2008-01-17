//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
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
        /// Game camera for demo level
        /// </summary>
        GameCamera gameCamera;

        /// <summary>
        /// Game landscape
        /// </summary>
        Landscape landscape;

        /// <summary>
        /// Default game light
        /// </summary>
        ShadowMapEffect shadow;

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
        /// Content for game screen.
        /// Resources are created during game load and destroyed
        /// until game exits
        /// </summary>
        ContentManager content;

        /// <summary>
        /// Content manager for a level
        /// Resources created using this content manager are destroyed
        /// when the level exits.
        /// </summary>
        ContentManager levelContent;

        /// <summary>
        /// Game entity manager
        /// </summary>
        EntityManager entityManager;

        /// <summary>
        /// Entity picked this frame
        /// </summary>
        Entity pickedEntity;

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
        /// Gets game landscape
        /// </summary>
        public Landscape Landscape
        {
            get { return landscape; }
        }

        /// <summary>
        /// Gets game screen content manager
        /// </summary>
        public ContentManager Content
        {
            get { return content; }
        }

        /// <summary>
        /// Gets game screen level content manager
        /// </summary>
        public ContentManager LevelContent
        {
            get { return levelContent; }
        }

        /// <summary>
        /// Gets game entity manager
        /// </summary>
        public EntityManager EntityManager
        {
            get { return entityManager; }
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
        /// Gets the default light source used for shadow mapping
        /// </summary>
        public ShadowMapEffect Shadow
        {
            get { return shadow; }
        }
        #endregion

        #region Initialization

        public GameScreen(BaseGame game)
        {
            this.game = game;
            this.graphics = game.Graphics;
            this.content = game.Content;
            this.levelContent = new ContentManager(game.Services);
            this.levelContent.RootDirectory = content.RootDirectory;
        }

        /// <summary>
        /// Starts a new level
        /// </summary>
        /// <param name="newLevel"></param>
        public void StartLevel(ILevel newLevel)
        {
            // Reset loading context
            loadContext.Reset();

            loadContext.Refresh(0, "Unloading...");

            // Unload current level
            if (currentLevel != null)
                currentLevel.Unload();

            // Unload level content
            levelContent.Unload();

            // Reset everything
            Reset();

            loadContext.Refresh(0, "Loading...");

            // Load new level
            newLevel.Load(this, loadContext);

            // Set new landscape
            landscape = newLevel.Landscape;

            // Set new level
            currentLevel = newLevel;

            // Initialize camera
            gameCamera = new GameCamera(this);
            gameCamera.FlyTo(new Vector3(landscape.Width / 2, landscape.Depth / 2, 0), true);
            gameCamera.SpaceBounds = new BoundingBox(Vector3.Zero,
                new Vector3(landscape.Width, landscape.Depth, 6 * landscape.Height));

            // Set new camera
            game.Camera = gameCamera;

            // Setup user interface
            ResetUserInterface();

            InitializeGameLogic();
        }

        /// <summary>
        /// Reset everything
        /// </summary>
        public void Reset()
        {
            entityManager.Reset();
            hand.Reset();

            Wood = 0;
            Gold = 0;
            Food = 0;

            Dependencies.Clear();
            functions.Clear();
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

            // Initialize eneity manager
            entityManager = new EntityManager(this);

            // Initialize hand
            hand = new Hand(this);

            // Initialize UI
            ui = new UIDisplay(game);
            iconTexture = content.Load<Texture2D>("UI/Icons");

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

        /// <summary>
        /// Pick a game entity from the cursor
        /// </summary>
        /// <returns></returns>
        public Entity Pick()
        {
            if (pickedEntity != null)
                return pickedEntity;

            // Cache the result
            return pickedEntity = Pick(game.PickRay);
        }

        /// <summary>
        /// Pick grid offset
        /// </summary>
        readonly Point[] PickGridOffset = new Point[9]
        {
            new Point(-1, -1), new Point(0, -1), new Point(1, -1),
            new Point(-1, 0) , new Point(0, 0) , new Point(1, 0) ,
            new Point(-1, 1) , new Point(0, 1) , new Point(1, 1) ,
        };

        /// <summary>
        /// Pick a game entity from the given gay
        /// </summary>
        /// <returns></returns>
        public Entity Pick(Ray ray)
        {
            // This value affects how accurate this algorithm works.
            // Basically, a sample point starts at the origion of the
            // pick ray, it's position incremented along the direction
            // of the ray each step with a value of PickPrecision.
            // A pick precision of half the grid size is good, since
            // each grid has at most one game entity.
            const float PickPrecision = 5.0f;

            // This is the bounding box for all game entities
            BoundingBox boundingBox = landscape.TerrainBoundingBox;
            boundingBox.Max.Z += Entity.MaxHeight;

            // Nothing will be picked if the ray doesn't even intersects
            // with the bounding box of all grids
            Nullable<float> result = ray.Intersects(boundingBox);
            if (!result.HasValue)
                return null;

            // Initialize the sample point
            Vector3 step = ray.Direction * PickPrecision;
            Vector3 sampler = ray.Position + ray.Direction * result.Value;

            // Keep track of the grid visited previously, so that we can
            // avoid checking the same grid.
            Point previousGrid = new Point(-1, -1);

            while ( // Stop probing if we're outside the box
                boundingBox.Contains(sampler) == ContainmentType.Contains)
            {
                // Project to XY plane and get which grid we're in
                Point grid = landscape.PositionToGrid(sampler.X, sampler.Y);

                // If we hit the ground, nothing is picked
                if (landscape.HeightField[grid.X, grid.Y] > sampler.Z)
                    return null;

                // Check the grid visited previously
                if (grid.X != previousGrid.X || grid.Y != previousGrid.Y)
                {
                    // Check the 9 adjacent grids in case we miss the some
                    // entities like trees (Trees are big at the top but are
                    // small at the bottom).
                    // Also find the minimum distance from the entity to the
                    // pick ray position to make the pick correct

                    Point pt;
                    float shortest = 10000;
                    Entity pickEntity = null;

                    for (int i = 0; i < PickGridOffset.Length; i++)
                    {
                        pt.X = grid.X + PickGridOffset[i].X;
                        pt.Y = grid.Y + PickGridOffset[i].Y;

                        if (landscape.IsValidGrid(pt))
                        {
                            foreach (Entity entity in landscape.Data[pt.X, pt.Y].Owners)
                            {
                                Nullable<float> value = entity.Intersects(ray);

                                if (value.HasValue && value.Value < shortest)
                                {
                                    shortest = value.Value;
                                    pickEntity = entity;
                                }
                            }
                        }
                    }

                    if (pickEntity != null)
                        return pickEntity;

                    previousGrid = grid;
                }

                // Sample next position
                sampler += step;
            }

            return null;
        }        
        #endregion

        #region Update and Draw

        /// <summary>
        /// Handle game updates
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Set picked entity to null
            pickedEntity = null;
            
            // Update UI first
            UpdateUI(gameTime);

            // Update level
            if (currentLevel != null)
                currentLevel.Update(gameTime);

            // Update landscape
            if (landscape != null)
                landscape.Update(gameTime);

            // Update 3D cursor
            UpdateCursor();

            // Update our big hand
            if (hand != null)
                hand.Update(gameTime);

            // Update current spell
            if (currentSpell != null)
                currentSpell.Update(gameTime);

            // Update all game entites
            entityManager.Update(gameTime);
        }

        private void UpdateCursor()
        {
            // Update game cursor position
            Nullable<Vector3> hitPoint = landscape.Intersects(game.PickRay);
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
            //GenerateShadowMap(gameTime);

            // Draw shadow receivers using a special shadow mapping pixel shader
            //landscape.Draw(gameTime, shadow.ViewProjection, shadow.ShadowMap);
            landscape.Draw(gameTime);

            // Draw all game entities
            entityManager.Draw(gameTime);

            // Draw current spell
            if (currentSpell != null)
                currentSpell.Draw(gameTime);

            //DrawGridOwner();
            DrawGameStates();

            // Draw level
            if (currentLevel != null)
                currentLevel.Draw(gameTime);

            // Force all billboards to be drawed before our UI is rendered
            game.Billboard.Present(gameTime);

            // Force all point sprites to be drawed
            game.PointSprite.Present(gameTime);
            
            DrawUI(gameTime);

            // Draw god's hand at last
            if (hand != null)
                hand.Draw(gameTime);
        }

        private void GenerateShadowMap(GameTime gameTime)
        {
            shadow.Position = new Vector3(0, 0, 100);

            // Generate shadow maps first
            shadow.Begin();

            shadow.Effect.Parameters["ViewProjection"].SetValue(shadow.ViewProjection);

            //shadow.Effect.Begin();

            //foreach (EffectPass pass in shadow.Effect.CurrentTechnique.Passes)
            {
                //pass.Begin();

                // Only entities cast shadows
                entityManager.GenerateShadows(gameTime, shadow.Effect);

                //pass.End();
            }

            //shadow.Effect.End();
            shadow.End();
        }

        private void DrawGridOwner()
        {
            for (int y = 0; y < landscape.GridRowCount; y++)
                for (int x = 0; x < landscape.GridColumnCount; x++)
                {
                    // FIXME: Not implemented
                    //if (landscape.Data[x, y].Owners != null)
                    //    Text.DrawString(landscape.Data[x, y].Owners.Name,
                    //        14, new Vector3(landscape.GridToPosition(x, y), landscape.HeightField[x, y]),
                    //        Color.White);
                }
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
                if (landscape != null)
                    landscape.Dispose();

                if (shadow != null)
                    shadow.Dispose();

                if (ui != null)
                    ui.Dispose();
            }
        }

        #endregion
    }
}
