//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;
using Isles.UI;
using Isles.Screens;

namespace Isles
{
    /// <summary>
    /// Represents a game screen
    /// </summary>
    public class GameScreen : IScreen, IEventListener
    {
        #region Field
        private const string ReplayDirectory = "Replays";
        private const string DefaultReplayName = "LastReplay";
        private const string ReplayExtension = "ixr";

        /// <summary>
        /// Graphcis device
        /// </summary>
        private readonly GraphicsDeviceManager graphics;

        /// <summary>
        /// Game screen UI
        /// </summary>
        public GameUI UI => ui;

        private GameUI ui;
        private TipBox pausePanel;

        /// <summary>
        /// Gets basic game instance
        /// </summary>
        public BaseGame Game => game;

        private readonly BaseGame game;
        private bool paused = false;
        private IEventListener activeObject = null;

        public void Pause(IEventListener activeObject)
        {
            this.activeObject = activeObject;
            paused = true;
            (game.Camera as GameCamera).Freezed = true;
        }

        public void Resume()
        {
            paused = false;
            activeObject = null;
            (game.Camera as GameCamera).Freezed = false;
        }

        /// <summary>
        /// Gets game world
        /// </summary>
        public GameWorld World => world;

        private GameWorld world;

        /// <summary>
        /// The read me panel
        /// </summary>
        private ReadmePanel readme;

        /// <summary>
        /// Gets or sets game level
        /// </summary>
        public Level Level
        {
            get => level;
            set => level = value;
        }

        private Level level;

        /// <summary>
        /// Level filename
        /// </summary>
        private string levelFilename;

        /// <summary>
        /// Gets game recorder
        /// </summary>
        public GameRecorder Recorder => recorder;

        private GameRecorder recorder;

        /// <summary>
        /// Gets game replay
        /// </summary>
        public GameReplay Replay => replay;

        private GameReplay replay;

        /// <summary>
        /// Gets game server interface
        /// </summary>
        public GameServer Server => server;

        private GameServer server;
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
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Starts a new editor control
        /// </summary>
        public void StartEditor(System.Windows.Forms.Form editorForm)
        {
            GameWindow Window = game.Window;
            editorForm.Show(System.Windows.Forms.Control.FromHandle(Window.Handle));
            editorForm.Location = new System.Drawing.Point(
                Window.ClientBounds.X + Window.ClientBounds.Width / 2,
                Window.ClientBounds.Y + Window.ClientBounds.Height / 2);
            Log.Write("Editor Started: " + editorForm.Text);
        }

        /// <summary>
        /// Starts a new replay
        /// </summary>
        public void StartReplay(string replayFilename)
        {
            // Create a replay for test purpose
            using (Stream stream = new FileStream(DefaultReplayFilename, FileMode.Open))
            {
                replay = new GameReplay(game);
                replay.Load(stream);
            }

            LoadWorld(replay.WorldFilename, null);
        }

        /// <summary>
        /// Starts a new level
        /// </summary>
        /// <param name="newLevel"></param>
        public void StartLevel(string levelFilename)
        {
            LoadWorld(levelFilename, new Skirmish(this, CreateTestPlayerInfo()));
        }

        /// <summary>
        /// Internal load world method
        /// </summary>
        private void LoadWorld(string levelFilename, Level level)
        {

            // Creates a default level if no input specified
            if (level == null)
            {
                level = new Level();
            }

            this.levelFilename = levelFilename ?? throw new ArgumentNullException();
            this.level = level;

            // Reset game recorder
            recorder = new GameRecorder();

            // Load game world
            using (Stream levelFile = game.ZipContent.GetFileStream(levelFilename))
            {
                // Hide cursor
                game.IsMouseVisible = false;

                // Reset loading context
                ILoading loadContext = new Loading(graphics.GraphicsDevice, game.Graphics2D);

                loadContext.Refresh(0, "Loading...");

                // Reset players
                Player.Reset();

                // Read XML scene content
                var doc = new XmlDocument();
                doc.Load(levelFile);

                if (level != null)
                {
                    level.Load(doc.DocumentElement, loadContext);
                }

                // Load game world
                world = new GameWorld();
                world.Load(doc.DocumentElement, loadContext);

                // Set world
                server = new GameServer(world, recorder);

                loadContext.Refresh(100);

                // Initialize camera
                ResetCamera();

                // Initialize game players & UI
                ResetUI(loadContext);

                // Start level
                if (level != null)
                {
                    level.Start(world);
                }

                // Load complete
                loadContext.Refresh(100);

                // Restore cursor
                BaseGame.Singleton.Cursor = Cursors.Default;
                game.IsMouseVisible = true;

                Event.SendMessage(EventType.Unknown, this, this, 1, 0.2f);
            }            
        }

