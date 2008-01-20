//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    #region Helper
    /// <summary>
    /// A simple helper class
    /// </summary>
    public static class Helper
    {
        #region String parser
        public static Vector2 StringToVector2(string value)
        {
            string[] split = value.Split(new Char[] { ',' }, 2);

            if (split.Length >= 2)
                return new Vector2(
                    float.Parse(split[0]),
                    float.Parse(split[1]));

            return Vector2.Zero;
        }

        public static Vector3 StringToVector3(string value)
        {
            string[] split = value.Split(new Char[] { ',' }, 3);

            if (split.Length >= 3)
                return new Vector3(
                    float.Parse(split[0]),
                    float.Parse(split[1]),
                    float.Parse(split[2]));
            
            return Vector3.Zero;
        }

        public static Matrix StringToMatrix(string value)
        {
            string[] split = value.Split(new Char[] { ',' }, 16);

            if (split.Length >= 16)
                return new Matrix(
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
                    float.Parse(split[15]));

            return Matrix.Identity;
        }
        #endregion
    }
    #endregion

    #region BroadcastList
    /// <summary>
    /// A list, allow safe deletion of objects
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <remarks>
    /// Remove objects until update is called
    /// </remarks>
    public class BroadcastList<TValue, TList> 
        : IEnumerable<TValue>, ICollection<TValue> where TList : ICollection<TValue>, new()
    {
        bool clear;
        private TList elements = new TList();
        private List<TValue> pendingDeletes = new List<TValue>();

        public void Update()
        {
            if (clear)
            {
                elements.Clear();
                clear = false;
            }
            else
            {
                foreach (TValue e in pendingDeletes)
                    elements.Remove(e);
            }

            pendingDeletes.Clear();
        }

        public TList Elements
        {
            get { return elements; }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TValue e)
        {
            elements.Add(e);
        }

        public bool Remove(TValue e)
        {
            pendingDeletes.Add(e);
            return true;
        }

        public void Clear()
        {
            clear = true;
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public int Count
        {
            get { return elements.Count; }
        }

        public bool Contains(TValue item)
        {
            return elements.Contains(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            elements.CopyTo(array, arrayIndex);
        }
    }
    #endregion
}
