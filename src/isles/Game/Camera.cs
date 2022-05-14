// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class CameraSettings
{
    /// <summary>
    /// Minmun camera height above ground.
    /// </summary>
    public float MinRadius { get; set; } = 10.0f;

    /// <summary>
    /// Max camera arcball radius.
    /// </summary>
    public float MaxRadius { get; set; } = 1000.0f;

    /// <summary>
    /// Default camera arcball radius.
    /// </summary>
    public float DefaultRadius { get; set; } = 100.0f;

    /// <summary>
    /// Size of the hot area at the borders of the screen that
    /// scroll the position of the camera, in pixels.
    /// </summary>
    public float ScrollAreaSize { get; set; } = 10;

    /// <summary>
    /// Global camera sensitivity scaler.
    /// </summary>
    public float Sensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Controls how mouse wheel value affects view distance.
    /// </summary>
    public float WheelFactor { get; set; } = 0.1f;

    /// <summary>
    /// Controls the global smoothness of camera transitions.
    /// </summary>
    public float Smoothness { get; set; } = 1.0f;

    /// <summary>
    /// Scrolling speed of the camera. It's faster to scroll
    /// when the camera gets higher (Depends on arcball radius).
    /// </summary>
    public float MinSpeed { get; set; } = 0.2f;
    public float MaxSpeed { get; set; } = 1.0f;
}

public enum CameraMoveState
{
    None,
    Move,
    Rotate,
    N, NE, E, SE, S, SW, W, NW,
    Freeze,
}

public class Camera : IEventListener
{
    private Vector3 eye = new(0, -100, 100);
    private Vector3 lookAt = Vector3.Zero;
    private Vector3 up = Vector3.UnitZ;

    private Matrix view;

    public Matrix View => view;

