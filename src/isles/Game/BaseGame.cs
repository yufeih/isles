// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Isles;

public class BaseGame : Game, IEventListener
{
    private Color backgroundColor = Color.Black;

    private Matrix view;
    private Matrix projection;
    private Matrix viewProjection;
    private Matrix viewInverse;
    private Matrix projectionInverse;
    private Matrix viewProjectionInverse;

    private Ray pickRay;
    private Vector3 eye;
    private Vector3 facing;

    public Input Input { get; private set; }

    public AudioManager Audio { get; private set; }

    public IScreen CurrentScreen { get; private set; }

    public ICamera Camera { get; set; }

    public Matrix View => view;

    public Matrix Projection => projection;

    public Matrix ViewProjection => viewProjection;

    public Matrix ViewInverse => viewInverse;

    public Matrix ProjectionInverse => projectionInverse;

    public Matrix ViewProjectionInverse => viewProjectionInverse;

    public BoundingFrustum ViewFrustum { get; private set; }

    public Ray PickRay => pickRay;

    public Vector3 Eye => eye;

    public Vector3 Facing => facing;

    public Settings Settings { get; set; }

    public GraphicsDeviceManager Graphics { get; }

    public TextureLoader TextureLoader { get; private set; }

    public ModelLoader ModelLoader { get; private set; }

    public ShaderLoader ShaderLoader { get; private set; }

    public int ScreenWidth { get; private set; }

    public int ScreenHeight { get; private set; }

    public BillboardManager Billboard { get; private set; }

    public ModelRenderer ModelRenderer { get; private set; }

    public Graphics2D Graphics2D { get; private set; }

    public GameTime CurrentGameTime { get; private set; }

    public ShadowEffect Shadow { get; private set; }

    public BloomEffect Bloom { get; private set; }

    public void StartScreen(IScreen newScreen)
    {
        CurrentScreen = newScreen;
    }

    public BaseGame()
    {
        Singleton = this;
        Settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText("data/settings/settings.json"));

        Graphics = new GraphicsDeviceManager(this)
        {
            GraphicsProfile = GraphicsProfile.HiDef,
            IsFullScreen = Settings.Fullscreen,
            PreferredBackBufferWidth = Settings.ScreenWidth,
            PreferredBackBufferHeight = Settings.ScreenHeight,
            SynchronizeWithVerticalRetrace = Settings.VSync,
        };

        // Show cursor
        IsMouseVisible = Settings.IsMouseVisible;
        IsFixedTimeStep = Settings.IsFixedTimeStep;
    }

    public static BaseGame Singleton { get; private set; }

    protected override void Initialize()
    {
        Graphics.DeviceReset += graphics_DeviceReset;
        graphics_DeviceReset(null, EventArgs.Empty);

        // Initialize sound
        Input = new Input();
        Input.Register(this, 0);

        Components.Add(Audio = new AudioManager(this));

        TextureLoader = new(GraphicsDevice);
        ModelLoader = new(GraphicsDevice, TextureLoader);
        ShaderLoader = new(GraphicsDevice);

        if (Settings.BloomSettings != null)
        {
            Bloom = new BloomEffect(GraphicsDevice, ShaderLoader) { Settings = Settings.BloomSettings };
        }

        ParticleSystem.LoadContent(this);

        // Initialize text
        Graphics2D = new Graphics2D(this);

        Billboard = new BillboardManager(this);

        if (Settings.ShadowEnabled)
        {
            Shadow = new ShadowEffect(GraphicsDevice, ShaderLoader);
        }

        ModelRenderer = new ModelRenderer(GraphicsDevice, ShaderLoader);

        base.Initialize();
    }

    private void graphics_DeviceReset(object sender, EventArgs e)
    {
        ScreenWidth = GraphicsDevice.Viewport.Width;
        ScreenHeight = GraphicsDevice.Viewport.Height;
    }

    private bool initialized;

    protected override void Update(GameTime gameTime)
    {
        CurrentGameTime = gameTime;

        Input.Update(gameTime);
        Event.Update(gameTime);

        if (Camera != null)
        {
            Camera.Update(gameTime);
            UpdateMatrices();
            UpdateFrustum();
            UpdatePickRay();
            UpdateAudioListener();
        }

        CurrentScreen?.Update(gameTime);

        // Update particle system
        ParticleSystem.UpdateAll(gameTime);

        Audios.Update(gameTime);
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
        pickRay.Direction = Vector3.Normalize(Vector3.Transform(v, viewProjectionInverse) - pickRay.Position);
    }

    /// <summary>
    /// Unproject a point on the screen to a ray in the 3D world.
    /// </summary>
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

    protected override void Draw(GameTime gameTime)
    {
        // Invoke first time initialize
        if (!initialized)
        {
            initialized = true;
            FirstTimeInitialize();
        }

        Bloom?.BeginDraw();
        Graphics.GraphicsDevice.Clear(backgroundColor);
        CurrentScreen?.Draw(gameTime);
        Billboard?.Present();
        ParticleSystem.Present();
        Graphics2D.Present();
        Bloom?.EndDraw();
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
}
