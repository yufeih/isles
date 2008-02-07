//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Isles.Engine;

namespace Isles
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif

            using (BaseGame game = new GameIsles())
            {
                game.Run();

                // Sucessfully exit the game
                Log.NewLine();
                Log.Write("Program terminated. Overall FPS: " + game.Profiler.OverallFPS);
            }
#if !DEBUG
            }
            catch (Exception e)
            {
                MessageBox.Show("Error: " + e.Message + "\n\nSee log file for error detailes.", "Isles");
                Log.Write(e.Source + " - " + e.Message);
                Log.NewLine();
                Log.Write(e.StackTrace, false);
            }   
#endif
        }
    }
}

