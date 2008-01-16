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

        protected BaseGame game;
        protected GraphicsDevice device;

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

        public Camera(BaseGame game)
        {
            this.game = game;

            device = game.GraphicsDevice;
            game.Graphics.DeviceReset += new EventHandler(Graphics_DeviceReset);
            view = Matrix.CreateLookAt(eye, lookAt, up);
            Graphics_DeviceReset(null, EventArgs.Empty);

            Log.Write("Camera Initialized...");
        }
        
        void Graphics_DeviceReset(object sender, EventArgs e)
        {
            projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, (float)device.Viewport.Width / device.Viewport.Height, 1, 5000);
            //projection = Matrix.CreateScale(0.001f, 0.001f, -0.0001f);
        }
        
        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public virtual void Update(GameTime gameTime)
        {
            // Update matrix and view frustum
            Matrix.CreateLookAt(ref eye, ref lookAt, ref up, out view);
            //view = Matrix.CreateLookAt(eye, lookAt, up);
            //Log.Write(eye.ToString());
            //Log.Write(lookAt.ToString());
            //Log.Write(view.Translation.ToString());
            //Text.DrawString(
            //    up.ToString(),
            //    15, new Vector2(0, 200), Color.Yellow);
        }
    }

    /// <summary>
    /// Game camera
    /// </summary>
    public class GameCamera : Camera
    {
        const float NavigateAreaSize = 10;
        const float WheelFactor = 0.1f;
        const float SpeedHeightFactor = 1.25f;
        const float InitialHeight = 100.0f;
        const float RotationFactorX = MathHelper.PiOver2 / 300;
        const float RotationFactorY = MathHelper.PiOver2 / 150;

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
        float maxRoll = MathHelper.ToRadians(40);

        float minRadius = 50.0f;
        float maxRadius = 1500.0f;

        float startRoll, startPitch;
        //float arcBallRadius;
        //bool dragFartherHalf;
        Point startMousePosition;
        //Vector2 startSpherePosition;

        Vector3 direction = Vector3.Zero;

        BoundingBox spaceBounds = new BoundingBox(
            new Vector3(-100, -100, 0), new Vector3(1100, 1100, 2000));

        Landscape landscape;
        GameScreen gameScreen;

        /// <summary>
        /// Creates a game camera
        /// </summary>
        /// <param name="game"></param>
        /// <param name="landscape"></param>
        public GameCamera(GameScreen gameScreen) : base(gameScreen.Game)
        {
            this.gameScreen = gameScreen;
            this.landscape = gameScreen.Landscape;
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
            float heightFactor = ( radius - InitialHeight) / spaceBounds.Max.Z;
            float smoother = (float)(0.003f * gameTime.ElapsedGameTime.TotalMilliseconds);

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
                /*
                Vector3 cursor = gameScreen.CursorPosition;
                Vector3 arc = cursor - lookAt;
                Vector3 view = lookAt - game.PickRay.Position;
                arcBallRadius = arc.Length();
                dragFartherHalf = (Vector3.Dot(arc, view) > 0);
                startSpherePosition = XYZToSphere(arc);
                 */
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
                /*
                // Ray sphere test
                Vector3 eyeToLookAt = lookAt - game.PickRay.Position;
                float a = Vector3.Dot(eyeToLookAt, game.PickRay.Direction);
                float b = Vector3.Subtract(a * game.PickRay.Direction, eyeToLookAt).Length();
                float c = (float)Math.Sqrt((float)(arcBallRadius * arcBallRadius - b * b));
                Vector3 final = game.PickRay.Position + game.PickRay.Direction *
                                (dragFartherHalf ? a + c : a - c);

                // Transform to sphere coordinate system
                Vector2 offset = XYZToSphere(final - lookAt) - startSpherePosition;
                */
                pitch = startPitch +
                    (Input.MousePosition.X - startMousePosition.X) * RotationFactorX;
                roll = startRoll -
                    (Input.MousePosition.Y - startMousePosition.Y) * RotationFactorY;
            }
            else
            {
                float xDelta = 0;
                float yDelta = 0;
                bool xMove = false;
                bool yMove = false;
                speed = baseSpeed + heightFactor * SpeedHeightFactor;

                // Navigation
                if (Input.Keyboard.IsKeyDown(Keys.A) ||
                    Input.MousePosition.X < NavigateAreaSize)
                {
                    xMove = true;
                    xDelta =  (float)(speed * Math.Sin(pitchSmoother)) * elapsedTime;
                    yDelta -= (float)(speed * Math.Cos(pitchSmoother)) * elapsedTime;
                }
                else if (
                    Input.Keyboard.IsKeyDown(Keys.D) ||
                    Input.MousePosition.X > game.ScreenWidth - NavigateAreaSize)
                {
                    xMove = true;
                    xDelta -= (float)(speed * Math.Sin(pitchSmoother)) * elapsedTime;
                    yDelta =  (float)(speed * Math.Cos(pitchSmoother)) * elapsedTime;
                }

                if (Input.Keyboard.IsKeyDown(Keys.W) ||
                    Input.MousePosition.Y < NavigateAreaSize)
                {
                    yMove = true;
                    xDelta -= (float)(speed * Math.Cos(pitchSmoother)) * elapsedTime;
                    yDelta -= (float)(speed * Math.Sin(pitchSmoother)) * elapsedTime;
                }
                else if (
                    Input.Keyboard.IsKeyDown(Keys.S) ||
                    Input.MousePosition.Y > game.ScreenHeight - NavigateAreaSize)
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
                pitch -= 0.4f * RotationFactorX * elapsedTime;
            else if (Input.Keyboard.IsKeyDown(Keys.Right))
                pitch += 0.4f * RotationFactorX * elapsedTime;

            if (Input.Keyboard.IsKeyDown(Keys.Up))
                roll += 0.2f * RotationFactorY * elapsedTime;
            else if (Input.Keyboard.IsKeyDown(Keys.Down))
                roll -= 0.2f * RotationFactorY * elapsedTime;

            // Restrict roll
            if (roll < minRoll)
                roll = minRoll;
            else if (roll > maxRoll)
                roll = maxRoll;
            
            // Adjust view distance (radius) using mouse wheel or '+', '-' button
            radius -= Input.MouseWheelDelta * WheelFactor * (10 * heightFactor + 1);
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
                float height = landscape.GetHeight(eye.X, eye.Y) * 1.2f;
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


