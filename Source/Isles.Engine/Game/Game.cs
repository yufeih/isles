//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

#region Using Statements
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Isles.Graphics;
using Cursor = System.Windows.Forms.Cursor;
using Control = System.Windows.Forms.Control;
#endregion

namespace Isles.Engine
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BaseGame : Game, IEventListener
    {
        #region Variables
        /// <summary>
        /// Windows cursor
        /// </summary>
        Cursor cursor;

        /// <summary>
        /// XNA graphics device manager
        /// </summary>
        GraphicsDeviceManager graphics;

        /// <summary>
        /// ZipContent manager
        /// </summary>
        ZipContentManager content;

        /// <summary>
        /// Background color used to clear the scene
        /// </summary>
        Color backgroundColor = Color.Black;// new Color(47, 62, 97);

        /// <summary>
        /// Game settings
        /// </summary>
        Settings settings;

        /// <summary>
        /// Game camera interface
        /// </summary>
        ICamera camera;

        /// <summary>
        /// Cached matrices of this frame
        /// </summary>
        Matrix
            view,
            projection,
            viewProjection,
            viewInverse,
            projectionInverse,
            viewProjectionInverse;

        /// <summary>
        /// view frustum of this frame
        /// </summary>
        BoundingFrustum viewFrustum;

        /// <summary>
        /// Ray casted from cursor
        /// </summary>
        Ray pickRay = new Ray();

        /// <summary>
        /// Eye position of this frame
        /// </summary>
        Vector3 eye;
        
        /// <summary>
        /// Facing direction of this frame
        /// </summary>
        Vector3 facing;

        /// <summary>
        /// Game profiler
        /// </summary>
        Profiler profiler = new Profiler();

        /// <summary>
        /// Game screenshot capturer
        /// </summary>
        ScreenshotCapturer screenshotCapturer;

        /// <summary>
        /// Current game screen
        /// </summary>
        IScreen currentScreen;

        /// <summary>
        /// All game screens
        /// </summary>
        Dictionary<string, IScreen> screens = new Dictionary<string,IScreen>();

        /// <summary>
        /// Game screen width and height
        /// </summary>
        int screenWidth, screenHeight;

        /// <summary>
        /// Billboard manager
        /// </summary>
        BillboardManager billboard;

        /// <summary>
        /// Point sprite manager
        /// </summary>
        PointSpriteManager pointSprite;

        /// <summary>
        /// Game model manager
        /// </summary>
        ModelManager modelManager;

        /// <summary>
        /// Game 2D graphics
        /// </summary>
        Graphics2D graphics2D;

        /// <summary>
        /// Store game time in this frame
        /// </summary>
        GameTime currentGameTime;

        /// <summary>
        /// GameSound
        /// </summary>
        AudioManager sound;

        /// <summary>
        /// Game input
        /// </summary>
        Input input;

        /// <summary>
        /// Shadow mapping effect
        /// </summary>
        ShadowEffect shadow;

        /// <summary>
        /// Post-screen bloom effect
        /// </summary>
        BloomEffect bloom;

        /// <summary>
        /// Whether the game is paused
        /// </summary>
        bool paused = false;

        /// <summary>
        /// Gets or sets game speed
        /// </summary>
        double gameSpeed = 1;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets windows cursor
        /// </summary>
        public Cursor Cursor
        {
            get { return cursor; }
            set
            {
                Control control = Control.FromHandle(Window.Handle);
                control.Cursor = cursor;
                cursor = value; 
            }
        }

        /// <summary>
        /// Gets content manager
        /// </summary>
        public ZipContentManager ZipContent
        {
            get { return content; }
        }

        /// <summary>
        /// Gets game input
        /// </summary>
        public Input Input
        {
            get { return input; }
        }

        /// <summary>
        /// Gets game sound
        /// </summary>
        public AudioManager Audio
        {
            get { return sound; }
        }

        /// <summary>
        /// Gets game profiler
        /// </summary>
        public Profiler Profiler
        {
            get { return profiler; }
        }

        /// <summary>
        /// Gets game screenshot capturer
        /// </summary>
        public ScreenshotCapturer ScreenshotCapturer
        {
            get { return screenshotCapturer; }
        }

        /// <summary>
        /// Gets current game screen
        /// </summary>
        public IScreen CurrentScreen
        {
            get { return currentScreen; }
        }

        /// <summary>
        /// Gets game frame per second
        /// </summary>
        public float FramePerSecond
        {
            get { return (float)profiler.FramesPerSecond; }
        }

        /// <summary>
        /// Gets Game camera
        /// </summary>
        public ICamera Camera
        {
            get { return camera; }
            set { camera = value; }
        }

        /// <summary>
        /// Gets view matrix
        /// </summary>
        public Matrix View
        {
            get { return view; }
        }

        /// <summary>
        /// Gets projection matrix
        /// </summary>
        public Matrix Projection
        {
            get { return projection; }
        }

        /// <summary>
        /// Gets view projection matrix
        /// </summary>
        public Matrix ViewProjection
        {
            get { return viewProjection; }
        }

        /// <summary>
        /// Gets view inverse matrix
        /// </summary>
        public Matrix ViewInverse
        {
            get { return viewInverse; }
        }

        /// <summary>
        /// Gets projection inverse matrix
        /// </summary>
        public Matrix ProjectionInverse
        {
            get { return projectionInverse; }
        }
        
        /// <summary>
        /// Gets view projection inverse matrix
        /// </summary>
        public Matrix ViewProjectionInverse
        {
            get { return viewProjectionInverse; }
        }

        /// <summary>
        /// Gets current view frustum
        /// </summary>
        public BoundingFrustum ViewFrustum
        {
            get { return viewFrustum; }
        }

        /// <summary>
        /// Gets the ray casted from current cursor position
        /// </summary>
        public Ray PickRay
        {
            get { return pickRay; }
        }

        /// <summary>
        /// Gets the eye position of this frame
        /// </summary>
        public Vector3 Eye
        {
            get { return eye; }
        }

        /// <summary>
        /// Gets the facing direction of this frame
        /// </summary>
        public Vector3 Facing
        {
            get { return facing; }
        }

        /// <summary>
        /// Gets or sets Game settings
        /// </summary>
        public Settings Settings
        {
            get { return settings; }
            set { settings = value; }
        }

        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        /// <summary>
        /// Gets Xna graphics device manager
        /// </summary>
        public GraphicsDeviceManager Graphics
        {
            get { return graphics; }
        }

        /// <summary>
        /// Gets screen width
        /// </summary>
        public int ScreenWidth
        {
            get { return screenWidth; }
        }

        /// <summary>
        /// Gets screen height
        /// </summary>
        public int ScreenHeight
        {
            get { return screenHeight; }
        }

        /// <summary>
        /// Gets game billboard manager
        /// </summary>
        public BillboardManager Billboard
        {
            get { return billboard; }
        }

        /// <summary>
        /// Gets point sprite manager
        /// </summary>
        public PointSpriteManager PointSprite
        {
            get { return pointSprite; }
        }

        /// <summary>
        /// Gets game model manager
        /// </summary>
        public ModelManager ModelManager
        {
            get { return modelManager; }
        }

        /// <summary>
        /// Gets game 2D graphics
        /// </summary>
        public Graphics2D Graphics2D
        {
            get { return graphics2D; }
        }

        /// <summary>
        /// Gets all game screens
        /// </summary>
        public Dictionary<string, IScreen> Screens
        {
            get { return screens; }
        }

        /// <summary>
        /// Gets current game time
        /// </summary>
        public GameTime CurrentGameTime
        {
            get { return currentGameTime; }
        }

        /// <summary>
        /// Gets game shadow effect
        /// </summary>
        public ShadowEffect Shadow
        {
            get { return shadow; }
        }

        /// <summary>
        /// Gets game bloom effect
        /// </summary>
        public BloomEffect Bloom
        {
            get { return bloom; }
        }

        /// <summary>
        /// Gets whether the game is been paused
        /// </summary>
        /// TODO: Fixe issues caused by pausing. (E.g., Timer)
        public bool Paused
        {
            get { return paused; }
            set { paused = value; }
        }

        /// <summary>
        /// Gets or sets game speed
        /// </summary>
        public double GameSpeed
        {
            get { return gameSpeed; }
            set { gameSpeed = value; }
        }
        #endregion

        #region Methods

        /// <summary>
        /// Starts a game screen and run
        /// </summary>
        /// <param name="gameScreen"></param>
        public void Run(string screenName)
        {
            Run(screens[screenName]);
        }

        /// <summary>
        /// Starts a game screen and run
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
        /// Starts a game screen
        /// </summary>
        public void StartScreen(string screenName)
        {
            StartScreen(screens[screenName]);
        }

        /// <summary>
        /// Starts a game screen
        /// </summary>
        public void StartScreen(IScreen newScreen)
        {
            if (newScreen != currentScreen)
            {
                // Leave current screen
                if (currentScreen != null)
                    currentScreen.Leave();

                // Enter the new screen
                if (newScreen != null)
                    newScreen.Enter();

                // Set current screen
                currentScreen = newScreen;
            }
        }

        /// <summary>
        /// Adds a screen to the game
        /// </summary>
        /// <param name="name"></param>
        /// <param name="screen"></param>
        public void AddScreen(string name, IScreen screen)
        {
            if (!screens.ContainsKey(name))
            {
                screens.Add(name, screen);
                Log.Write("Screen Added: " + name);
            }
        }

        /// <summary>
        /// Adds a screen to the game
        /// </summary>
        /// <param name="name"></param>
        /// <param name="screen"></param>
        public void RemoveScreen(string name)
        {
            if (screens.Remove(name))
                Log.Write("Screen Removed: " + name);
        }

        #endregion

        #region Initialization
        public BaseGame()
            : this(Settings.CreateDefaultSettings(null))
        {
        }

        public BaseGame(Settings settings)
        {
            singleton = this;

            Assembly assembly = Assembly.GetCallingAssembly();

            // Initialize log
            Log.Initialize();
            Log.NewLine();
            Log.NewLine();
            Log.Write("Isles", false);
            Log.Write("Date: " + DateTime.Now, false);
            Log.Write("Full Name: " + assembly.FullName, false);
            Log.Write("CLR Runtime Version: " + assembly.ImageRuntimeVersion, false);
            Log.NewLine();

            // Initialize settings
            if (settings == null)
                settings = Settings.CreateDefaultSettings(null);
            this.settings = settings;
            gameSpeed = settings.GameSpeed;

            content = new ZipContentManager(Services, settings.ArchiveFile, settings.ContentDirectory);
            Content.RootDirectory = settings.ContentDirectory;
            Log.Write("Archive File:" + settings.ArchiveFile + "...");
            Log.Write("Content Directory:" + settings.ContentDirectory +"...");

            graphics = new GraphicsDeviceManager(this);

            graphics.IsFullScreen               = settings.Fullscreen;
            graphics.PreferredBackBufferWidth   = settings.ScreenWidth;
            graphics.PreferredBackBufferHeight  = settings.ScreenHeight;
            graphics.SynchronizeWithVerticalRetrace = settings.VSync;
            graphics.MinimumPixelShaderProfile  = ShaderProfile.PS_2_0;
            graphics.MinimumVertexShaderProfile = ShaderProfile.VS_2_0;

            // Show cursor
            IsMouseVisible = settings.IsMouseVisible;
//#if DEBUG
            // Use variant time step to trace frame performance
            //IsFixedTimeStep = true;
            IsFixedTimeStep = settings.IsFixedTimeStep;
            //TargetElapsedTime = new TimeSpan(5000000);
//#endif
        }

        /// <summary>
        /// Gets the singleton instance of base game
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This is not a strict implementation of 'Singleton'. The constructor is not private.
        /// since we want to allow real games to derive from this class, so the singleton is
        /// actually the current game. Typically an app only creates 1 game instance, that won't
        /// be a problem most of the time.
        /// </remarks>
        public static BaseGame Singleton
        {
            get { return singleton; }
        }

        private static BaseGame singleton;

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.DeviceReset += new EventHandler(graphics_DeviceReset);
            graphics_DeviceReset(null, EventArgs.Empty);

            // Initialize sound
            input = new Input();
            input.Register(this, 0);
            Log.Write("Input Initialized...");

            if (settings.EnableSound)
            {
                Components.Add(sound = new AudioManager(this, ZipContent));
                Log.Write("Sound Initialized...");
            }

            if (settings.EnableScreenshot)
            {
                Components.Add(screenshotCapturer = new ScreenshotCapturer(this));
                Log.Write("Screenshot Capturer Initialized...");
            }

            if (settings.EnableProfile)
            {
                Components.Add(profiler = new Profiler(this));
                Log.Write("Profiler Initialized...");
            }

            if (settings.BloomSettings != null &&
                settings.BloomSettings.Enabled)
            {
                bloom = new BloomEffect(this, content);
                bloom.Settings = new BloomSettings(
                    settings.BloomSettings.Type,
                    settings.BloomSettings.Threshold,
                    settings.BloomSettings.Blur,
                    settings.BloomSettings.BloomIntensity,
                    settings.BloomSettings.BaseIntensity,
                    settings.BloomSettings.BloomSaturation,
                    settings.BloomSettings.BaseSaturation);

                Components.Add(bloom);
                Log.Write("Bloom Effect Initialized...");
            }

            ParticleSystem.LoadContent(this);
            Log.Write("Particle System Initialized...");

            // Initialize text
            graphics2D = new Graphics2D(this);
            Log.Write("2D Graphics Initialized...");

            billboard = new BillboardManager(this);
            Log.Write("Billboard Initialized...");

            if (settings.ShadowEnabled)
            {
                shadow = new ShadowEffect(this);
                Log.Write("Shadow Mapping Effect Initialized...");
            }

            //trailEffect = new TrailEffectManager();
            //Log.Write("Trail Effect Initialized...");

            pointSprite = new PointSpriteManager(this);
            Log.Write("PointSprite Initialized...");

            // Notify all screens to load contents
            foreach (KeyValuePair<string, IScreen> screen in screens)
                screen.Value.LoadContent();

            modelManager = new ModelManager();
            Log.Write("Model Manager Initialized...");

            base.Initialize();
        }

        void graphics_DeviceReset(object sender, EventArgs e)
        {
            screenWidth = GraphicsDevice.Viewport.Width;
            screenHeight = GraphicsDevice.Viewport.Height;
            
            Log.Write("Device Reset <" + screenWidth + ", " + screenHeight + ">...");

            // Re-Set device
            // Restore z buffer state
            GraphicsDevice.RenderState.DepthBufferEnable = true;
            GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            // Set u/v addressing back to wrap
            GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            // Set 128 and greate alpha compare for Model.Render
            GraphicsDevice.RenderState.ReferenceAlpha = 128;
            GraphicsDevice.RenderState.AlphaFunction = CompareFunction.Greater;
            // Set alpha blending operations
            GraphicsDevice.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
            GraphicsDevice.RenderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
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
            foreach (KeyValuePair<string, IScreen> screen in screens)
                screen.Value.UnloadContent();

            //Content.Unload();

            base.UnloadContent();
        }

        #endregion

        #region Update
        bool initialized = false;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Adjust speed
            gameTime = AdjustGameSpeed(gameTime);

            // Store game time
            currentGameTime = gameTime;

            // Update input
            input.Update(gameTime);

            // Do not update other stuff when the game is paused
            if (paused)
                return;
            
            // Update events
            Event.Update(gameTime);

            // Update timer
            Timer.Update(gameTime);

            if (camera != null)
            {
                // Update camera
                camera.Update(gameTime);

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
            if (currentScreen != null)
                currentScreen.Update(gameTime);

            // Update particle system
            ParticleSystem.UpdateAll(gameTime);

            // Clip cursor
            if (settings.ClipCursor && IsActive)
            {
                Cursor.Clip = new System.Drawing.Rectangle(
                    Window.ClientBounds.X, Window.ClientBounds.Y,
                    Window.ClientBounds.Width, Window.ClientBounds.Height);
            }

            // Tell me why I have to set this every frame...
            Cursor = cursor;

            base.Update(gameTime);
        }

        private GameTime AdjustGameSpeed(GameTime gameTime)
        {
            if (gameSpeed != 1)
            {
                // Note we only update game time
                gameTime = new GameTime(
                    gameTime.TotalRealTime, gameTime.ElapsedRealTime,
                    new TimeSpan((long)(gameTime.TotalGameTime.Ticks * gameSpeed)),
                    new TimeSpan((long)(gameTime.ElapsedGameTime.Ticks * gameSpeed)));
            }
            return gameTime;
        }

        private void UpdateFrustum()
        {
            viewFrustum = new BoundingFrustum(viewProjection);
        }

        private void UpdateAudioListener()
        {
            if (sound != null)
            {
                sound.Listener.Position = eye;
                sound.Listener.Forward = facing;
                
                // Trick! we assume the camera is always facing upwards
                sound.Listener.Up = Vector3.UnitZ;

                // Camera velocity is ignored
                sound.Listener.Velocity = Vector3.Zero;
            }
        }

        protected virtual void FirstTimeInitialize()
        {
        }

        //private void UpdatePickRay()
        //{
        //    MouseState mouseState = Mouse.GetState();

        //    int mouseX = mouseState.X;
        //    int mouseY = mouseState.Y;

        //    Vector3 nearSource = new Vector3((float)mouseX, (float)mouseY, 0.0f);
        //    Vector3 farSource = new Vector3((float)mouseX, (float)mouseY, 1.0f);

        //    //Vector3 nearPoint = GraphicsDevice.Viewport.Unproject(
        //    //    nearSource, projection, view, Matrix.Identity);

        //    //Vector3 farPoint = GraphicsDevice.Viewport.Unproject(
        //    //    farSource, projection, view, Matrix.Identity);

        //    Vector3 nearPoint = Unproject(nearSource);
        //    Vector3 farPoint = Unproject(farSource);

        //    //Log.Write(farPoint.ToString());
        //    //Log.Write(view.ToString());
        //    //Log.Write(projection.ToString());
        //    // Create a ray from the near clip plane to the far clip plane.
        //    Vector3 direction = farPoint - nearPoint;
        //    direction.Normalize();

        //    pickRay.Position = nearPoint;
        //    pickRay.Direction = direction;
        //}

        void UpdatePickRay()
        {
            MouseState mouseState = Mouse.GetState();

            Vector3 v;
            v.X = (((2.0f * mouseState.X) / screenWidth) - 1);
            v.Y = -(((2.0f * mouseState.Y) / screenHeight) - 1);
            v.Z = 0.0f;

            pickRay.Position.X = viewInverse.M41;
            pickRay.Position.Y = viewInverse.M42;
            pickRay.Position.Z = viewInverse.M43;
            pickRay.Direction = Vector3.Normalize(
                Vector3.Transform(v, viewProjectionInverse) - pickRay.Position);
        }

        /// <summary>
        /// Unproject a point on the screen to a ray in the 3D world
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Ray Unproject(int x, int y)
        {
            Ray ray;

            Vector3 v;
            v.X = (((2.0f * x) / screenWidth) - 1);
            v.Y = -(((2.0f * y) / screenHeight) - 1);
            v.Z = 0.0f;

            ray.Position.X = viewInverse.M41;
            ray.Position.Y = viewInverse.M42;
            ray.Position.Z = viewInverse.M43;
            ray.Direction = Vector3.Normalize(
                Vector3.Transform(v, viewProjectionInverse) - ray.Position);

            return ray;
        }

        /// <summary>
        /// Project a point in 3D world space to 2D screen space
        /// </summary>
        public Point Project(Vector3 position)
        {
            Vector4 hPosition = Vector4.Transform(position, viewProjection);
            hPosition.X /= hPosition.W;
            hPosition.Y /= hPosition.W;

            Point screenPosition;
            screenPosition.X = (int)(0.5f * (hPosition.X + 1) * screenWidth);
            screenPosition.Y = (int)(0.5f * (-hPosition.Y + 1) * screenHeight);
            return screenPosition;
        }

        /// <summary>
        /// Update view/projection matrices
        /// </summary>
        void UpdateMatrices()
        {
            if (camera != null)
            {
                view = camera.View;
                projection = camera.Projection;
                viewProjection = view * projection;
                viewInverse = Matrix.Invert(view);
                projectionInverse = Matrix.Invert(projection);
                //viewProjectionInverse = Matrix.Invert(viewProjection);

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

        #endregion

        #region Draw

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

            gameTime = AdjustGameSpeed(gameTime);

            graphics.GraphicsDevice.Clear(backgroundColor);

            // Draw current screen
            if (currentScreen != null)
                currentScreen.Draw(gameTime);

            if (modelManager != null)
                modelManager.Present(gameTime);

            if (billboard != null)
                billboard.Present(gameTime);

            ParticleSystem.Present(gameTime);
            
            if (pointSprite != null)
                pointSprite.Present(gameTime);



            Graphics2D.Present();

            base.Draw(gameTime);

            // Take screen shot
            if (screenshotCapturer != null && screenshotCapturer.ShouldCapture)
                screenshotCapturer.TakeScreenshot();

            GraphicsDevice.Vertices[0].SetSource(null, 0, 0);
            GraphicsDevice.Indices = null;

            base.Draw(gameTime);
        }

        #endregion

        #region Handle Event
        public EventResult HandleEvent(EventType type, object sender, object tag)
        {
            // Take screenshot
            if (type == EventType.KeyDown && (tag as Keys?).Value == Keys.PrintScreen)
                screenshotCapturer.ShouldCapture = true;

            if (currentScreen != null &&
                currentScreen.HandleEvent(type, sender, tag) == EventResult.Handled)
                return EventResult.Handled;

            if (camera != null &&
                camera.HandleEvent(type, sender, tag) == EventResult.Handled)
                return EventResult.Handled;

            return EventResult.Unhandled;
        }
        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (profiler != null)
                    profiler.Dispose();

                if (billboard != null)
                    billboard.Dispose();

                if (shadow != null)
                    shadow.Dispose();

                // Notify all screens to unload contents
                foreach (KeyValuePair<string, IScreen> screen in screens)
                    screen.Value.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
