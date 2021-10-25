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
            var i = handlers.FindIndex(item => item == handler);

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
}
