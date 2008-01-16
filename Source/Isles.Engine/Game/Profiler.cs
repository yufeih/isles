using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Engine
{
    /// <summary>
    /// Frame rate profiler
    /// </summary>
    public class Profiler : DrawableGameComponent
    {
        private int updateCount = 0;
        private int currentFrame = 0;
        private int counter = 0;
        private double storedTime = 0;
        private float fps = 0;
        private float fpsInterpolated = 300.0f;
        private float overallFps = 0;
        
        /// <summary>
        /// Time needed to calculate FPS, Measured in milliseconds
        /// </summary>
        public const float UpdateFrequency = 1000;

        /// <summary>
        /// Gets the total number of frames since profiler started
        /// </summary>
        public int CurrentFrame
        {
            get { return currentFrame; }
        }

        /// <summary>
        /// Gets the average frame rate up until now
        /// </summary>
        public float OverallFPS
        {
            get { return overallFps; }
        }
  
        /// <summary>
        /// Gets the current Frame Per Second for the game
        /// </summary>
        public float FramesPerSecond
        {
            get { return fpsInterpolated; }
        }

        public Profiler() : base(null)
        {
        }

        /// <summary>
        /// The main constructor for the class.
        /// </summary>
        /// <param name="game">The <see cref="Microsoft.Xna.Framework.Game" /> instance for this <see cref="DrawableGameComponent"/> to use.</param>
        /// <remarks>Sets the <see cref="_gameWindowTitle"/> data member to the value of <see cref="Microsoft.Xna.Framework.GameWindow.Title"/>.</remarks>
        public Profiler(Game game) : base(game)
        {
        }
  
        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            Visible = true;
            Enabled = true;
            base.Initialize();

            Log.Write("Profiler Initialized...");
        }
  
        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
  
            if (!this.Enabled)
                return;

            counter++;
            currentFrame++;

            float elapsed = (float)(gameTime.TotalRealTime.TotalMilliseconds - storedTime);

            if (elapsed > UpdateFrequency)
            {
                fps = 1000 * counter / elapsed;
                counter = 0;
                storedTime = gameTime.TotalRealTime.TotalMilliseconds;

                fpsInterpolated =
                    MathHelper.Lerp(fpsInterpolated, fps, 0.5f);

                overallFps = ( overallFps * updateCount + fps ) / (updateCount + 1);
                updateCount++;
            }
        }
  
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
  
            if (!this.Visible || !this.Enabled)
                return;
            
            // Try if we can do without saving state changes
            Graphics.Text.DrawString("FPS: " + fpsInterpolated, 14, new Vector2(0, 0), Color.White);
        }
    }
}
