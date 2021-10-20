// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Isles.Engine;

namespace Isles
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using BaseGame game = new GameIsles();
            game.Run();

            // Sucessfully exit the game
            Log.NewLine();
            Log.Write("Program Terminated. Overall FPS: " + game.Profiler.OverallFPS);
        }
    }
}

