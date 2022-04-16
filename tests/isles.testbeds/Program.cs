// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

class MoveTestBeds : Game
{


    public static void Main()
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        using var testBeds = new MoveTestBeds();
        testBeds.Run();
    }
}