        private void ResetUI(ILoading loadContext)
        {
            ui = new GameUI(game, (loadContext as Loading).LoadingFinished, world);

            using (Stream readmeText = BaseGame.Singleton.ZipContent.GetFileStream("Content/Readme.txt"))
            {
                readme = new ReadmePanel(readmeText,
                                        new Rectangle(65, 70, 530, 460));
                readme.ScaleMode = ScaleMode.ScaleY;
                readme.Anchor = Anchor.Center;
                readme.OK.Click += delegate(object o, EventArgs e)
                {
                    readme.Visible = false;
                    Audios.Play("OK");
                    Resume();
                };
                readme.Visible = false;
                ui.Display.Add(readme);
            }

            pausePanel = new TipBox(new Rectangle(game.ScreenWidth * 3 / 10, game.ScreenHeight * 3 / 8,
                                                  game.ScreenWidth * 2 / 5, game.ScreenHeight / 5));

            var quitOrNotText = new TextField("Are your sure you want to quit?", 20f / 23, Color.White,
                                            new Rectangle(0, 0, game.ScreenWidth * 2 / 5, game.ScreenHeight / 8));

            var ok = new TextButton("OK", 20f / 23, Color.Gold,
                                            new Rectangle(game.ScreenWidth * 2 / 35,
                                                          game.ScreenHeight / 8,
                                                          game.ScreenWidth / 7,
                                                          game.ScreenHeight * 2 / 35));

            var cancel = new TextButton("Cancel", 20f / 23, Color.Gold,
                                            new Rectangle(game.ScreenWidth / 5,
                                                          game.ScreenHeight / 8,
                                                          game.ScreenWidth / 7,
                                                          game.ScreenHeight * 2 / 35));
            ok.HotKey = Keys.Space;
            ok.Click += delegate(object o, EventArgs e)
            {
                pausePanel.Visible = false;
                Resume();
                ReturnToTitleScreen();
            };

            cancel.HotKey = Keys.Escape;
            cancel.Click += delegate(object o, EventArgs e)
            {
                pausePanel.Visible = false;
                Resume();
            };

            quitOrNotText.Centered = true;
            pausePanel.Mask = true;

            pausePanel.Add(ok);
            pausePanel.Add(cancel);
            pausePanel.Add(quitOrNotText);
            pausePanel.Visible = false;
            ui.Display.Add(pausePanel);
        }

        private bool scrollingCamera = false;

        private void ResetCamera()
        {
            var camera = new GameCamera(game.Settings.CameraSettings, world);
            camera.FlyTo(new Vector3(Player.LocalPlayer.SpawnPoint, 0), true);
            game.Camera = camera;

            camera.BeginMove += new EventHandler(delegate(object sender, EventArgs e)
            {
                Cursors.StoredCursor = game.Cursor;
                game.Cursor = Cursors.Move;
                scrollingCamera = true;
            });
            camera.EndMove += new EventHandler(delegate(object sender, EventArgs e)
            {
                scrollingCamera = false;
                game.Cursor = Cursors.StoredCursor;
            });
            camera.BeginRotate += new EventHandler(delegate(object sender, EventArgs e)
            {
                Cursors.StoredCursor = game.Cursor;
                game.Cursor = Cursors.Rotate;
            });
            camera.EndRotate += new EventHandler(delegate(object sender, EventArgs e)
            {
                game.Cursor = Cursors.StoredCursor;
            });
        }

