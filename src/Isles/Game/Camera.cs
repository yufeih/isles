// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Isles.Engine
{
    /// <summary>
    /// Interface for game camera.
    /// </summary>
    public interface ICamera : IEventListener
    {
        /// <summary>
        /// Gets the camera view matrix.
        /// </summary>
        Matrix View { get; }

        /// <summary>
        /// Gets the camera projection matrix.
        /// </summary>
        Matrix Projection { get; }

        /// <summary>
        /// Update camera parameters.
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);
    }

    /// <summary>
    /// Game camera class.
    /// Coordinate system in Xna is right-handed!
    /// Z axis is up by default, since we are using a bird-eye view.
    /// </summary>
    public class Camera : ICamera
    {
        protected Vector3 eye = new(0, -100, 100);
        protected Vector3 lookAt = Vector3.Zero;
        protected Vector3 up = Vector3.UnitZ;

        protected Matrix view;
        protected Matrix projection;

        protected GraphicsDevice device;

        protected float nearPlane = 1;
        protected float farPlane = 5000;
        protected float fieldOfView = MathHelper.PiOver4;

        /// <summary>
        /// Gets camera view matrix. Can't be set due to consistency.
        /// </summary>
        public Matrix View => view;

        /// <summary>
        /// Gets camera projection matrix.
        /// </summary>
        public Matrix Projection => projection;

        public Camera(GraphicsDevice graphics)
        {
            device = graphics;
            graphics.DeviceReset += ResetProjection;
            view = Matrix.CreateLookAt(eye, lookAt, up);
            ResetProjection(null, EventArgs.Empty);

            Log.Write("Camera Initialized...");
        }

        protected void ResetProjection(object sender, EventArgs e)
        {
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                (float)device.Viewport.Width / device.Viewport.Height,
                nearPlane, farPlane);
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public virtual void Update(GameTime gameTime)
        {
            // Update matrix and view frustum
            Matrix.CreateLookAt(ref eye, ref lookAt, ref up, out view);
        }

        public virtual EventResult HandleEvent(EventType type, object sender, object tag)
        {
            return EventResult.Unhandled;
        }
    }

    /// <summary>
    /// Settings for game camera.
    /// </summary>
    public class GameCameraSettings
    {
        /// <summary>
        /// Minmun camera height above ground.
        /// </summary>
        public float MinHeightAboveGround { get; set; } = 10.0f;

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

    /// <summary>
    /// Game camera.
    /// </summary>
    public class GameCamera : Camera
    {
        /// <summary>
        /// Game camera settings.
        /// </summary>
        public GameCameraSettings Settings { get; set; }

        /// <summary>
        /// Landscape for the game camera.
        /// </summary>
        public ILandscape Landscape => world.Landscape;

        private readonly GameWorld world;
        private const float BorderSize = 100;

        /// <summary>
        /// GameCamera needs screen width and height and mouse
        /// pick ray each frame, so BaseGame is presented here.
        /// </summary>
        private readonly BaseGame game = BaseGame.Singleton;

        /// <summary>
        /// Radius of the camera arcball.
        /// </summary>
        private float radius = 100.0f;
        private float radiusScaler = 100.0f;
        private float radiusTarget = 100.0f;
        private const float DefaultRadius = 180.0f;

        /// <summary>
        /// Roll of the camera arcball. (Rotate around x in view space).
        /// </summary>
        private float roll = DefaultRoll;
        private float rollTarget = DefaultRoll;
        private const float DefaultRoll = 1.02173047639f;

        /// <summary>
        /// Pitch of the camera arcball. (Rotate around z in world space).
        /// </summary>
        private float pitch = -MathHelper.PiOver2;
        private float pitchTarget = -MathHelper.PiOver2;
        private const float DefaultPitch = -MathHelper.PiOver2;

        /// <summary>
        /// Height of the camera.
        /// </summary>
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

        /// <summary>
        /// Gets or sets whether this camera is been moved by user.
        /// </summary>
        public bool MovedByUser { get; set; }

        /// <summary>
        /// Events
        /// </summary>
        public event EventHandler BeginRotate;

        public event EventHandler EndRotate;

        public event EventHandler BeginMove;

        public event EventHandler EndMove;

        private bool moving;
        private bool orbit;
        private float orbitSpeed;

        /// <summary>
        /// Creates a game camera.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="world.Landscape"></param>
        public GameCamera(GameCameraSettings settings, GameWorld world)
            : base(BaseGame.Singleton.GraphicsDevice)
        {
            this.world = world;

            Settings = settings;

            radius = radiusScaler = radiusTarget = settings.DefaultRadius;

            // Apply changes
            ResetProjection(null, EventArgs.Empty);

            // Center camera
            FlyTo(new Vector3(
                world.Landscape.Size.X / 2, world.Landscape.Size.Y / 2, 0), true);
        }

        /// <summary>
        /// Fly the camera to a given location.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="teleport"></param>
        public void FlyTo(Vector3 position, bool teleport)
        {
            const float Distance = 200;

            position = RestrictToBorder(position);

            if (teleport)
            {
                MovedByUser = true;
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

        /// <summary>
        /// Make the game camera orbit around a specified point on the ground.
        /// </summary>
        public void Orbit(Vector3? point, float speed, float radius, float roll)
        {
            if (point.HasValue)
            {
                FlyTo(point.Value, false);
            }

            orbitSpeed = speed;
            radiusTarget = radius;
            rollTarget = roll;
            orbit = true;
            Freezed = true;
        }

        public void CancelOrbit()
        {
            orbit = false;
        }

        public override void Update(GameTime gameTime)
        {
            // Apply global camera sensitivity
            var elapsedTime = Settings.Sensitivity *
                (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            var smoother = elapsedTime * 0.005f * Settings.Smoothness;
            if (smoother > 1)
            {
                smoother = 1;
            }

            UpdateLookAt(elapsedTime, smoother);
            UpdateViewDistance(elapsedTime, smoother);
            UpdateOrientation(elapsedTime, smoother);
            UpdateEye(smoother);

            base.Update(gameTime);
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
            eyeZTarget = world.Landscape.GetHeight(eye.X, eye.Y) + Settings.MinHeightAboveGround;
            eyeZTarget += (Settings.MaxRadius - eyeZTarget) * eye.Z / Settings.MaxRadius;

            eyeZ += (eyeZTarget - eyeZ) * smoother;
            eye.Z = eyeZ;
        }

        /// <summary>
        /// Adjust view distance (radius) using mouse wheel or 'PgUp', 'PgDown' button.
        /// </summary>
        /// <param name="elapsedTime"></param>
        private void UpdateViewDistance(float elapsedTime, float smoother)
        {
            if (!Freezed && !orbit)
            {
                radiusScaler -= game.Input.MouseWheelDelta * Settings.WheelFactor;

                if (game.Input.Keyboard.IsKeyDown(Keys.PageUp))
                {
                    radiusScaler += 0.5f * elapsedTime;
                }
                else if (game.Input.Keyboard.IsKeyDown(Keys.PageDown))
                {
                    radiusScaler -= 0.5f * elapsedTime;
                }

                radiusScaler = MathHelper.Clamp(
                    radiusScaler, Settings.MinHeightAboveGround, Settings.MaxRadius);

                radiusTarget = (float)(Settings.MaxRadius *
                    (Math.Exp(radiusScaler / Settings.MaxRadius) - 1) / (Math.E - 1));

                radiusTarget = MathHelper.Clamp(
                    radiusTarget, Settings.MinHeightAboveGround, Settings.MaxRadius);
            }

            // Smooth radius
            radius += (radiusTarget - radius) * smoother;
        }

        /// <summary>
        /// Update the position of camera lookat when the cursor enters the
        /// borders of the screen.
        /// </summary>
        /// <param name="elapsedTime"></param>
        /// <param name="heightFactor"></param>
        private void UpdateLookAt(float elapsedTime, float smoother)
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

            if (!Freezed && !orbit)
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
                MovedByUser = true;

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
            lookAt += (lookAtTarget - lookAt) * smoother / 2;
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

        /// <summary>
        /// Adjust the orientation of the camera using middle button or arrow keys.
        /// Simulate middle button by pressing left and right button at the same time.
        /// </summary>
        private void UpdateOrientation(float elapsedTime, float smoother)
        {
            // Adjusting view
            if (dragging && game.Input.Mouse.MiddleButton == ButtonState.Pressed)
            {
                pitchTarget = startPitch +
                    (game.Input.MousePosition.X - startMousePosition.X) * RotationFactorX;
                rollTarget = startRoll -
                    (game.Input.MousePosition.Y - startMousePosition.Y) * RotationFactorY;
            }

            if (!Freezed && !orbit)
            {
                // Adjust roll/pitch using arrow keys
                if (game.Input.Keyboard.IsKeyDown(Keys.Left))
                {
                    pitchTarget -= 0.4f * RotationFactorX * elapsedTime;
                }
                else if (game.Input.Keyboard.IsKeyDown(Keys.Right))
                {
                    pitchTarget += 0.4f * RotationFactorX * elapsedTime;
                }

                if (game.Input.Keyboard.IsKeyDown(Keys.Up))
                {
                    rollTarget += 0.2f * RotationFactorY * elapsedTime;
                }
                else if (game.Input.Keyboard.IsKeyDown(Keys.Down))
                {
                    rollTarget -= 0.2f * RotationFactorY * elapsedTime;
                }
            }

            // Restrict roll
            if (!orbit)
            {
                if (rollTarget < minRoll)
                {
                    rollTarget = minRoll;
                }
                else if (rollTarget > maxRoll)
                {
                    rollTarget = maxRoll;
                }
            }

            // Smooth pitch and roll
            roll += (rollTarget - roll) * smoother;

            // Orbit around the point
            if (orbit)
            {
                pitch += elapsedTime * orbitSpeed;
            }
            else
            {
                const float PiPi = 2 * MathHelper.Pi;
                var offset = pitchTarget - pitch;
                while (offset > MathHelper.Pi)
                {
                    offset -= PiPi;
                }

                while (offset < -MathHelper.Pi)
                {
                    offset += PiPi;
                }

                pitch += offset * smoother;
            }
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            var input = sender as Input;
            var key = tag as Keys?;

            // Start adjusting view by pressing middle button
            if (type == EventType.MiddleButtonDown && !moving && !Freezed && !orbit)
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
            if (!Freezed && !orbit && type == EventType.KeyDown && (tag as Keys?).Value == Keys.Back)
            {
                rollTarget = DefaultRoll;
                radiusScaler = radiusTarget = DefaultRadius;
                pitchTarget = DefaultPitch;
            }

            // Pump the event down to other listener
            return EventResult.Unhandled;
        }
    }
}

