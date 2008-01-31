//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;
#endregion

namespace Isles.Engine
{
    /// <summary>
    /// Interface for game camera
    /// </summary>
    public interface ICamera
    {
        /// <summary>
        /// Gets the camera view matrix
        /// </summary>
        Matrix View { get; }

        /// <summary>
        /// Gets the camera projection matrix
        /// </summary>
        Matrix Projection { get; }

        /// <summary>
        /// Update camera parameters
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);
    }

    /// <summary>
    /// Game camera class.
    /// Coordinate system in Xna is right-handed!
    /// Z axis is up by default, since we are using a bird-eye view
    /// </summary>
    public class Camera : ICamera
    {
        protected Vector3 eye = new Vector3(0, -100, 100);
        protected Vector3 lookAt = Vector3.Zero;
        protected Vector3 up = Vector3.UnitZ;

        protected Matrix view;
        protected Matrix projection;

        protected GraphicsDevice device;

        protected float nearPlane = 1;
        protected float farPlane = 10000;
        protected float fieldOfView = MathHelper.PiOver4;

        /// <summary>
        /// Gets camera view matrix. Can't be set due to consistency
        /// </summary>
        public Matrix View
        {
            get { return view; }
        }

        /// <summary>
        /// Gets camera projection matrix.
        /// </summary>
        public Matrix Projection
        {
            get { return projection; }
        }

        public Camera(GraphicsDevice graphics)
        {
            device = graphics;
            graphics.DeviceReset += new EventHandler(ResetProjection);
            view = Matrix.CreateLookAt(eye, lookAt, up);
            ResetProjection(null, EventArgs.Empty);

            Log.Write("Camera Initialized...");
        }
        
        protected void ResetProjection(object sender, EventArgs e)
        {
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, (float)device.Viewport.Width / device.Viewport.Height, 1, 10000);
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
    }

    /// <summary>
    /// Game camera
    /// </summary>
    public class GameCamera : Camera
    {
        float minHeightAboveGround = 10.0f;
        float navigateAreaSize = 10;
        float wheelFactor = 0.1f;
        float speedHeightFactor = 1.25f;
        float initialHeight = 100.0f;
        float rotationFactorX = MathHelper.PiOver2 / 300;
        float rotationFactorY = MathHelper.PiOver2 / 150;

        bool dragging = false;
        bool disableSmootherForOneFrame = true;

        float baseSpeed = 0.15f;
        float speed = 0.2f;
        float radius = 200.0f;
        float radiusSmoother = 100.0f;
        float roll = MathHelper.PiOver4;
        float rollSmoother = MathHelper.PiOver4;
        float pitch = -MathHelper.PiOver4;
        float pitchSmoother = -MathHelper.PiOver2;
        float eyeZSmoother = 0;
        Vector3 lookAtSmoother = new Vector3();

        float minRoll = MathHelper.ToRadians(10);
        float maxRoll = MathHelper.ToRadians(80);

        float minRadius = 10.0f;
        float maxRadius = 1500.0f;

        float startRoll, startPitch;
        Point startMousePosition;

        Vector3 direction = Vector3.Zero;

        BoundingBox spaceBounds = new BoundingBox(
            new Vector3(-100, -100, 0), new Vector3(1100, 1100, 2000));

        Landscape landscape;
        BaseGame game;

        /// <summary>
        /// Creates a game camera
        /// </summary>
        /// <param name="game"></param>
        /// <param name="landscape"></param>
        public GameCamera(Landscape landscape)
            : base(BaseGame.Singleton.GraphicsDevice)
        {
            this.game = BaseGame.Singleton;
            this.landscape = landscape;

            // Initialize camera from settings
            Settings.Camera cameraSettings = game.Settings.CameraSettings;

            fieldOfView = cameraSettings.FieldOfView;
            nearPlane = cameraSettings.NearPlane;
            farPlane = cameraSettings.FarPlane;
            minHeightAboveGround = cameraSettings.MinHeightAboveGround;
            navigateAreaSize = cameraSettings.NavigationAreaSize;
            wheelFactor = cameraSettings.WheelFactor;
            speedHeightFactor = cameraSettings.ScrollHeightFactor;
            initialHeight = cameraSettings.InitialHeight;
            rotationFactorX = cameraSettings.RotationFactorX;
            rotationFactorY = cameraSettings.RotationFactorY;
            baseSpeed = cameraSettings.BaseSpeed;
            radius = cameraSettings.Radius;
            roll = cameraSettings.Roll;
            pitch = cameraSettings.Pitch;
            minRoll = cameraSettings.MinRoll;
            maxRoll = cameraSettings.MaxRoll;
            minRadius = cameraSettings.MinRadius;
            maxRadius = cameraSettings.MaxRadius;
            
            // Apply changes
            ResetProjection(null, EventArgs.Empty);


            // Center camera
            FlyTo(new Vector3(landscape.Size.X / 2, landscape.Size.Y / 2, 0), true);
            SpaceBounds = new BoundingBox(Vector3.Zero,
                new Vector3(landscape.Size.X, landscape.Size.Y, 6 * landscape.Size.Z));
        }

        /// <summary>
        /// Fly the camera to a given location
        /// </summary>
        /// <param name="position"></param>
        /// <param name="teleport"></param>
        public void FlyTo(Vector3 position, bool teleport)
        {
            lookAtSmoother = lookAt = position;
            disableSmootherForOneFrame = teleport;
        }

        /// <summary>
        /// Gets or sets the bounds that restrict the camera
        /// </summary>
        public BoundingBox SpaceBounds
        {
            get { return spaceBounds; }
            set { spaceBounds = value; maxRadius = spaceBounds.Max.Z; }
        }

        private static Vector2 XYZToSphere(Vector3 v)
        {
            Vector2 xy = new Vector2(v.X, v.Y);

            return new Vector2(
                (float)Math.Atan2((float)xy.Y, (float)xy.X),
                (float)Math.Atan2((float)v.Z, (float)xy.Length()));
        }

        public override void Update(GameTime gameTime)
        {
            float elapsedTime = (float)(gameTime.ElapsedGameTime.TotalMilliseconds);
            float heightFactor = ( radius - initialHeight) / spaceBounds.Max.Z;
            float smoother = (float)(0.003f * gameTime.ElapsedGameTime.TotalMilliseconds);

            //Log.Write(elapsedTime.ToString());

            // Start adjusting view
            // Press middle button or Press left and right button
            if (Input.MouseMiddleButtonJustPressed ||
               (Input.MouseLeftButtonPressed && Input.MouseRightButtonJustPressed) ||
               (Input.MouseRightButtonPressed && Input.MouseLeftButtonJustPressed))
            {
                dragging = true;
                startRoll = rollSmoother;
                startPitch = pitchSmoother;
                startMousePosition = Input.MousePosition;
            }
            else if (Input.MouseRightButtonJustReleased ||
                     Input.MouseLeftButtonJustReleased ||
                     Input.MouseMiddleButtonJustReleased)
            {
                dragging = false;
            }
            // Adjusting view
            else if( dragging &&
                   (Input.MouseMiddleButtonPressed ||
                   (Input.MouseLeftButtonPressed && Input.MouseRightButtonPressed)))
            {
                pitch = startPitch +
                    (Input.MousePosition.X - startMousePosition.X) * rotationFactorX;
                roll = startRoll -
                    (Input.MousePosition.Y - startMousePosition.Y) * rotationFactorY;
            }
            else
            {
                float xDelta = 0;
                float yDelta = 0;
                bool xMove = false;
                bool yMove = false;
                speed = baseSpeed + heightFactor * speedHeightFactor;

                // Navigation
                if (Input.Keyboard.IsKeyDown(Keys.A) ||
                    Input.MousePosition.X < navigateAreaSize)
                {
                    xMove = true;
                    xDelta =  (float)(speed * Math.Sin(pitchSmoother)) * elapsedTime;
                    yDelta -= (float)(speed * Math.Cos(pitchSmoother)) * elapsedTime;
                }
                else if (
                    Input.Keyboard.IsKeyDown(Keys.D) ||
                    Input.MousePosition.X > game.ScreenWidth - navigateAreaSize)
                {
                    xMove = true;
                    xDelta -= (float)(speed * Math.Sin(pitchSmoother)) * elapsedTime;
                    yDelta =  (float)(speed * Math.Cos(pitchSmoother)) * elapsedTime;
                }

                if (Input.Keyboard.IsKeyDown(Keys.W) ||
                    Input.MousePosition.Y < navigateAreaSize)
                {
                    yMove = true;
                    xDelta -= (float)(speed * Math.Cos(pitchSmoother)) * elapsedTime;
                    yDelta -= (float)(speed * Math.Sin(pitchSmoother)) * elapsedTime;
                }
                else if (
                    Input.Keyboard.IsKeyDown(Keys.S) ||
                    Input.MousePosition.Y > game.ScreenHeight - navigateAreaSize)
                {
                    yMove = true;
                    xDelta += (float)(speed * Math.Cos(pitchSmoother)) * elapsedTime;
                    yDelta += (float)(speed * Math.Sin(pitchSmoother)) * elapsedTime;
                }

                // Mouse in the corners
                const float HalfSqrt2 = 0.70710678f;
                if (xMove && yMove)
                {
                    xDelta *= HalfSqrt2;
                    yDelta *= HalfSqrt2;
                }

                lookAtSmoother.X += xDelta;
                lookAtSmoother.Y += yDelta;
            }

            // Adjust roll/pitch using arrow keys
            if (Input.Keyboard.IsKeyDown(Keys.Left))
                pitch -= 0.4f * rotationFactorX * elapsedTime;
            else if (Input.Keyboard.IsKeyDown(Keys.Right))
                pitch += 0.4f * rotationFactorX * elapsedTime;

            if (Input.Keyboard.IsKeyDown(Keys.Up))
                roll += 0.2f * rotationFactorY * elapsedTime;
            else if (Input.Keyboard.IsKeyDown(Keys.Down))
                roll -= 0.2f * rotationFactorY * elapsedTime;

            // Restrict roll
            if (roll < minRoll)
                roll = minRoll;
            else if (roll > maxRoll)
                roll = maxRoll;
            
            // Adjust view distance (radius) using mouse wheel or '+', '-' button
            radius -= Input.MouseWheelDelta * wheelFactor * (10 * heightFactor + 1);
            if (Input.Keyboard.IsKeyDown(Keys.PageUp))
                radius += 0.5f * elapsedTime;
            else if (Input.Keyboard.IsKeyDown(Keys.PageDown))
                radius -= 0.5f * elapsedTime;

            if (radius < minRadius)
                radius = minRadius;
            else if (radius > maxRadius)
                radius = maxRadius;

            // Compute eye position
            float r = (float)(radiusSmoother * Math.Cos(rollSmoother));
            direction.Z = (float)(radiusSmoother * Math.Sin(rollSmoother));
            direction.X = (float)(r * Math.Cos(pitchSmoother));
            direction.Y = (float)(r * Math.Sin(pitchSmoother));

            // Restrict lookAt to camera space bounds
            if (lookAtSmoother.X < spaceBounds.Min.X)
                lookAtSmoother.X = spaceBounds.Min.X;
            else if (lookAtSmoother.X > spaceBounds.Max.X)
                lookAtSmoother.X = spaceBounds.Max.X;

            if (lookAtSmoother.Y < spaceBounds.Min.Y)
                lookAtSmoother.Y = spaceBounds.Min.Y;
            else if (lookAtSmoother.Y > spaceBounds.Max.Y)
                lookAtSmoother.Y = spaceBounds.Max.Y;

            if (lookAtSmoother.Z < spaceBounds.Min.Z)
                lookAtSmoother.Z = spaceBounds.Min.Z;
            else if (lookAtSmoother.Z > spaceBounds.Max.Z)
                lookAtSmoother.Z = spaceBounds.Max.Z;

            lookAt += (lookAtSmoother - lookAt) * smoother * 2;

            // Update eye position
            eye = lookAt + direction;

            // Avoid collision with heightmap
            if (landscape != null)
            {
                float height = landscape.GetHeight(eye.X, eye.Y) +minHeightAboveGround;
                if (height < minHeightAboveGround)
                    height = minHeightAboveGround;;
                eye.Z = eye.Z * (spaceBounds.Max.Z - height) / spaceBounds.Max.Z + height;
            }

            // Update smoother
            if (!disableSmootherForOneFrame)
            {
                eyeZSmoother += (eye.Z - eyeZSmoother) * smoother *1.5f;                 
                rollSmoother += (roll - rollSmoother) * smoother;
                radiusSmoother += (radius - radiusSmoother) * smoother;
                pitchSmoother += (pitch - pitchSmoother) * smoother;
            }
            else
            {
                lookAt = lookAtSmoother;    // lookAtSmoother is a bit different
                pitchSmoother = pitch;
                rollSmoother = roll;
                radiusSmoother = radius;
                disableSmootherForOneFrame = false;
            }

            eye.Z = eyeZSmoother;

            base.Update(gameTime);
        }
    }
}


