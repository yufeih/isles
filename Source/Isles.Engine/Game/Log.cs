//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

#region Using directives
using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Threading;
#endregion

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
    /// Orignally grabbed from Racing game
    /// </summary>
    public static class Log
    {
        #region Variables
        /// <summary>
        /// Writer
        /// </summary>
        private static StreamWriter writer = null;

        /// <summary>
        /// Log filename
        /// </summary>
        private const string LogFilename = "Log.txt";
        #endregion

        #region Static constructor to create log file
        /// <summary>
        /// Static constructor
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Open file
                FileStream file = new FileStream(
                    LogFilename, FileMode.OpenOrCreate,
                    FileAccess.Write, FileShare.ReadWrite);

                // Check if file is too big (more than 2 MB),
                // in this case we just kill it and create a new one :)
                if (file.Length > 2 * 1024 * 1024)
                {
                    file.Close();
                    file = new FileStream(
                        LogFilename, FileMode.Create,
                        FileAccess.Write, FileShare.ReadWrite );
                }
                // Associate writer with that, when writing to a new file,
                // make sure UTF-8 sign is written, else don't write it again!
                if (file.Length == 0)
                    writer = new StreamWriter(file,
                        System.Text.Encoding.UTF8);
                else
                    writer = new StreamWriter(file);

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
        #endregion

        #region Write log entry
        static public void NewLine()
        {
            writer.WriteLine("", false);
        }

        /// <summary>
        /// Writes a LogType and info/error message string to the Log file with time info
        /// </summary>
        /// <param name="message"></param>
        static public void Write(string message)
        {
            Write(message, true);
        }

        /// <summary>
        /// Writes a LogType and info/error message string to the Log file
        /// </summary>
        static public void Write(string message, bool writeTime)
        {
            // Can't continue without valid writer
            if (writer == null)
                return;

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
        #endregion
    }
}
