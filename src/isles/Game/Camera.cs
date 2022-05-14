// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class CameraSettings
{
    public float MinRadius { get; set; } = 50;
    public float MaxRadius { get; set; } = 225;
    public float DefaultRadius { get; set; } = 180;
    public float ScrollAreaSize { get; set; } = 2;
    public float Sensitivity { get; set; } = 1;
    public float WheelFactor { get; set; } = 0.1f;
    public float Speed { get; set; } = 0.3f;
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

    private const float BorderSize = 100;

    private readonly BaseGame game = BaseGame.Singleton;

    private float radius = 100.0f;

    private float roll = DefaultRoll;
    private const float DefaultRoll = 1.02173047639f;

    private float pitch = -MathHelper.PiOver2;
    private const float DefaultPitch = -MathHelper.PiOver2;

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

    public event EventHandler BeginRotate;

    public event EventHandler EndRotate;

    public event EventHandler BeginMove;

    public event EventHandler EndMove;

    private bool moving;

    public Camera(CameraSettings settings)
    {
        view = Matrix.CreateLookAt(eye, lookAt, up);

        Settings = settings;

        radius = settings.DefaultRadius;
    }

    public void FlyTo(Vector3 position)
    {
        lookAt = position;
    }

    public void Update(GameTime gameTime, ILandscape terrain)
    {
        if (!Freezed)
        {
            var elapsedTime = Settings.Sensitivity * (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            UpdateLookAt(elapsedTime);
            UpdateViewDistance();
            UpdateOrientation();
            UpdateEye(terrain);
        }
    }

    private void UpdateEye(ILandscape terrain)
    {
        // Compute eye position
        var r = (float)(radius * Math.Cos(roll));
        direction.Z = (float)(radius * Math.Sin(roll));
        direction.X = (float)(r * Math.Cos(pitch));
        direction.Y = (float)(r * Math.Sin(pitch));

        // Update eye position
        eye = lookAt + direction;
        eye.Z = Math.Max(eye.Z, terrain.GetHeight(eye.X, eye.Y) + Settings.MinRadius);

        lookAt = ClampBounds(lookAt, terrain);

        Matrix.CreateLookAt(ref eye, ref lookAt, ref up, out view);
    }

    private void UpdateViewDistance()
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

        var speed =  Settings.Speed;
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

            lookAt.X += xDelta;
            lookAt.Y += yDelta;
        }
        else if (moving)
        {
            moving = false;
            EndMove(this, EventArgs.Empty);
        }
    }

    private static Vector3 ClampBounds(Vector3 v, ILandscape terrain)
    {
        if (v.X < BorderSize)
        {
            v.X = BorderSize;
        }
        else if (v.X > terrain.Size.X - BorderSize)
        {
            v.X = terrain.Size.X - BorderSize;
        }

        if (v.Y < BorderSize)
        {
            v.Y = BorderSize;
        }
        else if (v.Y > terrain.Size.Y - BorderSize)
        {
            v.Y = terrain.Size.Y - BorderSize;
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
            pitch = MathFHelper.NormalizeRotation(startPitch + (game.Input.MousePosition.X - startMousePosition.X) * RotationFactorX);
            roll = MathHelper.Clamp(
                MathFHelper.NormalizeRotation(startRoll - (game.Input.MousePosition.Y - startMousePosition.Y) * RotationFactorY),
                minRoll,
                maxRoll);
        }
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

