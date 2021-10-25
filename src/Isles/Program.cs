// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Isles
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using var game = new GameIsles();
            game.Run();
        }
    }
}

