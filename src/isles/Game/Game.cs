// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Isles;

public class GameIsles : BaseGame
{
    protected override void FirstTimeInitialize()
    {
        if (!string.IsNullOrEmpty(EnvironmentVariables.StartupLevel))
        {
            StartScreen(new GameScreen(EnvironmentVariables.StartupLevel));
        }
        else
        {
            StartScreen(new TitleScreen());
        }
    }
}
