//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        private List<TValue> pendingAdds = new List<TValue>();

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

            foreach (TValue e in pendingAdds)
                elements.Add(e);

            pendingDeletes.Clear();
            pendingAdds.Clear();
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
            pendingAdds.Add(e);
        }

        public bool Remove(TValue e)
        {
            pendingDeletes.Add(e);
            return true;
        }

        public void Clear()
        {
            clear = true;
            pendingAdds.Clear();
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

    #region Property
    /// <summary>
    /// A set of property to describe an object
    /// 
    /// 
    /// We want to be able to do this:
    /// 
    /// GameDefault:
    /// <Avator Model="models/avator">
    ///     <Spells>
    ///         <Fireball Level="1" />
    ///         <Windwalk Level="1" />
    ///     </Spells>
    ///     <Items>
    ///         <HealthPotion Power="300" />
    ///         <HealthPotion Power="300" />
    ///     </Items>
    /// </Avator>
    /// 
    /// GameWorld:
    /// 
    /// This avator has 2 spells and 2 items
    /// <Avator Position="1024, 512, 0" />
    /// 
    /// This avator has only 1 item
    /// <Avator Position="1024, 512, 0">
    ///     <Items>
    ///         <ManaPotion Power="500" />
    ///     </Items>
    /// </Avator>
    /// 
    /// </summary>
    public interface IProperty<TKey>
    {
        /// <summary>
        /// Gets the name of the property
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets all child properties
        /// </summary>
        IEnumerable<IProperty<TKey>> ChildNodes { get; }

        /// <summary>
        /// Adds a new child property
        /// </summary>
        void AppendChild(IProperty<TKey> child);

        /// <summary>
        /// Clear all child properties
        /// </summary>
        void ClearChildNodes();

        /// <summary>
        /// Removes an attribute
        /// </summary>
        /// <param name="key"></param>
        void RemoveAttribute(TKey key);

        /// <summary>
        /// Clear all attributes
        /// </summary>
        void ClearAttributes();

        /// <summary>
        /// Determines whether the this property contains an
        /// attribute with the specific key
        /// </summary>
        bool ContainsKey(TKey key);

        /// <summary>
        /// Gets an attribute of type string
        /// </summary>
        bool Read(TKey key, out string value);

        /// <summary>
        /// Gets an attribute of type int
        /// </summary>
        bool Read(TKey key, out int value);

        /// <summary>
        /// Gets an attribute of type float
        /// </summary>
        bool Read(TKey key, out float value);

        /// <summary>
        /// Gets an attribute of type double
        /// </summary>
        bool Read(TKey key, out double value);

        /// <summary>
        /// Gets an attribute of type vector2
        /// </summary>
        bool Read(TKey key, out Vector2 value);

        /// <summary>
        /// Gets an attribute of type vector3
        /// </summary>
        bool Read(TKey key, out Vector3 value);

        /// <summary>
        /// Gets an attribute of type vector4
        /// </summary>
        bool Read(TKey key, out Vector4 value);

        /// <summary>
        /// Gets an attribute of type matrix
        /// </summary>
        bool Read(TKey key, out Matrix value);

        /// <summary>
        /// Sets an attribute of type string
        /// </summary>
        void Write(TKey key, string value);

        /// <summary>
        /// Sets an attribute of type int
        /// </summary>
        void Write(TKey key, int value);

        /// <summary>
        /// Sets an attribute of type float
        /// </summary>
        void Write(TKey key, float value);

        /// <summary>
        /// Sets an attribute of type double
        /// </summary>
        void Write(TKey key, double value);

        /// <summary>
        /// Sets an attribute of type vector2
        /// </summary>
        void Write(TKey key, Vector2 value);

        /// <summary>
        /// Sets an attribute of type vector3
        /// </summary>
        void Write(TKey key, Vector3 value);

        /// <summary>
        /// Sets an attribute of type vector4
        /// </summary>
        void Write(TKey key, Vector4 value);

        /// <summary>
        /// Sets an attribute of type matrix
        /// </summary>
        void Write(TKey key, Matrix value);
    }

    /// <summary>
    /// Defines an xml style property
    /// </summary>
    public class XmlProperty : IProperty<string>
    {
        XmlElement xml;

        public XmlProperty(XmlElement element)
        {
            xml = element;
        }

        #region IProperty<string> Members
        public string Name
        {
            get { return xml.Name; }
        }

        public IEnumerable<IProperty<string>> ChildNodes
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public void AppendChild(IProperty<string> child)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void ClearChildNodes()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void RemoveAttribute(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void ClearAttributes()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool ContainsKey(string key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Read(string key, out string value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Read(string key, out int value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Read(string key, out float value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Read(string key, out double value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Read(string key, out Vector2 value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Read(string key, out Vector3 value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Read(string key, out Vector4 value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool Read(string key, out Matrix value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Write(string key, string value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Write(string key, int value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Write(string key, float value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Write(string key, Vector2 value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Write(string key, Vector3 value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Write(string key, Vector4 value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Write(string key, Matrix value)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        #endregion
    }
    #endregion
}
