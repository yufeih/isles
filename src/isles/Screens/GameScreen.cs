// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml;
using Isles.Engine;
using Isles.Graphics;
using Isles.Screens;
using Isles.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Isles
{
    public class GameScreen : IScreen, IEventListener
    {
        private readonly GraphicsDeviceManager graphics;

        public GameUI UI { get; private set; }

        private TipBox pausePanel;

        public BaseGame Game { get; }

        private bool paused;
        private IEventListener activeObject;

        private readonly Dictionary<string, BloomSettings> _bloomSettings = JsonSerializer.Deserialize<Dictionary<string, BloomSettings>>(
            File.ReadAllText("data/settings/bloom.json"));

        public void Pause(IEventListener activeObject)
        {
            this.activeObject = activeObject;
            paused = true;
            (Game.Camera as GameCamera).Freezed = true;
        }

        public void Resume()
        {
            paused = false;
            activeObject = null;
            (Game.Camera as GameCamera).Freezed = false;
        }

        public GameWorld World { get; private set; }

        private ReadmePanel readme;

        public Level Level { get; set; }

        public GameScreen(string levelName)
        {
            Game = BaseGame.Singleton;
            graphics = Game.Graphics;

            LoadWorld(levelName, new Skirmish(this, CreateTestPlayerInfo()));
        }

        private void LoadWorld(string levelFilename, Level level)
        {
            // Creates a default level if no input specified
            if (level == null)
            {
                level = new Level();
            }

            Level = level;

            // Hide cursor
            Game.IsMouseVisible = false;

            // Reset loading context
            ILoading loadContext = new Loading(graphics.GraphicsDevice);

            loadContext.Refresh(0);

            // Reset players
            Player.Reset();

            // Read XML scene content
            var doc = new XmlDocument();
            doc.Load($"data/levels/{levelFilename}");

            if (level != null)
            {
                level.Load(doc.DocumentElement, loadContext);
            }

            // Load game world
            World = new GameWorld();
            World.Load(doc.DocumentElement, loadContext);

            loadContext.Refresh(100);

            // Initialize camera
            ResetCamera();

            // Initialize game players & UI
            ResetUI();

            // Start level
            if (level != null)
            {
                level.Start(World);
            }

            // Load complete
            loadContext.Refresh(100);

            // Restore cursor
            Cursors.SetCursor(Cursors.Default);
            Game.IsMouseVisible = true;

            Event.SendMessage(EventType.Unknown, this, this, 1, 0.2f);
        }

        private void ResetUI()
        {
            UI = new GameUI(Game, World);

            readme = new ReadmePanel(new Rectangle(65, 70, 530, 460))
            {
                ScaleMode = ScaleMode.ScaleY,
                Anchor = Anchor.Center,
            };
            readme.OK.Click += (o, e) =>
            {
                readme.Visible = false;
                Audios.Play("OK");
                Resume();
            };
            readme.Visible = false;
            UI.Display.Add(readme);

            pausePanel = new TipBox(new Rectangle(Game.ScreenWidth * 3 / 10, Game.ScreenHeight * 3 / 8,
                                                  Game.ScreenWidth * 2 / 5, Game.ScreenHeight / 5));

            var quitOrNotText = new TextField("Are your sure you want to quit?", 20f / 23, Color.White,
                                            new Rectangle(0, 0, Game.ScreenWidth * 2 / 5, Game.ScreenHeight / 8));

            var ok = new TextButton("OK", 20f / 23, Color.Gold,
                                            new Rectangle(Game.ScreenWidth * 2 / 35,
                                                          Game.ScreenHeight / 8,
                                                          Game.ScreenWidth / 7,
                                                          Game.ScreenHeight * 2 / 35));

            var cancel = new TextButton("Cancel", 20f / 23, Color.Gold,
                                            new Rectangle(Game.ScreenWidth / 5,
                                                          Game.ScreenHeight / 8,
                                                          Game.ScreenWidth / 7,
                                                          Game.ScreenHeight * 2 / 35));
            ok.HotKey = Keys.Space;
            ok.Click += (o, e) =>
            {
                pausePanel.Visible = false;
                Resume();
                ReturnToTitleScreen();
            };

            cancel.HotKey = Keys.Escape;
            cancel.Click += (o, e) =>
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
            UI.Display.Add(pausePanel);
        }

        private bool scrollingCamera;

        private void ResetCamera()
        {
            var camera = new GameCamera(Game.Settings.CameraSettings, World);
            camera.FlyTo(new Vector3(Player.LocalPlayer.SpawnPoint, 0), true);
            Game.Camera = camera;

            camera.BeginMove += (sender, e) =>
            {
                Cursors.SetCursor(Cursors.Move);
                scrollingCamera = true;
            };
            camera.EndMove += (sender, e) =>
            {
                scrollingCamera = false;
                Cursors.SetCursor(Cursors.Default);
            };
            camera.BeginRotate += (sender, e) => Cursors.SetCursor(Cursors.Rotate);
            camera.EndRotate += (sender, e) => Cursors.SetCursor(Cursors.Default);
        }

        private IEnumerable<PlayerInfo> CreateTestPlayerInfo()
        {
            var info1 = new PlayerInfo
            {
                Name = Game.Settings.PlayerName,
                Team = 1,
                TeamColor = Color.Wheat,
                Type = PlayerType.Local,
            };

            yield return info1;

            var info2 = new PlayerInfo
            {
                Name = "Computer",
                Team = 2,
                TeamColor = Color.Green,
                Type = PlayerType.Computer,
                SpawnPoint = Vector2.Zero,
            };

            yield return info2;
        }

        private bool postScreen;
        private Texture2D victoryTexture;
        private Texture2D failureTexture;
        private Rectangle postScreenRectangle;
        private BloomSettings targetSettings;
        private BloomSettings defaultSettings;
        private float bloomLerpElapsedTime;

        public void ShowVictory()
        {
            if (!postScreen)
            {
                if (victoryTexture == null)
                {
                    victoryTexture = Game.TextureLoader.LoadTexture("data/ui/Victory.png");
                }

                // Load bloom
                if (Game.Bloom != null)
                {
                    targetSettings = _bloomSettings["Victory"];
                    bloomLerpElapsedTime = 0;
                    defaultSettings = Game.Bloom.Settings;
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
                    failureTexture = Game.TextureLoader.LoadTexture("data/ui/Failure.png");
                }

                if (Game.Bloom != null)
                {
                    targetSettings = _bloomSettings["Failed"];
                    bloomLerpElapsedTime = 0;
                    defaultSettings = Game.Bloom.Settings;
                }

                Audios.PlayMusic("Lose", false, 0, 0);
                EnterPostScreen(failureTexture);
            }
        }

        private void EnterPostScreen(Texture2D texture)
        {
            postScreenRectangle.Width = (int)(Game.ScreenWidth * 0.8f);
            postScreenRectangle.Height = (int)(1.0f * postScreenRectangle.Width /
                                               texture.Width * texture.Height);
            postScreenRectangle.X = (Game.ScreenWidth - postScreenRectangle.Width) / 2;
            postScreenRectangle.Y = (Game.ScreenHeight - postScreenRectangle.Height) / 2;

            Vector3? position = null;
            if (Building.LastDestroyedBuilding != null)
            {
                position = Building.LastDestroyedBuilding.Position;
            }

            if (Game.Camera is GameCamera)
            {
                (Game.Camera as GameCamera).Orbit(position, 0.00008f, 150, MathHelper.ToRadians(40));
            }

            Game.Input.Uncapture();
            postScreen = true;
            Game.IsMouseVisible = false;
        }

        /// <summary>
        /// Handle game updates.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Handle paused
            if (paused)
            {
                readme.Update(gameTime);
                pausePanel.Update(gameTime);
                Cursors.SetCursor(Cursors.Default);
                return;
            }

            UpdateCursorArrows();

            // Update UI first
            if (UI != null)
            {
                UI.Update(gameTime);
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
            if (World != null)
            {
                World.Update(gameTime);
            }

            // Update spells
            if (Spell.CurrentSpell != null)
            {
                Spell.CurrentSpell.UpdateCast(gameTime);
            }

            if (Level != null)
            {
                Level.Update(gameTime);
            }
        }

        private void UpdateCursorArrows()
        {
            if (scrollingCamera)
            {
                Point mouse = Game.Input.MousePosition;
                var border = (int)Game.Settings.CameraSettings.ScrollAreaSize;

                if (mouse.Y <= border)
                {
                    Cursors.SetCursor(Cursors.Top);
                }
                else if (mouse.Y >= Game.ScreenHeight - border)
                {
                    Cursors.SetCursor(Cursors.Bottom);
                }
                else if (mouse.X <= border)
                {
                    Cursors.SetCursor(Cursors.Left);
                }
                else if (mouse.X >= Game.ScreenWidth - border)
                {
                    Cursors.SetCursor(Cursors.Right);
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            // Draw game world
            World?.Draw(gameTime);

            // Draw player info
            foreach (var player in Player.AllPlayers)
            {
                player?.Draw(gameTime);
            }

            // Force everything to be presented before UI is rendered
            Game.Billboard.Present();
            Game.Graphics2D.Present();
            ParticleSystem.Present();

            if (postScreen)
            {
                // Draw post screen texture
                Game.Graphics2D.Sprite.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
                Game.Graphics2D.Sprite.Draw(victoryTexture ?? failureTexture, postScreenRectangle, Color.White);
                Game.Graphics2D.Sprite.End();

                // Lerp bloom settings
                if (Game.Bloom != null && targetSettings != null)
                {
                    const float BloomLerpTime = 3;

                    bloomLerpElapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    Game.Bloom.Settings = bloomLerpElapsedTime < BloomLerpTime
                        ? BloomSettings.Lerp(defaultSettings, targetSettings,
                            bloomLerpElapsedTime / BloomLerpTime)
                        : targetSettings;
                }
            }

            // Draw UI at last
            else if (UI != null)
            {
                UI.Draw(gameTime);
            }
        }

        public EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (type == EventType.Unknown && sender == this && (int)tag == 1)
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
            if (UI != null &&
                UI.HandleEvent(type, sender, tag) == EventResult.Handled)
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
            if (defaultSettings != null && Game.Bloom != null)
            {
                Game.Bloom.Settings = defaultSettings;
            }

            Game.IsMouseVisible = true;
            postScreen = false;
            Game.StartScreen(new TitleScreen());
        }
    }
}
