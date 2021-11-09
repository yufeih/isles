// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public static class EnvironmentVariables
{
    public static string StartupLevel { get; } = GetString("ISLES_STARTUP_LEVEL");

    public static string GameDefaults { get; } = GetString("ISLES_GAME_DEFAULTS");

    private static string GetString(string key)
    {
        return Environment.GetEnvironmentVariable(key) is string value && !string.IsNullOrEmpty(value) ? value : null;
    }
}
