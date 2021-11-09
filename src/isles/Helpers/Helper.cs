// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Isles;

public static class Helper
{
    public static Color StringToColor(string value)
    {
        var split = value.Split(new char[] { ',' }, 3);

        return split.Length >= 3
            ? new Color(byte.Parse(split[0]),
                             byte.Parse(split[1]),
                             byte.Parse(split[2]), 255)
            : Color.White;
    }

    public static Vector2 StringToVector2(string value)
    {
        var split = value.Split(new char[] { ',' }, 2);

        return split.Length >= 2
            ? new Vector2(
                float.Parse(split[0]),
                float.Parse(split[1]))
            : Vector2.Zero;
    }

    public static Vector3 StringToVector3(string value)
    {
        var split = value.Split(new char[] { ',' }, 3);

        return split.Length >= 3
            ? new Vector3(
                float.Parse(split[0]),
                float.Parse(split[1]),
                float.Parse(split[2]))
            : Vector3.Zero;
    }

    public static Quaternion StringToQuaternion(string value)
    {
        var split = value.Split(new char[] { ',' }, 4);

        return split.Length >= 3
            ? new Quaternion(
                float.Parse(split[0]),
                float.Parse(split[1]),
                float.Parse(split[2]),
                float.Parse(split[3]))
            : Quaternion.Identity;
    }

    public static Random Random { get; } = new();

    public static float RandomInRange(float min, float max)
    {
        return min + (float)(Random.NextDouble() * (max - min));
    }
}

/// <summary>
/// A list, allow safe deletion of objects.
/// </summary>
/// <typeparam name="TValue"></typeparam>
/// <remarks>
/// Remove objects until update is called.
/// </remarks>
public class BroadcastList<TValue, TList>
    : IEnumerable<TValue>, ICollection<TValue> where TList : ICollection<TValue>, new()
{
    private bool isDirty = true;
    private readonly TList copy = new();

    public TList Elements { get; } = new();

    public IEnumerator<TValue> GetEnumerator()
    {
        // Copy a new list whiling iterating it
        if (isDirty)
        {
            copy.Clear();
            foreach (TValue e in Elements)
            {
                copy.Add(e);
            }

            isDirty = false;
        }

        return copy.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(TValue e)
    {
        isDirty = true;
        Elements.Add(e);
    }

    public bool Remove(TValue e)
    {
        isDirty = true;
        return Elements.Remove(e);
    }

    public void Clear()
    {
        isDirty = true;
        Elements.Clear();
    }

    public bool IsReadOnly => true;

    public int Count => Elements.Count;

    public bool Contains(TValue item)
    {
        return Elements.Contains(item);
    }

    public void CopyTo(TValue[] array, int arrayIndex)
    {
        Elements.CopyTo(array, arrayIndex);
    }
}