        private IEnumerable<PlayerInfo> CreateTestPlayerInfo()
        {
            var info1 = new PlayerInfo();

            info1.Name = game.Settings.PlayerName;
            info1.Race = (Race.Islander);
            info1.Team = 1;
            info1.TeamColor = Color.Wheat;
            info1.Type = PlayerType.Local;

            yield return info1;

            var info2 = new PlayerInfo();

            info2.Name = "Computer";
            info2.Race = (Race.Islander);
            info2.Team = 2;
            info2.TeamColor = Color.Green;
            info2.Type = PlayerType.Computer;
            info2.SpawnPoint = Vector2.Zero;

            yield return info2;
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

        private bool postScreen = false;
        private Texture2D victoryTexture;
        private Texture2D failureTexture;
        private Rectangle postScreenRectangle;
        private BloomSettings targetSettings;
        private BloomSettings defaultSettings;
        private float bloomLerpElapsedTime = 0;

        public void ShowVictory()
        {
            if (!postScreen)
            {
                if (victoryTexture == null)
                {
                    victoryTexture = game.ZipContent.Load<Texture2D>("Textures/Victory");
                }

                // Load bloom
                if (game.Bloom != null)
                {
                    targetSettings = LoadBloomSettings("Victory");
                    bloomLerpElapsedTime = 0;
                    defaultSettings = game.Bloom.Settings;
                }

                Audios.PlayMusic("Win", false, 0, 0);
                EnterPostScreen(victoryTexture);
            }
        }

        public void ShowDefeated()
        {
            if (!postScreen)
            {
                if (failureTexture == null)
                {
                    failureTexture = game.ZipContent.Load<Texture2D>("Textures/Failure");
                }

                if (game.Bloom != null)
                {
                    targetSettings = LoadBloomSettings("Failed");
                    bloomLerpElapsedTime = 0;
                    defaultSettings = game.Bloom.Settings;
                }

                Audios.PlayMusic("Lose", false, 0, 0);
                EnterPostScreen(failureTexture);
            }
        }

        private BloomSettings LoadBloomSettings(string type)
        {
            using (Stream stream = game.ZipContent.GetFileStream("Content/Settings/Bloom.xml"))
            {
                var settings = (List<BloomSettings>)
                    new XmlSerializer(typeof(List<BloomSettings>)).Deserialize(stream);

                foreach (BloomSettings bloom in settings)
                {
                    if (bloom.Name.Equals(type))
                    {
                        return bloom;
                    }
                }
            }

            return null;
        }

        private void EnterPostScreen(Texture2D texture)
        {
            postScreenRectangle.Width = (int)(game.ScreenWidth * 0.8f);
            postScreenRectangle.Height = (int)(1.0f * postScreenRectangle.Width /
                                               texture.Width * texture.Height);
            postScreenRectangle.X = (game.ScreenWidth - postScreenRectangle.Width) / 2;
            postScreenRectangle.Y = (game.ScreenHeight - postScreenRectangle.Height) / 2;

            Vector3? position = null;
            if (Building.LastDestroyedBuilding != null)
            {
                position = Building.LastDestroyedBuilding.Position;
            }

            if (game.Camera is GameCamera)
            {
                (game.Camera as GameCamera).Orbit(position, 0.00008f, 150, MathHelper.ToRadians(40));
            }

            game.Input.Uncapture();
            postScreen = true;
            game.IsMouseVisible = false;
        }

        #endregion

        #region Update and Draw
        /// <summary>
        /// Handle game updates
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Handle paused
            if (paused)
            {
                readme.Update(gameTime);
                pausePanel.Update(gameTime);
                game.Cursor = Cursors.Default;
                return;
            }

            UpdateCursorArrows();            

            // Update UI first
            if (ui != null)
            {
                ui.Update(gameTime);
            }

            // Update players
            foreach (Player player in Player.AllPlayers)
            {
                if (player != null)
                {
                    player.Update(gameTime);
                }
            }

            // Update world
            if (world != null)
            {
                world.Update(gameTime);
            }

            // Update spells
            if (Spell.CurrentSpell != null)
            {
                Spell.CurrentSpell.UpdateCast(gameTime);
            }

            if (level != null)
            {
                level.Update(gameTime);
            }

            if (replay != null)
            {
                replay.Update(gameTime);
            }

            // Update game server
            if (server != null)
            {
                server.Update(gameTime);
            }
        }

        private void UpdateCursorArrows()
        {
            if (scrollingCamera)
            {
                Point mouse = game.Input.MousePosition;
                var border = (int)(game.Settings.CameraSettings.ScrollAreaSize);

                if (mouse.Y <= border)
                {
                    game.Cursor = Cursors.Top;
                }
                else if (mouse.Y >= game.ScreenHeight - border)
                {
                    game.Cursor = Cursors.Bottom;
                }
                else if (mouse.X <= border)
                {
                    game.Cursor = Cursors.Left;
                }
                else if (mouse.X >= game.ScreenWidth - border)
                {
                    game.Cursor = Cursors.Right;
                }
            }
        }

        /// <summary>
        /// Handle game draw event
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            // Draw level info
            if (level != null)
            {
                level.Draw(gameTime);
            }

            // Draw game world
            if (world != null)
            {
                world.Draw(gameTime);
            }

            // Draw player info
            foreach (Player player in Player.AllPlayers)
            {
                if (player != null)
                {
                    player.Draw(gameTime);
                }
            }

            // Draw spell
            if (Spell.CurrentSpell != null)
            {
                Spell.CurrentSpell.Draw(gameTime);
            }