    public Matrix GetProjection(Viewport viewport)
    {
        return Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4, (float)viewport.Width / viewport.Height, 1, 5000);
    }

    public CameraSettings Settings { get; set; }

    private readonly GameWorld world;
    private const float BorderSize = 100;

    private readonly BaseGame game = BaseGame.Singleton;

    private float radius = 100.0f;

    private float roll = DefaultRoll;
    private const float DefaultRoll = 1.02173047639f;

    private float pitch = -MathHelper.PiOver2;
    private const float DefaultPitch = -MathHelper.PiOver2;

    private float eyeZ;
    private float eyeZTarget;
    private Vector3 lookAtTarget;
    private readonly float minRoll = MathHelper.ToRadians(60);
    private readonly float maxRoll = MathHelper.ToRadians(80);
    private Vector3 direction = Vector3.Zero;

    /// <summary>
    /// Variables to adjust orientation.
    /// </summary>
    private bool dragging;
    private float startRoll;
    private float startPitch;
    private Point startMousePosition;

    /// <summary>
    /// Gets or sets whether this camera is freezed.
    /// </summary>
    public bool Freezed { get; set; }

    public CameraMoveState MoveState { get; private set; }

    /// <summary>
    /// Events
    /// </summary>
    public event EventHandler BeginRotate;

    public event EventHandler EndRotate;

    public event EventHandler BeginMove;

    public event EventHandler EndMove;

    private bool moving;

    public Camera(CameraSettings settings, GameWorld world)
    {
        this.world = world;
        view = Matrix.CreateLookAt(eye, lookAt, up);

        Settings = settings;

        radius = settings.DefaultRadius;

        // Center camera
        FlyTo(new Vector3(
            world.Landscape.Size.X / 2, world.Landscape.Size.Y / 2, 0), true);
    }

    public void FlyTo(Vector3 position, bool teleport)
    {
        const float Distance = 200;

        position = RestrictToBorder(position);

        if (teleport)
        {
            lookAtTarget = lookAt = position;
        }
        else
        {
            lookAtTarget = position;

            Vector2 distance;
            distance.X = position.X - lookAt.X;
            distance.Y = position.Y - lookAt.Y;

            if (distance.LengthSquared() > Distance * Distance)
            {
                distance.Normalize();

                lookAt.X = lookAtTarget.X - distance.X * Distance;
                lookAt.Y = lookAtTarget.Y - distance.Y * Distance;
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        // Apply global camera sensitivity
        var elapsedTime = Settings.Sensitivity * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        var smoother = elapsedTime * 0.005f * Settings.Smoothness;
        if (smoother > 1)
        {
            smoother = 1;
        }

        if (!Freezed)
        {
            UpdateLookAt(elapsedTime);
            UpdateViewDistance(elapsedTime);
            UpdateOrientation();
        }
        UpdateEye(smoother);

        Matrix.CreateLookAt(ref eye, ref lookAt, ref up, out view);
    }

    private void UpdateEye(float smoother)
    {
        // Compute eye position
        var r = (float)(radius * Math.Cos(roll));
        direction.Z = (float)(radius * Math.Sin(roll));
        direction.X = (float)(r * Math.Cos(pitch));
        direction.Y = (float)(r * Math.Sin(pitch));

        // Update eye position
        eye = lookAt + direction;

        // Avoid collision with heightmap
        eyeZTarget = world.Landscape.GetHeight(eye.X, eye.Y) + Settings.MinRadius;
        eyeZTarget += (Settings.MaxRadius - eyeZTarget) * eye.Z / Settings.MaxRadius;

        eyeZ += (eyeZTarget - eyeZ) * smoother;
        eye.Z = eyeZ;
    }

    private void UpdateViewDistance(float elapsedTime)
    {
        if (!Freezed)
        {
            radius -= game.Input.MouseWheelDelta * Settings.WheelFactor;
            radius = MathHelper.Clamp(radius, Settings.MinRadius, Settings.MaxRadius);
        }
    }

    private void UpdateLookAt(float elapsedTime)
    {
        float xDelta = 0;
        float yDelta = 0;
        var xMove = false;
        var yMove = false;

        // Compute camera speed based on radius
        var speed = MathHelper.Lerp(
            Settings.MinSpeed, Settings.MaxSpeed, radius / Settings.MaxRadius);

        var sin = (float)(speed * Math.Sin(pitch)) * elapsedTime;
        var cos = (float)(speed * Math.Cos(pitch)) * elapsedTime;

        if (!Freezed)
        {
            // Navigation
            if (game.Input.MousePosition.X <= Settings.ScrollAreaSize)
            {
                xMove = true;
                xDelta = sin;
                yDelta -= cos;
            }
            else if (game.Input.MousePosition.X >= game.ScreenWidth - Settings.ScrollAreaSize)
            {
                xMove = true;
                xDelta -= sin;
                yDelta = cos;
            }

            if (game.Input.MousePosition.Y <= Settings.ScrollAreaSize)
            {
                yMove = true;
                xDelta -= cos;
                yDelta -= sin;
            }
            else if (game.Input.MousePosition.Y >= game.ScreenHeight - Settings.ScrollAreaSize)
            {
                yMove = true;
                xDelta += cos;
                yDelta += sin;
            }
        }

        // Mouse in the corners
        const float HalfSqrt2 = 0.70710678f;
        if (xMove && yMove)
        {
            xDelta *= HalfSqrt2;
            yDelta *= HalfSqrt2;
        }

        // Update look at position
        if (xDelta != 0 || yDelta != 0)
        {
            if (!moving)
            {
                moving = true;
                BeginMove(this, EventArgs.Empty);
            }

            lookAtTarget.X += xDelta;
            lookAtTarget.Y += yDelta;
            lookAtTarget = RestrictToBorder(lookAtTarget);
        }
        else if (moving)
        {
            moving = false;
            EndMove(this, EventArgs.Empty);
        }

        // Smooth look at position
        lookAt = lookAtTarget;
    }

    private Vector3 RestrictToBorder(Vector3 v)
    {
        // Restrict look at to the world.Landscape bounds
        if (v.X < BorderSize)
        {
            v.X = BorderSize;
        }
        else if (v.X > world.Landscape.Size.X - BorderSize)
        {
            v.X = world.Landscape.Size.X - BorderSize;
        }

        if (v.Y < BorderSize)
        {
            v.Y = BorderSize;
        }
        else if (v.Y > world.Landscape.Size.Y - BorderSize)
        {
            v.Y = world.Landscape.Size.Y - BorderSize;
        }

        return v;
    }

    private const float RotationFactorX = 0.0052359877559f;
    private const float RotationFactorY = 0.0104719755119f;

    private void UpdateOrientation()
    {
        // Adjusting view
        if (dragging && game.Input.Mouse.MiddleButton == ButtonState.Pressed)
        {
            pitch = startPitch + (game.Input.MousePosition.X - startMousePosition.X) * RotationFactorX;
            roll = startRoll - (game.Input.MousePosition.Y - startMousePosition.Y) * RotationFactorY;
        }

        // Restrict roll
        if (roll < minRoll)
        {
            roll = minRoll;
        }
        else if (roll > maxRoll)
        {
            roll = maxRoll;
        }

        // Orbit around the point
        const float PiPi = 2 * MathHelper.Pi;
        var offset = pitch - pitch;
        while (offset > MathHelper.Pi)
        {
            offset -= PiPi;
        }

        while (offset < -MathHelper.Pi)
        {
            offset += PiPi;
        }

        pitch += offset;
    }

    public EventResult HandleEvent(EventType type, object sender, object tag)
    {
        var input = sender as Input;
        var key = tag as Keys?;

        // Start adjusting view by pressing middle button
        if (type == EventType.MiddleButtonDown && !moving && !Freezed)
        {
            input.Capture(this);
            dragging = true;
            startRoll = roll;
            startPitch = pitch;
            startMousePosition = input.MousePosition;
            Freezed = true;
            BeginRotate(this, EventArgs.Empty);
            return EventResult.Handled;
        }

        if (type == EventType.MiddleButtonUp)
        {
            input.Uncapture();
            dragging = false;
            Freezed = false;
            EndRotate(this, EventArgs.Empty);
            return EventResult.Handled;
        }

        // Press space to return to normal view
        if (!Freezed && type == EventType.KeyDown && (tag as Keys?).Value == Keys.Back)
        {
            roll = DefaultRoll;
            radius = Settings.DefaultRadius;
            pitch = DefaultPitch;
        }

        // Pump the event down to other listener
        return EventResult.Unhandled;
    }
}

