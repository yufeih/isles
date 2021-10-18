// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Engine
{
    /// <summary>
    /// Log will create automatically a log file and write
    /// log/error/debug info for simple runtime error checking, very useful
    /// for minor errors, such as finding not files.
    /// The application can still continue working, but this log provides
    /// an easy support to find out what files are missing (in this example).
    ///
    /// Note: I don't use this class anymore for big projects, but its small
    /// and handy for smaller projects and nice to log non-debugable stuff.
    ///
    /// Orignally grabbed from Racing game.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Writer.
        /// </summary>
        private static StreamWriter writer;

        /// <summary>
        /// Log filename.
        /// </summary>
        private const string LogFilename = "Log.txt";

        /// <summary>
        /// Static constructor.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Open file
                var file = new FileStream(
                    LogFilename, FileMode.OpenOrCreate,
                    FileAccess.Write, FileShare.ReadWrite);

                // Check if file is too big (more than 2 MB),
                // in this case we just kill it and create a new one :)
                if (file.Length > 2 * 1024 * 1024)
                {
                    file.Close();
                    file = new FileStream(
                        LogFilename, FileMode.Create,
                        FileAccess.Write, FileShare.ReadWrite);
                }

                // Associate writer with that, when writing to a new file,
                // make sure UTF-8 sign is written, else don't write it again!
                writer = file.Length == 0
                    ? new StreamWriter(file,
                        System.Text.Encoding.UTF8)
                    : new StreamWriter(file);

                // Go to end of file
                writer.BaseStream.Seek(0, SeekOrigin.End);

                // Enable auto flush (always be up to date when reading!)
                writer.AutoFlush = true;
            }
            catch (IOException)
            {
                // Ignore any file exceptions, if file is not
                // createable (e.g. on a CD-Rom) it doesn't matter.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore any file exceptions, if file is not
                // createable (e.g. on a CD-Rom) it doesn't matter.
            }
        }

        static public void NewLine()
        {
            writer.WriteLine("", false);
        }

        /// <summary>
        /// Writes a LogType and info/error message string to the Log file with time info.
        /// </summary>
        /// <param name="message"></param>
        static public void Write(string message)
        {
            Write(message, true);
        }

        /// <summary>
        /// Writes a LogType and info/error message string to the Log file.
        /// </summary>
        static public void Write(string message, bool writeTime)
        {
            // Can't continue without valid writer
            if (writer == null)
            {
                return;
            }

            try
            {
                string s;

                if (writeTime)
                {
                    DateTime ct = DateTime.Now;
                    s = "[" + ct.Hour.ToString("00") + ":" +
                        ct.Minute.ToString("00") + ":" +
                        ct.Second.ToString("00") + "] " +
                        message;
                }
                else
                {
                    s = message;
                }

                writer.WriteLine(s);
#if DEBUG
                // In debug mode write that message to the console as well!
                System.Console.WriteLine(s);
#endif
            }
            catch (IOException)
            {
                // Ignore any file exceptions, if file is not
                // createable (e.g. on a CD-Rom) it doesn't matter.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore any file exceptions, if file is not
                // createable (e.g. on a CD-Rom) it doesn't matter.
            }
        }
    }

    public static class Timer
    {
        private static readonly List<IEventListener> handlers = new();
        private static readonly List<float> intervals = new();
        private static readonly List<float> times = new();

        /// <summary>
        /// Adds a new timer alert.
        /// </summary>
        /// <param name="interval">Time interval measured in seconds.</param>
        public static void Add(IEventListener handler, float interval)
        {
            if (handler == null || interval <= 0)
            {
                throw new ArgumentException();
            }

            if (handlers.Contains(handler))
            {
                throw new InvalidOperationException();
            }

            handlers.Add(handler);
            intervals.Add(interval);
            times.Add(0);
        }

        /// <summary>
        /// Removes a timer alert.
        /// </summary>
        public static void Remove(IEventListener handler)
        {
            var i = handlers.FindIndex(delegate(IEventListener item)
            {
                return item == handler;
            });

            if (i >= 0 && i < handlers.Count)
            {
                handlers.RemoveAt(i);
                intervals.RemoveAt(i);
                times.RemoveAt(i);
            }
        }

        /// <summary>
        /// Update the timer and triggers all timer events.
        /// </summary>
        /// <param name="gameTime"></param>
        public static void Update(GameTime gameTime)
        {
            for (var i = 0; i < handlers.Count; i++)
            {
                times[i] += (float)gameTime.ElapsedGameTime.TotalSeconds;

                while (times[i] > intervals[i])
                {
                    times[i] -= intervals[i];
                    Event.SendMessage(EventType.TimerTick, handlers[i], null, null, 0);
                }
            }
        }
    }

    /// <summary>
    /// Frame rate profiler.
    /// </summary>
    public class Profiler : DrawableGameComponent
    {
        private readonly BaseGame game;
        private int updateCount;
        private int currentFrame;
        private int counter;
        private double storedTime;
        private float fps;
        private float fpsInterpolated = 60.0f;
        private float overallFps;

        /// <summary>
        /// Time needed to calculate FPS, Measured in milliseconds.
        /// </summary>
        public const float UpdateFrequency = 1000;

        /// <summary>
        /// Gets the total number of frames since profiler started.
        /// </summary>
        public int CurrentFrame => currentFrame;

        /// <summary>
        /// Gets the average frame rate up until now.
        /// </summary>
        public float OverallFPS => overallFps;

        /// <summary>
        /// Gets the current Frame Per Second for the game.
        /// </summary>
        public float FramesPerSecond => fpsInterpolated;

        public Profiler()
            : base(null)
        {
        }

        /// <summary>
        /// The main constructor for the class.
        /// </summary>
        /// <param name="game">The <see cref="Game" /> instance for this <see cref="DrawableGameComponent"/> to use.</param>
        /// <remarks>Sets the <see cref="_gameWindowTitle"/> data member to the value of <see cref="GameWindow.Title"/>.</remarks>
        public Profiler(BaseGame game)
            : base(game)
        {
            this.game = game;
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
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!Enabled)
            {
                return;
            }

            counter++;
            currentFrame++;

            var elapsed = (float)(gameTime.TotalRealTime.TotalMilliseconds - storedTime);

            if (elapsed > UpdateFrequency)
            {
                fps = 1000 * counter / elapsed;
                counter = 0;
                storedTime = gameTime.TotalRealTime.TotalMilliseconds;

                fpsInterpolated =
                    MathHelper.Lerp(fpsInterpolated, fps, 0.5f);

                overallFps = (overallFps * updateCount + fps) / (updateCount + 1);
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

            if (!Visible || !Enabled)
            {
                return;
            }

            // Try if we can do without saving state changes
            game.Graphics2D.DrawString("FPS: " + fpsInterpolated, 16f / 23, new Vector2(0, 0), Color.White);
        }
    }

    /// <summary>
    /// Screenshot capturer grabbed from Racing game.
    /// </summary>
    public partial class ScreenshotCapturer : GameComponent
    {
        private const string ScreenshotsDirectory = "Screenshots";

        /// <summary>
        /// Internal screenshot number (will increase by one each screenshot).
        /// </summary>
        private int screenshotNum;

        /// <summary>
        /// Link to BaseGame class instance. Also holds windows title,
        /// which is used instead of Application.ProgramName.
        /// </summary>
        private readonly BaseGame game;

        /// <summary>
        /// If it's true, the game should call TakeScreenshot at the end of the frame.
        /// </summary>
        private bool shouldCapture;

        public bool ShouldCapture
        {
            get => shouldCapture;
            set => shouldCapture = value;
        }

        public Texture2D Screenshot
        {
            get
            {
                if (screenshot == null)
                {
                    screenshot = new ResolveTexture2D(
                        game.GraphicsDevice,
                        game.ScreenWidth, game.ScreenHeight, 1,
                        SurfaceFormat.Color);

                    // Get data with help of the resolve method
                    game.GraphicsDevice.ResolveBackBuffer(screenshot);
                }

                return screenshot;
            }
        }

        private ResolveTexture2D screenshot;

        public ScreenshotCapturer(BaseGame setGame)
            : base(setGame)
        {
            game = setGame;
            screenshotNum = GetCurrentScreenshotNum();
        }

        /// <summary>
        /// Screenshot name builder.
        /// </summary>
        /// <param name="num">Num.</param>
        /// <returns>String.</returns>
        private string ScreenshotNameBuilder(int num)
        {
            return ScreenshotsDirectory + "/" +
                game.Window.Title + " Screenshot " +
                num.ToString("0000") + ".jpg";
        }

        /// <summary>
        /// Get current screenshot num.
        /// </summary>
        /// <returns>Int.</returns>
        private int GetCurrentScreenshotNum()
        {
            // We must search for last screenshot we can found in list using own
            // fast filesearch
            int i = 0, j = 0, k = 0, l = -1;
            // First check if at least 1 screenshot exist
            if (File.Exists(ScreenshotNameBuilder(0)) == true)
            {
                // First scan for screenshot num/1000
                for (i = 1; i < 10; i++)
                {
                    if (File.Exists(ScreenshotNameBuilder(i * 1000)) == false)
                    {
                        break;
                    }
                }

                // This i*1000 does not exist, continue scan next level
                // screenshotnr/100
                i--;
                for (j = 1; j < 10; j++)
                {
                    if (File.Exists(ScreenshotNameBuilder(i * 1000 + j * 100)) == false)
                    {
                        break;
                    }
                }

                // This i*1000+j*100 does not exist, continue scan next level
                // screenshotnr/10
                j--;
                for (k = 1; k < 10; k++)
                {
                    if (File.Exists(ScreenshotNameBuilder(
                            i * 1000 + j * 100 + k * 10)) == false)
                    {
                        break;
                    }
                }

                // This i*1000+j*100+k*10 does not exist, continue scan next level
                // screenshotnr/1
                k--;
                for (l = 1; l < 10; l++)
                {
                    if (File.Exists(ScreenshotNameBuilder(
                            i * 1000 + j * 100 + k * 10 + l)) == false)
                    {
                        break;
                    }
                }

                // This i*1000+j*100+k*10+l does not exist, we have now last
                // screenshot nr!!!
                l--;
            }

            return i * 1000 + j * 100 + k * 10 + l;
        }

        private IEventListener photographer;

        /// <summary>
        /// Gets the screenshot for this frame.
        /// The screenshot texture will be given to the photographer in the next frame.
        /// Only one photographer is allowed.
        /// </summary>
        public void TakeScreenshot(IEventListener photographer)
        {
            this.photographer = photographer;
            shouldCapture = true;
        }

        /// <summary>
        /// Make screenshot.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "App should not crash from making a screenshot, " +
            "but exceptions are often thrown by this code, see inside.")]
        public void TakeScreenshot()
        {
            try
            {
                // NOTE: This doesn't always work on all cards, especially if
                // desktop mode switches in fullscreen mode!
                if (photographer == null)
                {
                    screenshotNum++;
                    // Make sure screenshots directory exists
                    if (Directory.Exists(ScreenshotsDirectory) == false)
                    {
                        Directory.CreateDirectory(ScreenshotsDirectory);
                    }

                    using var dstTexture = new ResolveTexture2D(
                        game.GraphicsDevice,
                        game.ScreenWidth, game.ScreenHeight, 1,
                        SurfaceFormat.Color);
                    // Get data with help of the resolve method
                    game.GraphicsDevice.ResolveBackBuffer(dstTexture);

                    dstTexture.Save(
                        ScreenshotNameBuilder(screenshotNum),
                        ImageFileFormat.Bmp);

                    Log.Write("Screen shot captured: " + ScreenshotNameBuilder(screenshotNum));
                }
                else
                {
                    var dstTexture = new ResolveTexture2D(
                        game.GraphicsDevice,
                        game.ScreenWidth, game.ScreenHeight, 1,
                        SurfaceFormat.Color);

                    // Get data with help of the resolve method
                    game.GraphicsDevice.ResolveBackBuffer(dstTexture);

                    Event.SendMessage(EventType.ScreenshotTaken, photographer, this, dstTexture);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Failed to save Screenshot: " + ex.ToString());
            }
            finally
            {
                shouldCapture = false;
            }
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            if (screenshot != null)
            {
                // screenshot.Dispose();
                screenshot = null;
            }

            // if (Input.KeyboardKeyJustPressed(Keys.PrintScreen))
            // {
            //    shouldCapture = true;
            //    //TakeScreenshot();
            // }
            base.Update(gameTime);
        }
    }

    public class PathGraphVisualizer : DrawableGameComponent
    {
        private readonly BaseGame game;
        private readonly PathGraph graph;
        private readonly BasicEffect effect;
        private readonly GraphicsDevice graphics;
        private readonly VertexDeclaration declaraction;
        private readonly List<int> indices = new();
        private readonly List<VertexPositionColor> vertices = new();

        public PathGraphVisualizer(BaseGame game, PathGraph graph)
            : base(game)
        {
            if (game.GraphicsDevice == null || graph == null)
            {
                throw new InvalidOperationException();
            }

            this.game = game;
            this.graph = graph;
            graphics = game.GraphicsDevice;
            effect = new BasicEffect(graphics, null);
            declaraction = new VertexDeclaration(graphics, VertexPositionColor.VertexElements);
        }

        public override void Draw(GameTime gameTime)
        {
            graphics.VertexDeclaration = declaraction;
            graphics.RenderState.DepthBufferFunction = CompareFunction.Always;

            effect.World = Matrix.Identity;
            effect.View = game.View;
            effect.Projection = game.Projection;

            effect.Begin();
            effect.CurrentTechnique.Passes[0].Begin();

            BuildMesh();

            graphics.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Count, indices.ToArray(), 0, indices.Count / 3);

            effect.CurrentTechnique.Passes[0].End();
            effect.End();

            graphics.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
        }

        private void BuildMesh()
        {
            vertices.Clear();
            indices.Clear();

            Vector3 v;
            Color color = Color.RoyalBlue;

            for (var y = 0; y < graph.EntryHeight; y++)
            {
                for (var x = 0; x < graph.EntryWidth; x++)
                {
                    if (graph.IsGridObstructed(x, y, true))
                    {
                        Vector2 p = graph.GridToPosition(x, y);
                        v.X = p.X;
                        v.Y = p.Y;
                        v.Z = graph.Landscape.GetHeight(x, y);

                        if (game.ViewFrustum.Contains(v) == ContainmentType.Contains)
                        {
                            var bias = vertices.Count;

                            vertices.Add(new VertexPositionColor(
                                new Vector3(v.X - graph.HalfCellSize, v.Y - graph.HalfCellSize, v.Z), color));
                            vertices.Add(new VertexPositionColor(
                                new Vector3(v.X - graph.HalfCellSize, v.Y + graph.HalfCellSize, v.Z), color));
                            vertices.Add(new VertexPositionColor(
                                new Vector3(v.X + graph.HalfCellSize, v.Y + graph.HalfCellSize, v.Z), color));
                            vertices.Add(new VertexPositionColor(
                                new Vector3(v.X + graph.HalfCellSize, v.Y - graph.HalfCellSize, v.Z), color));

                            indices.Add(bias + 0);
                            indices.Add(bias + 1);
                            indices.Add(bias + 2);
                            indices.Add(bias + 0);
                            indices.Add(bias + 2);
                            indices.Add(bias + 3);
                        }
                    }
                }
            }
        }
    }
}
