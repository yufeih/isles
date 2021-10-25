// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Isles.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Control = System.Windows.Forms.Control;
using Cursor = System.Windows.Forms.Cursor;

namespace Isles.Engine
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class BaseGame : Game, IEventListener
    {
        /// <summary>
        /// Windows cursor.
        /// </summary>
        private Cursor cursor;

        /// <summary>
        /// Background color used to clear the scene.
        /// </summary>
        private Color backgroundColor = Color.Black;// new Color(47, 62, 97);

        /// <summary>
        /// Cached matrices of this frame.
        /// </summary>
        private Matrix view;
        private Matrix projection;
        private Matrix viewProjection;
        private Matrix viewInverse;
        private Matrix projectionInverse;
        private Matrix viewProjectionInverse;

        /// <summary>
        /// Ray casted from cursor.
        /// </summary>
        private Ray pickRay;

        /// <summary>
        /// Eye position of this frame.
        /// </summary>
        private Vector3 eye;

        /// <summary>
        /// Facing direction of this frame.
        /// </summary>
        private Vector3 facing;

        /// <summary>
        /// Gets or sets windows cursor.
        /// </summary>
        public Cursor Cursor
        {
            get => cursor;
            set
            {
                var control = Control.FromHandle(Window.Handle);
                control.Cursor = cursor;
                cursor = value;
            }
        }

        /// <summary>
        /// Gets game input.
        /// </summary>
        public Input Input { get; private set; }

        /// <summary>
        /// Gets game sound.
        /// </summary>
        public AudioManager Audio { get; private set; }

        /// <summary>
        /// Gets current game screen.
        /// </summary>
        public IScreen CurrentScreen { get; private set; }

        /// <summary>
        /// Gets Game camera.
        /// </summary>
        public ICamera Camera { get; set; }

        /// <summary>
        /// Gets view matrix.
        /// </summary>
        public Matrix View => view;

        /// <summary>
        /// Gets projection matrix.
        /// </summary>
        public Matrix Projection => projection;

        /// <summary>
        /// Gets view projection matrix.
        /// </summary>
        public Matrix ViewProjection => viewProjection;

        /// <summary>
        /// Gets view inverse matrix.
        /// </summary>
        public Matrix ViewInverse => viewInverse;

        /// <summary>
        /// Gets projection inverse matrix.
        /// </summary>
        public Matrix ProjectionInverse => projectionInverse;

        /// <summary>
        /// Gets view projection inverse matrix.
        /// </summary>
        public Matrix ViewProjectionInverse => viewProjectionInverse;

        /// <summary>
        /// Gets current view frustum.
        /// </summary>
        public BoundingFrustum ViewFrustum { get; private set; }

        /// <summary>
        /// Gets the ray casted from current cursor position.
        /// </summary>
        public Ray PickRay => pickRay;

        /// <summary>
        /// Gets the eye position of this frame.
        /// </summary>
        public Vector3 Eye => eye;

        /// <summary>
        /// Gets the facing direction of this frame.
        /// </summary>
        public Vector3 Facing => facing;

        /// <summary>
        /// Gets or sets Game Settings.
        /// </summary>
        public Settings Settings { get; set; }

        public Color BackgroundColor
        {
            get => backgroundColor;
            set => backgroundColor = value;
        }

        /// <summary>
        /// Gets Xna graphics device manager.
        /// </summary>
        public GraphicsDeviceManager Graphics { get; }

        /// <summary>
        /// Gets screen width.
        /// </summary>
        public int ScreenWidth { get; private set; }

        /// <summary>
        /// Gets screen height.
        /// </summary>
        public int ScreenHeight { get; private set; }

        /// <summary>
        /// Gets game billboard manager.
        /// </summary>
        public BillboardManager Billboard { get; private set; }

        /// <summary>
        /// Gets game model manager.
        /// </summary>
        public ModelManager ModelManager { get; private set; }

        /// <summary>
        /// Gets game 2D graphics.
        /// </summary>
        public Graphics2D Graphics2D { get; private set; }

        /// <summary>
        /// Gets all game screens.
        /// </summary>
        public Dictionary<string, IScreen> Screens { get; } = new();

        /// <summary>
        /// Gets current game time.
        /// </summary>
        public GameTime CurrentGameTime { get; private set; }

        /// <summary>
        /// Gets game shadow effect.
        /// </summary>
        public ShadowEffect Shadow { get; private set; }

        /// <summary>
        /// Gets game bloom effect.
        /// </summary>
        public BloomEffect Bloom { get; private set; }

        /// <summary>
        /// Gets whether the game is been paused.
        /// </summary>
        /// TODO: Fixe issues caused by pausing. (E.g., Timer)
        public bool Paused { get; set; }

        /// <summary>
        /// Starts a game screen and run.
        /// </summary>
        /// <param name="gameScreen"></param>
        public void Run(string screenName)
        {
            Run(Screens[screenName]);
        }

        /// <summary>
        /// Starts a game screen and run.
        /// </summary>
        /// <param name="gameScreen"></param>
        public void Run(IScreen screen)
        {
            // Sets game screen
            StartScreen(screen);

            // Run the application
            Run();
        }

        /// <summary>
        /// Starts a game screen.
        /// </summary>
        public void StartScreen(string screenName)
        {
            StartScreen(Screens[screenName]);
        }

        /// <summary>
        /// Starts a game screen.
        /// </summary>
        public void StartScreen(IScreen newScreen)
        {
            if (newScreen != CurrentScreen)
            {
                // Leave current screen
                if (CurrentScreen != null)
                {
                    CurrentScreen.Leave();
                }

                // Enter the new screen
                if (newScreen != null)
                {
                    newScreen.Enter();
                }

                // Set current screen
                CurrentScreen = newScreen;
            }
        }

        /// <summary>
        /// Adds a screen to the game.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="screen"></param>
        public void AddScreen(string name, IScreen screen)
        {
            if (!Screens.ContainsKey(name))
            {
                Screens.Add(name, screen);
                Log.Write("Screen Added: " + name);
            }
        }

        /// <summary>
        /// Adds a screen to the game.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="screen"></param>
        public void RemoveScreen(string name)
        {
            if (Screens.Remove(name))
            {
                Log.Write("Screen Removed: " + name);
            }
        }

        public BaseGame()
        {
            Singleton = this;

            var assembly = Assembly.GetCallingAssembly();

            // Initialize log
            Log.Initialize();
            Log.NewLine();
            Log.NewLine();
            Log.Write("Isles", false);
            Log.Write("Date: " + DateTime.Now, false);
            Log.Write("Full Name: " + assembly.FullName, false);
            Log.Write("CLR Runtime Version: " + assembly.ImageRuntimeVersion, false);
            Log.NewLine();

            Settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText("data/settings/settings.json"));

            Content = new ContentManager(Services, Settings.ContentDirectory);
            Log.Write("Content Directory:" + Settings.ContentDirectory + "...");

            Graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = Settings.Fullscreen,
                PreferredBackBufferWidth = Settings.ScreenWidth,
                PreferredBackBufferHeight = Settings.ScreenHeight,
                SynchronizeWithVerticalRetrace = Settings.VSync,
            };

            // Show cursor
            IsMouseVisible = Settings.IsMouseVisible;
            IsFixedTimeStep = Settings.IsFixedTimeStep;
        }

        /// <summary>
        /// Gets the singleton instance of base game.
        /// </summary>
        /// <remarks>
        /// This is not a strict implementation of 'Singleton'. The constructor is not private.
        /// since we want to allow real games to derive from this class, so the singleton is
        /// actually the current game. Typically an app only creates 1 game instance, that won't
        /// be a problem most of the time.
        /// </remarks>
        public static BaseGame Singleton { get; private set; }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Graphics.DeviceReset += graphics_DeviceReset;
            graphics_DeviceReset(null, EventArgs.Empty);

            // Initialize sound
            Input = new Input();
            Input.Register(this, 0);

            Components.Add(Audio = new AudioManager(this));

            if (Settings.BloomSettings != null &&
                Settings.BloomSettings.Enabled)
            {
                Bloom = new BloomEffect(this, Content)
                {
                    Settings = new BloomSettings(
                    Settings.BloomSettings.Type,
                    Settings.BloomSettings.Threshold,
                    Settings.BloomSettings.Blur,
                    Settings.BloomSettings.BloomIntensity,
                    Settings.BloomSettings.BaseIntensity,
                    Settings.BloomSettings.BloomSaturation,
                    Settings.BloomSettings.BaseSaturation),
                };

                Components.Add(Bloom);
                Log.Write("Bloom Effect Initialized...");
            }

            ParticleSystem.LoadContent(this);
            Log.Write("Particle System Initialized...");

            // Initialize text
            Graphics2D = new Graphics2D(this);
            Log.Write("2D Graphics Initialized...");

            Billboard = new BillboardManager(this);
            Log.Write("Billboard Initialized...");

            if (Settings.ShadowEnabled)
            {
                Shadow = new ShadowEffect(this);
                Log.Write("Shadow Mapping Effect Initialized...");
            }

            // Notify all screens to load contents
            foreach (KeyValuePair<string, IScreen> screen in Screens)
            {
                screen.Value.LoadContent();
            }

            ModelManager = new ModelManager();
            Log.Write("Model Manager Initialized...");

            base.Initialize();
        }

        private void graphics_DeviceReset(object sender, EventArgs e)
        {
            ScreenWidth = GraphicsDevice.Viewport.Width;
            ScreenHeight = GraphicsDevice.Viewport.Height;

            Log.Write("Device Reset <" + ScreenWidth + ", " + ScreenHeight + ">...");
        }

        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        protected override void LoadContent()
        {
            base.LoadContent();
        }

        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        /// <param name="unloadAllContent">Which type of content to unload.</param>
        protected override void UnloadContent()
        {
            // Notify all screens to unload contents
            foreach (KeyValuePair<string, IScreen> screen in Screens)
            {
                screen.Value.UnloadContent();
            }

            // Content.Unload();
            base.UnloadContent();
        }

        private bool initialized;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Store game time
            CurrentGameTime = gameTime;

            // Update input
            Input.Update(gameTime);

            // Do not update other stuff when the game is paused
            if (Paused)
            {
                return;
            }

            // Update events
            Event.Update(gameTime);

            // Update timer
            Timer.Update(gameTime);

            if (Camera != null)
            {
                // Update camera
                Camera.Update(gameTime);

                // Update matrices
                UpdateMatrices();

                // Update view frustum
                UpdateFrustum();

                // Update ray from cursor
                UpdatePickRay();

                // Update audio listener
                UpdateAudioListener();
            }

            // Update current screen
            if (CurrentScreen != null)
            {
                CurrentScreen.Update(gameTime);
            }

            // Update particle system
            ParticleSystem.UpdateAll(gameTime);

            // Clip cursor
            if (Settings.ClipCursor && IsActive)
            {
                Cursor.Clip = new System.Drawing.Rectangle(
                    Window.ClientBounds.X, Window.ClientBounds.Y,
                    Window.ClientBounds.Width, Window.ClientBounds.Height);
            }

            // Tell me why I have to set this every frame...
            Cursor = cursor;

            base.Update(gameTime);
        }

        private void UpdateFrustum()
        {
            ViewFrustum = new BoundingFrustum(viewProjection);
        }

        private void UpdateAudioListener()
        {
            if (Audio != null)
            {
                Audio.Listener.Position = eye;
                Audio.Listener.Forward = facing;

                // Trick! we assume the camera is always facing upwards
                Audio.Listener.Up = Vector3.UnitZ;

                // Camera velocity is ignored
                Audio.Listener.Velocity = Vector3.Zero;
            }
        }

        protected virtual void FirstTimeInitialize()
        {
        }

        private void UpdatePickRay()
        {
            MouseState mouseState = Mouse.GetState();

            Vector3 v;
            v.X = (2.0f * mouseState.X / ScreenWidth) - 1;
            v.Y = -((2.0f * mouseState.Y / ScreenHeight) - 1);
            v.Z = 0.0f;

            pickRay.Position.X = viewInverse.M41;
            pickRay.Position.Y = viewInverse.M42;
            pickRay.Position.Z = viewInverse.M43;
            pickRay.Direction = Vector3.Normalize(
                Vector3.Transform(v, viewProjectionInverse) - pickRay.Position);
        }

        /// <summary>
        /// Unproject a point on the screen to a ray in the 3D world.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Ray Unproject(int x, int y)
        {
            Ray ray;

            Vector3 v;
            v.X = (2.0f * x / ScreenWidth) - 1;
            v.Y = -((2.0f * y / ScreenHeight) - 1);
            v.Z = 0.0f;

            ray.Position.X = viewInverse.M41;
            ray.Position.Y = viewInverse.M42;
            ray.Position.Z = viewInverse.M43;
            ray.Direction = Vector3.Normalize(
                Vector3.Transform(v, viewProjectionInverse) - ray.Position);

            return ray;
        }

        /// <summary>
        /// Project a point in 3D world space to 2D screen space.
        /// </summary>
        public Point Project(Vector3 position)
        {
            var hPosition = Vector4.Transform(position, viewProjection);
            hPosition.X /= hPosition.W;
            hPosition.Y /= hPosition.W;

            Point screenPosition;
            screenPosition.X = (int)(0.5f * (hPosition.X + 1) * ScreenWidth);
            screenPosition.Y = (int)(0.5f * (-hPosition.Y + 1) * ScreenHeight);
            return screenPosition;
        }

        /// <summary>
        /// Update view/projection matrices.
        /// </summary>
        private void UpdateMatrices()
        {
            if (Camera != null)
            {
                view = Camera.View;
                projection = Camera.Projection;
                viewProjection = view * projection;
                viewInverse = Matrix.Invert(view);
                projectionInverse = Matrix.Invert(projection);
                // viewProjectionInverse = Matrix.Invert(viewProjection);

                // Guess this is more accurate
                viewProjectionInverse = projectionInverse * ViewInverse;

                // Update eye / facing / right
                eye.X = viewInverse.M41;
                eye.Y = viewInverse.M42;
                eye.Z = viewInverse.M43;

                facing.X = -view.M13;
                facing.Y = -view.M23;
                facing.Z = -view.M33;
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Invoke first time initialize
            if (!initialized)
            {
                initialized = true;
                FirstTimeInitialize();

                // Delay one frame
                return;
            }

            Bloom?.BeginDraw();
            Graphics.GraphicsDevice.Clear(backgroundColor);
            CurrentScreen?.Draw(gameTime);
            ModelManager?.Present();
            Billboard?.Present();

            ParticleSystem.Present();

            Graphics2D.Present();

            base.Draw(gameTime);
        }

        public EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (CurrentScreen != null &&
                CurrentScreen.HandleEvent(type, sender, tag) == EventResult.Handled)
            {
                return EventResult.Handled;
            }

            return Camera != null &&
                Camera.HandleEvent(type, sender, tag) == EventResult.Handled
                ? EventResult.Handled
                : EventResult.Unhandled;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Billboard?.Dispose();
                Shadow?.Dispose();

                // Notify all screens to unload contents
                foreach (var screen in Screens)
                {
                    screen.Value.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
