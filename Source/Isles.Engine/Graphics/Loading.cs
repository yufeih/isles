//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    /// <summary>
    /// Used to presents loading progress
    /// </summary>
    public class Loading
    {
        /// <summary>
        /// Game graphics
        /// </summary>
        GraphicsDevice graphics;

        /// <summary>
        /// Current progress
        /// </summary>
        float progress;

        /// <summary>
        /// Loading message
        /// </summary>
        string message = "Loading...";

        /// <summary>
        /// Gets or sets current progress.
        /// Progress value ranges from 0 to 100, when a value greater
        /// or equal then 100 is assigned, the LoadComplete event is triggered
        /// </summary>
        public float Progress
        {
            get { return progress; }
        }

        /// <summary>
        /// Refresh the loading screen with the new progress
        /// </summary>
        /// <param name="newProgress"></param>
        /// <param name="newMessage"></param>
        public void Refresh(float newProgress)
        {
            Refresh(newProgress, message);
        }

        /// <summary>
        /// Refresh the loading screen with the new progress and message
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public void Refresh(float newProgress, string newMessage)
        {
            message = newMessage;
            progress = newProgress;

            if (progress < 0)
                progress = 0;
            else if (progress > 100)
                progress = 100;

            Draw();
        }

        /// <summary>
        /// Gets or sets current message
        /// </summary>
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        /// <summary>
        /// Create a loading screen for a given graphics device
        /// </summary>
        /// <param name="graphics"></param>
        public Loading(GraphicsDevice graphics)
        {
            this.graphics = graphics;
        }

        /// <summary>
        /// Begins a new loading procedure
        /// </summary>
        public void Reset()
        {
            progress = 0;
            message = "Loading...";
        }

        /// <summary>
        /// Draw everything
        /// </summary>
        protected virtual void Draw()
        {
            graphics.Clear(Color.Black);

            Text.Sprite.Begin();
            Text.Sprite.DrawString(Text.Font, message + " " + progress + "%", Vector2.Zero, Color.Wheat);
            Text.Sprite.End();

            graphics.Present();
        }
    }
}
