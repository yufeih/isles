//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.UI;

namespace Isles.Graphics
{
    #region ILoading
    /// <summary>
    /// Interface for tracking loading progress
    /// </summary>
    public interface ILoading
    {
        /// <summary>
        /// Gets or sets current message
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets current progress.
        /// </summary>
        float Progress { get; }

        void Refresh(float newProgress);

        /// <summary>
        /// Refresh the loading screen with the new progress and message
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        void Refresh(float newProgress, string newMessage);

        /// <summary>
        /// Begins a new loading procedure
        /// </summary>
        void Reset();
    }
    #endregion

    #region Loading
    /// <summary>
    /// Used to presents loading progress
    /// </summary>
    public class Loading : ILoading
    {
        /// <summary>
        /// 2D drawing functions
        /// </summary>
        Graphics2D graphics2D;

        /// <summary>
        /// Game graphics
        /// </summary>
        GraphicsDevice graphics;

        ProgressBar progressBar;

        /// <summary>
        /// Current progress
        /// </summary>
        float progress;

        UIDisplay uiDisplay;

        /// <summary>
        /// Loading message
        /// </summary>
        string message = "Loading...";

        /// <summary>
        /// Gets current progress.
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
            progressBar.SetProgress((int)newProgress);
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
        public Loading(GraphicsDevice graphics, Graphics2D gfx2D)
        {
            this.graphics2D = gfx2D;
            this.graphics = graphics;
            this.uiDisplay = new UIDisplay(BaseGame.Singleton);
            progressBar = new ProgressBar();
            progressBar.Texture = BaseGame.Singleton.ZipContent.Load<Texture2D>("UI/ProgressBar");
            progressBar.SourceRectangleLeftEnd = new Rectangle(0, 0, 64, 63);
            progressBar.SourceRectangleHightLight = new Rectangle(0, 126, 85, 63);
            progressBar.SourceRectangleRightEnd = new Rectangle(165, 0, 64, 63);
            progressBar.SourceRectangleFiller = new Rectangle(80, 0, 40, 63);
            progressBar.StartingTime = BaseGame.Singleton.CurrentGameTime.TotalGameTime.TotalSeconds;
            progressBar.HighLightRollingSpeed = 180;
            progressBar.HighLightLength = 50;
            progressBar.Area = new Rectangle(181, 269, 435, 6);//new Rectangle(181, 323, 435, 7);
            progressBar.Anchor = Anchor.TopLeft;
            progressBar.ScaleMode = ScaleMode.Stretch;

            double height = 875.0 * uiDisplay.Area.Width / 1400;

            Panel backgroundPanel = new Panel(new Rectangle(0, (int)((uiDisplay.Area.Height - height) / 2),
                                    uiDisplay.Area.Width, (int)height));
            backgroundPanel.Texture = BaseGame.Singleton.ZipContent.Load<Texture2D>("UI/LoadingDisplay");
            backgroundPanel.SourceRectangle = new Rectangle(0, 0, backgroundPanel.Texture.Width, backgroundPanel.Texture.Height);
            backgroundPanel.Anchor = Anchor.Center;
            backgroundPanel.ScaleMode = ScaleMode.Stretch;

            Panel panel = new Panel(new Rectangle(0, (int)((uiDisplay.Area.Height - height) / 2),
                                    uiDisplay.Area.Width, (int)height));
            panel.Texture = BaseGame.Singleton.ZipContent.Load<Texture2D>("UI/LoadingDisplay");
            panel.SourceRectangle = new Rectangle(0, 0, panel.Texture.Width, panel.Texture.Height);
            panel.Anchor = Anchor.Center;
            panel.ScaleMode = ScaleMode.Stretch;

            uiDisplay.Add(backgroundPanel);
            backgroundPanel.Add(progressBar);
            uiDisplay.Add(panel);
            uiDisplay.Draw(BaseGame.Singleton.CurrentGameTime);

            //if (BaseGame.Singleton.Settings.DirectEnter)
            //{
            //    progressBar.SetProgress(100);
            //    Draw();
            //}
            //else
            //{
            //    //progressBar.SetProgress(50);
            //    for (int i = 0; i <= 100; i++)
            //    {
            //        System.Threading.Thread.Sleep(1);
            //        progressBar.SetProgress(i);
            //        Draw();
            //    }
            //}
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
            //graphics2D.Sprite.Begin();
            //graphics2D.Sprite.DrawString(
            //    graphics2D.Font, message + " " + progress + "%",
            //    Vector2.Zero, Color.Wheat);
            //graphics2D.Sprite.End();

            graphics.Clear(Color.Black);
            uiDisplay.Draw(BaseGame.Singleton.CurrentGameTime);
            graphics.Present();
            if (progressBar.Persentage == 100 && !captured)
            {
                LoadingFinished = BaseGame.Singleton.ScreenshotCapturer.Screenshot;
                captured = true;
                Draw();
            }
        }

        bool captured = false;

        public Texture2D LoadingFinished;
    }
    #endregion
}
