// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Engine
{
    /// <summary>
    /// A simple helper class.
    /// </summary>
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

        public static Matrix StringToMatrix(string value)
        {
            var split = value.Split(new char[] { ',' }, 16);

            return split.Length >= 16
                ? new Matrix(
                    float.Parse(split[0]),
                    float.Parse(split[1]),
                    float.Parse(split[2]),
                    float.Parse(split[3]),
                    float.Parse(split[4]),
                    float.Parse(split[5]),
                    float.Parse(split[6]),
                    float.Parse(split[7]),
                    float.Parse(split[8]),
                    float.Parse(split[9]),
                    float.Parse(split[10]),
                    float.Parse(split[11]),
                    float.Parse(split[12]),
                    float.Parse(split[13]),
                    float.Parse(split[14]),
                    float.Parse(split[15]))
                : Matrix.Identity;
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

        public static string ColorToString(Color c)
        {
            return c.R + ", " + c.G + ", " + c.B;
        }

        public static string Vector2ToString(Vector2 v)
        {
            return v.X + ", " + v.Y;
        }

        public static string Vector3Tostring(Vector3 v)
        {
            return v.X + ", " + v.Y + ", " + v.Z;
        }

        public static string MatrixToString(Matrix m)
        {
            return
                m.M11 + ", " + m.M12 + ", " + m.M13 + ", " + m.M14 + ", " +
                m.M21 + ", " + m.M22 + ", " + m.M23 + ", " + m.M24 + ", " +
                m.M31 + ", " + m.M32 + ", " + m.M33 + ", " + m.M34 + ", " +
                m.M41 + ", " + m.M42 + ", " + m.M43 + ", " + m.M44;
        }

        public static string QuaternionTostring(Quaternion q)
        {
            return q.X + ", " + q.Y + ", " + q.Z + ", " + q.W;
        }

        private static readonly Random random = new();

        /// <summary>
        /// Gets the global random number generator.
        /// </summary>
        public static Random Random => random;

        /// <summary>
        /// Gets a random number with a range.
        /// </summary>
        public static float RandomInRange(float min, float max)
        {
            return min + (float)(random.NextDouble() * (max - min));
        }

        /// <summary>
        /// Convert an object to a byte array.
        /// </summary>
        public static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var structSize = Marshal.SizeOf(obj);
            IntPtr buffer = Marshal.AllocHGlobal(structSize);
            Marshal.StructureToPtr(obj, buffer, false);
            var streamData = new byte[structSize];
            Marshal.Copy(buffer, streamData, 0, structSize);
            Marshal.FreeHGlobal(buffer);
            return streamData;
        }

        /// <summary>
        /// Convert a byte array to an Object.
        /// </summary>
        public static object ByteArrayToObject(byte[] rawData, Type type)
        {
            var rawSize = Marshal.SizeOf(type);
            if (rawSize > rawData.Length)
            {
                return null;
            }

            IntPtr buffer = Marshal.AllocHGlobal(rawSize);
            Marshal.Copy(rawData, 0, buffer, rawSize);
            var retObject = Marshal.PtrToStructure(buffer, type);
            Marshal.FreeHGlobal(buffer);
            return retObject;
        }

        /// <summary>
        /// Checks if two byte array equals.
        /// </summary>
        public static bool ByteArrayEquals(byte[] raw1, byte[] raw2)
        {
            if (raw1.Length != raw2.Length)
            {
                return false;
            }

            for (var i = 0; i < raw1.Length; i++)
            {
                if (raw1[i] != raw2[i])
                {
                    return false;
                }
            }

            return true;
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
        private readonly TList elements = new();
        private readonly TList copy = new();

        public void Update()
        {
        }

        public TList Elements => elements;

        public IEnumerator<TValue> GetEnumerator()
        {
            // Copy a new list whiling iterating it
            if (isDirty)
            {
                copy.Clear();
                foreach (TValue e in elements)
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
            elements.Add(e);
        }

        public bool Remove(TValue e)
        {
            isDirty = true;
            return elements.Remove(e);
        }

        public void Clear()
        {
            isDirty = true;
            elements.Clear();
        }

        public bool IsReadOnly => true;

        public int Count => elements.Count;

        public bool Contains(TValue item)
        {
            return elements.Contains(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            elements.CopyTo(array, arrayIndex);
        }
    }
}