            // Force everything to be presented before UI is rendered
            game.ModelManager.Present(gameTime);
            game.Billboard.Present(gameTime);
            game.Graphics2D.Present();
            game.PointSprite.Present(gameTime);
            ParticleSystem.Present(gameTime);
            
            if (postScreen)
            {
                // Draw post screen texture
                game.Graphics2D.Sprite.Begin();
                game.Graphics2D.Sprite.Draw(victoryTexture != null ? victoryTexture : failureTexture,
                                            postScreenRectangle, Color.White);
                game.Graphics2D.Sprite.End();

                // Lerp bloom settings
                if (game.Bloom != null && targetSettings != null)
                {
                    const float BloomLerpTime = 3;

                    bloomLerpElapsedTime += (float)(gameTime.ElapsedGameTime.TotalSeconds);

                    if (bloomLerpElapsedTime < BloomLerpTime)
                    {
                        game.Bloom.Settings = BloomSettings.Lerp(defaultSettings, targetSettings,
                            bloomLerpElapsedTime / BloomLerpTime);
                    }
                    else
                    {
                        game.Bloom.Settings = targetSettings;
                    }
                }
            }
            // Draw UI at last
            else if (ui != null)
            {
                ui.Draw(gameTime);
            }
        }

        public EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (type == EventType.Unknown && sender == this &&(int)tag == 1)
            {
                readme.Visible = true;
                Pause(readme);
                return EventResult.Handled;
            }
            
            if (paused)
            {
                if (activeObject != null)
                {
                    activeObject.HandleEvent(type, sender, tag);
                }

                return EventResult.Handled;
            }
            // Prevent any event if we've in the post screen state
            if (postScreen)
            {
                if ((type == EventType.KeyDown && tag is Keys? &&
                    ((tag as Keys?).Value == Keys.Escape || (tag as Keys?).Value == Keys.Space ||
                    ((tag as Keys?).Value == Keys.Enter))) || type == EventType.LeftButtonDown)
                {
                    ReturnToTitleScreen();
                }

                return EventResult.Handled;
            }

            // Let spell handle event
            if (Spell.CurrentSpell != null &&
                Spell.CurrentSpell.HandleEvent(type, sender, tag) == EventResult.Handled)
            {
                return EventResult.Handled;
            }

            // Let UI handle event first
            if (ui != null &&
                ui.HandleEvent(type, sender, tag) == EventResult.Handled)
            {
                return EventResult.Handled;
            }
            // Let player handle event
            foreach (Player player in Player.AllPlayers)
            {
                if (player.HandleEvent(type, sender, tag) == EventResult.Handled)
                {
                    return EventResult.Handled;
                }
            }

            // Let level handle event
            if (Level != null &&
                Level.HandleEvent(type, sender, tag) == EventResult.Handled)
            {
                return EventResult.Handled;
            }

            if (type == EventType.KeyDown && tag is Keys? &&
                ((tag as Keys?).Value == Keys.Escape))
            {
                pausePanel.Visible = true;
                Pause(pausePanel);
                return EventResult.Handled;
            }

            return EventResult.Unhandled;
        }

        private void ReturnToTitleScreen()
        {
            // Return to main menu
            if (victoryTexture != null)
            {
                victoryTexture.Dispose();
                victoryTexture = null;
            }

            if (failureTexture != null)
            {
                failureTexture.Dispose();
                failureTexture = null;
            }

            if (defaultSettings != null && game.Bloom != null)
            {
                game.Bloom.Settings = defaultSettings;
            }

            game.IsMouseVisible = true;
            postScreen = false;
            game.StartScreen(new TitleScreen(this));

            // Save replay
            SaveReplay(DefaultReplayName);
        }

        private bool SaveReplay(string name)
        {
            try
            {
                if (recorder != null)
                {
                    // Make sure replays directory exists
                    if (Directory.Exists(ReplayDirectory) == false)
                    {
                        Directory.CreateDirectory(ReplayDirectory);
                    }

                    using (Stream replay = new FileStream(
                        ReplayDirectory + "/" + name + "." + ReplayExtension, FileMode.Create))
                    {
                        using (Stream world = game.ZipContent.GetFileStream(levelFilename))
                        {
                            recorder.Save(replay, levelFilename, world);
                        }
                    }
                }
            }
            // We don't wanna cause any exceptions when creating the replay file,
            // so just ignore all exceptions.
            catch (Exception e)
            {
                Log.Write(e.Message);
                return false;
            }

            return true;
        }

        public static string DefaultReplayFilename => ReplayDirectory + "/" + DefaultReplayName + "." + ReplayExtension;
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
