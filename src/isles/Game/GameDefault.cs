// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Xml;

namespace Isles;

/// <summary>
/// Default settings for game entities and spells.
/// </summary>
public class GameDefault
{
    /// <summary>
    /// Gets or sets the default attributes for world objects.
    /// The key of the outer dictionary is the type name of a world object,
    /// the inner dictionary stores its default {attribute, value} pair.
    /// </summary>
    public Dictionary<string, XmlElement> WorldObjectDefaults = new();

    /// <summary>
    /// Gets or sets the default attributes for spells.
    /// </summary>
    public Dictionary<string, XmlElement>  SpellDefaults = new();

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
        if (lumber.TryGetValue(type, out var i))
        {
            return i;
        }

        string value;
        if (WorldObjectDefaults.TryGetValue(type, out XmlElement element))
        {
            if ((value = element.GetAttribute("Lumber")) != "")
            {
                i = float.Parse(value);
                lumber.Add(type, i);
                return i;
            }
        }

        if (SpellDefaults.TryGetValue(type, out element))
        {
            if ((value = element.GetAttribute("Lumber")) != "")
            {
                i = float.Parse(value);
                lumber.Add(type, i);
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// Gets the gold property of a given type.
    /// </summary>
    public float GetGold(string type)
    {
        if (gold.TryGetValue(type, out var i))
        {
            return i;
        }

        string value;
        if (WorldObjectDefaults.TryGetValue(type, out XmlElement element))
        {
            if ((value = element.GetAttribute("Gold")) != "")
            {
                i = float.Parse(value);
                gold.Add(type, i);
                return i;
            }
        }

        if (SpellDefaults.TryGetValue(type, out element))
        {
            if ((value = element.GetAttribute("Gold")) != "")
            {
                i = float.Parse(value);
                gold.Add(type, i);
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// Gets the food property of a given type.
    /// </summary>
    public float GetFood(string type)
    {
        if (food.TryGetValue(type, out var i))
        {
            return i;
        }

        string value;
        if (WorldObjectDefaults.TryGetValue(type, out XmlElement element))
        {
            if ((value = element.GetAttribute("Food")) != "")
            {
                i = float.Parse(value);
                food.Add(type, i);
                return i;
            }
        }

        if (SpellDefaults.TryGetValue(type, out element))
        {
            if ((value = element.GetAttribute("Food")) != "")
            {
                i = float.Parse(value);
                food.Add(type, i);
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// Gets whether the given type is a unique.
    /// </summary>
    public bool IsUnique(string type)
    {
        if (isUnique.TryGetValue(type, out var i))
        {
            return i;
        }

        string value;
        if (WorldObjectDefaults.TryGetValue(type, out XmlElement element))
        {
            if ((value = element.GetAttribute("IsUnique")) != "")
            {
                i = bool.Parse(value);
                isUnique.Add(type, i);
                return i;
            }
        }

        return false;
    }

    /// <summary>
    /// Sets a type as unique.
    /// </summary>
    public void SetUnique(string type)
    {
        if (isUnique.ContainsKey(type))
        {
            isUnique[type] = true;
        }
        else
        {
            isUnique.Add(type, true);
        }
    }

    /// <summary>
    /// Load the game defaults from a stream.
    /// </summary>
    /// <param name="stream"></param>
    public GameDefault()
    {
        var doc = new XmlDocument();
        doc.Load(EnvironmentVariables.GameDefaults ?? "data/settings/defaults.xml");

        if (doc.DocumentElement.Name != "GameDefault")
        {
            throw new InvalidDataException();
        }

        // Gets world object defaults
        if (doc.DocumentElement.SelectSingleNode("WorldObject") is XmlElement element)
        {
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node is XmlElement child)
                {
                    WorldObjectDefaults.Add(child.Name, child);
                }
            }
        }

        // Gets spell defaults
        element = doc.DocumentElement.SelectSingleNode("Spell") as XmlElement;

        if (element != null)
        {
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node is XmlElement child)
                {
                    SpellDefaults.Add(child.Name, child);
                }
            }
        }

        Prefabs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllBytes("data/settings/prefabs.json"));
    }

    public static GameDefault Singleton { get; } = new GameDefault();
}
