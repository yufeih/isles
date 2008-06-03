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
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Cursor = System.Windows.Forms.Cursor;

namespace Isles.Engine
{
    #region Helper
    /// <summary>
    /// A simple helper class
    /// </summary>
    public static class Helper
    {
        #region String parser
        public static Color StringToColor(string value)
        {
            string[] split = value.Split(new char[] { ',' }, 3);

            if (split.Length >= 3)
                return new Color(byte.Parse(split[0]),
                                 byte.Parse(split[1]),
                                 byte.Parse(split[2]), 255);
            return Color.White;
        }

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

        public static Quaternion StringToQuaternion(string value)
        {
            string[] split = value.Split(new Char[] { ',' }, 4);

            if (split.Length >= 3)
                return new Quaternion(
                    float.Parse(split[0]),
                    float.Parse(split[1]),
                    float.Parse(split[2]),
                    float.Parse(split[3]));

            return Quaternion.Identity;
        }
        #endregion

        #region ToString
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
        #endregion

        #region Misc
        static Random random = new Random();

        /// <summary>
        /// Gets the global random number generator
        /// </summary>
        public static Random Random
        {
            get { return random; }
        }

        /// <summary>
        /// Gets a random number with a range
        /// </summary>
        public static float RandomInRange(float min, float max)
        {
            return min + (float)(random.NextDouble() * (max - min));
        }

#if FALSE
        /// <summary>
        /// Convert an object to a byte array
        /// </summary>
        public static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        /// <summary>
        /// Convert a byte array to an Object
        /// </summary>
        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }
#endif
        /// <summary>
        /// Convert an object to a byte array
        /// </summary>
        public static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            int structSize = Marshal.SizeOf(obj);
            IntPtr buffer = Marshal.AllocHGlobal(structSize);
            Marshal.StructureToPtr(obj, buffer, false);
            byte[] streamData = new byte[structSize];
            Marshal.Copy(buffer, streamData, 0, structSize);
            Marshal.FreeHGlobal(buffer);
            return streamData;
        }

        /// <summary>
        /// Convert a byte array to an Object
        /// </summary>
        public static Object ByteArrayToObject(byte[] rawData, Type type)
        {
            int rawSize = Marshal.SizeOf(type);
            if (rawSize > rawData.Length)
                return null;

            IntPtr buffer = Marshal.AllocHGlobal(rawSize);
            Marshal.Copy(rawData, 0, buffer, rawSize);
            object retObject = Marshal.PtrToStructure(buffer, type);
            Marshal.FreeHGlobal(buffer);
            return retObject;
        }

        /// <summary>
        /// Checks if two byte array equals
        /// </summary>
        public static bool ByteArrayEquals(byte[] raw1, byte[] raw2)
        {
            if (raw1.Length != raw2.Length)
                return false;

            for (int i = 0; i < raw1.Length; i++)
                if (raw1[i] != raw2[i])
                    return false;

            return true;
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
        private bool isDirty = true;
        private TList elements = new TList();
        private TList copy = new TList();

        public void Update()
        {

        }

        public TList Elements
        {
            get { return elements; }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            // Copy a new list whiling iterating it
            if (isDirty)
            {
                copy.Clear();
                foreach (TValue e in elements)
                    copy.Add(e);
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

    #region Obsolute
#if SOMETHINGSWRONGWITHTHIS
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
#endif
    #endregion
    #endregion

    #region Property
#if MAYBEWEDONOTNEETTHIS
    /// <summary>
    /// A set of property to describe an object
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
#endif
    #endregion
}
