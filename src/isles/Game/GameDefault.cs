// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Isles;

/// <summary>
/// Default settings for game entities and spells.
/// </summary>
public class GameDefault
{
    public Dictionary<string, JsonElement> Prefabs = new();

    private readonly Dictionary<string, float> lumber = new();
    private readonly Dictionary<string, float> gold = new();
    private readonly Dictionary<string, float> food = new();
    private readonly Dictionary<string, bool> isUnique = new();

    /// <summary>
    /// Gets the lumber property of a given type.
    /// </summary>
    public float GetLumber(string type)
    {
        return lumber.TryGetValue(type, out var i) ? i : GetSingle("Lumber");
    }

    /// <summary>
    /// Gets the gold property of a given type.
    /// </summary>
    public float GetGold(string type)
    {
        return gold.TryGetValue(type, out var i) ? i : GetSingle("Gold");
    }

    /// <summary>
    /// Gets the food property of a given type.
    /// </summary>
    public float GetFood(string type)
    {
        return food.TryGetValue(type, out var i) ? i : GetSingle("Food");
    }

    /// <summary>
    /// Gets whether the given type is a unique.
    /// </summary>
    public bool IsUnique(string type)
    {
        return isUnique.TryGetValue(type, out var i) ? i : GetBoolean("IsUnique");
    }

    /// <summary>
    /// Sets a type as unique.
    /// </summary>
    public void SetUnique(string type)
    {
        isUnique[type] = true;
    }

    /// <summary>
    /// Load the game defaults from a stream.
    /// </summary>
    /// <param name="stream"></param>
    public GameDefault()
    {
        Prefabs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllBytes("data/settings/prefabs.json"));
    }

    public static GameDefault Singleton { get; } = new GameDefault();

    private float GetSingle(string key)
    {
        return Prefabs.TryGetValue(key, out var element) && element.TryGetProperty(key, out var property) ? property.GetSingle() : default;
    }

    private bool GetBoolean(string key)
    {
        return Prefabs.TryGetValue(key, out var element) && element.TryGetProperty(key, out var property) ? property.GetBoolean() : default;
    }
}
