//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;
using Isles.Engine;
using Isles.UI;

namespace Isles
{
    #region GameRecorder
    public class GameRecorder
    {
        #region Keyframe
        public struct Keyframe
        {
            public float Time;
            public ushort ID;
            public uint Offset;

            public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Keyframe));
        }
        #endregion

        /// <summary>
        /// Constants
        /// </summary>
        public const string Magic = "Isles Replay";
        public const byte Version = 0;

        uint currentOffset = 0;

        List<Keyframe> keyframes = new List<Keyframe>();
        List<byte[]> keyValues = new List<byte[]>();

        public GameRecorder()
        {
        }

        /// <summary>
        /// Record an id and its value
        /// </summary>
        public void Record(ushort id, float time, byte[] bytes, int offset, int length)
        {
            Keyframe frame;

            frame.ID = id;
            frame.Time = time;
            frame.Offset = currentOffset;

            byte[] value = new byte[length];
            for (int i = 0; i < length; i++)
                value[i] = bytes[i + offset];

            keyframes.Add(frame);
            keyValues.Add(value);

            currentOffset += (uint)(value.Length);
        }

        /// <summary>
        /// Clear all recorded data
        /// </summary>
        public void Clear()
        {
            currentOffset = 0;

            keyframes.Clear();
            keyValues.Clear();
        }

        /// <summary>
        /// Save the recorded data to an output stream
        /// </summary>
        public void Save(Stream output, string worldFilename, Stream worldStream)
        {
            // Write replay header
            byte[] magic = Encoding.ASCII.GetBytes(Magic);            
            output.Write(magic, 0, magic.Length);
            output.WriteByte(Version);

            // Write map info & map identifier
            if (worldFilename.Length > byte.MaxValue)
                throw new Exception("Filename too long: " + worldFilename);

            output.WriteByte((byte)(worldFilename.Length));
            byte[] file = Encoding.Default.GetBytes(worldFilename);
            output.Write(file, 0, file.Length);

            // Compute MD5 hash key
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] worldIdentifier = md5.ComputeHash(worldStream);
            output.Write(worldIdentifier, 0, worldIdentifier.Length);

            // Write keyframe count
            byte[] keyframeCount = BitConverter.GetBytes(keyframes.Count);
            output.Write(keyframeCount, 0, keyframeCount.Length);

            // Store header offset
            uint headerOffset = (uint)output.Position;

            // Write each keyframe
            foreach (Keyframe keyframe in keyframes)
            {
                // Copy the keyframe.
                Keyframe frame = keyframe;

                // Reset keyframe offset
                frame.Offset += headerOffset + (uint)(keyframes.Count * Keyframe.SizeInBytes);

                // Convert keyframe to bytes
                byte[] bytes = Helper.ObjectToByteArray(frame);

                // Write it into the output stream
                output.Write(bytes, 0, bytes.Length);
            }

            // Write each key value
            foreach (byte[] value in keyValues)
            {
                output.Write(value, 0, value.Length);
            }
        }
    }
    #endregion

    #region GameReplay
    public class GameReplay
    {
        #region Keyframe
        public struct Keyframe
        {
            public float Time;
            public ushort ID;
            public byte[] Bytes;
        }
        #endregion
        
        int currentFrame;

        BaseGame game;

        List<Keyframe> keyframes;

        string worldFilename;

        /// <summary>
        /// Gets the world filename of this replay
        /// </summary>
        public string WorldFilename
        {
            get { return worldFilename; }
        }

        public GameReplay(BaseGame game)
        {
            this.game = game;
        }

        /// <summary>
        /// Loads the game replay from an input stream
        /// </summary>
        public bool Load(Stream input)
        {
            // Read replay header
            byte[] magic = Encoding.ASCII.GetBytes(GameRecorder.Magic);
            byte[] buffer = new byte[magic.Length];

            int bytesRead = input.Read(buffer, 0, magic.Length);

            if (bytesRead != magic.Length || !Helper.ByteArrayEquals(magic, buffer))
                return false;

            int version = input.ReadByte();

            // Check replay version
            if (version != (int)GameRecorder.Version)
                return false;

            // Read map info
            int filenameLength = input.ReadByte();
            if (filenameLength < 0)
                return false;

            // World filename
            if (buffer.Length < filenameLength)
                buffer = new byte[filenameLength];

            bytesRead = input.Read(buffer, 0, filenameLength);
            
            if (bytesRead != filenameLength)
                return false;

            worldFilename = Encoding.Default.GetString(buffer, 0, filenameLength);

            // Compute the MD5 hash key of the world file
            byte[] worldMD5;

            using (Stream world = game.ZipContent.GetFileStream(worldFilename))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                worldMD5 = md5.ComputeHash(world);
            }

            // Check world file MD5 hash key. Make sure the world is the same as recorded.
            const int MD5HashLength = 16;

            buffer = new byte[MD5HashLength];

            bytesRead = input.Read(buffer, 0, MD5HashLength);

            if (bytesRead != MD5HashLength || !Helper.ByteArrayEquals(worldMD5, buffer))
                return false;

            // Read keyframes
            bytesRead = input.Read(buffer, 0, 4);
            if (bytesRead != 4)
                return false;

            int keyframeCount = BitConverter.ToInt32(buffer, 0);
            if (keyframeCount <= 0)
                return false;

            List<Keyframe> keyframes = new List<Keyframe>(keyframeCount);
            uint[] offsets = new uint[keyframeCount + 1];
            offsets[keyframeCount] = (uint)input.Length;

            // Read each individual keyframe
            buffer = new byte[GameRecorder.Keyframe.SizeInBytes];
            for (int i = 0; i < keyframeCount; i++)
            {
                bytesRead = input.Read(buffer, 0, buffer.Length);

                if (bytesRead != buffer.Length)
                    return false;

                // Convert from bytes to keyframe
                GameRecorder.Keyframe frame;
                frame = (GameRecorder.Keyframe)Helper.ByteArrayToObject(
                                  buffer, typeof(GameRecorder.Keyframe));

                // Set time and id
                Keyframe newFrame;
                newFrame.Bytes = null;
                newFrame.Time = frame.Time;
                newFrame.ID = frame.ID;
                keyframes.Add(newFrame);

                // Store frame offset
                offsets[i] = frame.Offset;
            }

            // Read each raw data associated with each keyframe
            for (int i = 0; i < keyframeCount; i++)
            {
                int length = (int)(offsets[i + 1]) - (int)(offsets[i]);

                if (length <= 0)
                    return false;

                byte[] bytes = new byte[length];

                input.Seek((long)offsets[i], SeekOrigin.Begin);
                
                bytesRead = input.Read(bytes, 0, length);

                if (bytesRead != length)
                    return false;

                Keyframe newFrame = keyframes[i];
                newFrame.Bytes = bytes;
                keyframes[i] = newFrame;
            }

            // Sort keyframes by time
            keyframes.Sort(delegate(Keyframe frame1, Keyframe frame2)
            {
                return frame1.Time.CompareTo(frame2.Time);
            });

            // Store it
            this.keyframes = keyframes;
            this.currentFrame = 0;

            return true;
        }

        public void Update(GameTime gameTime)
        {
            if (keyframes == null)
                return;

            GameServer server = GameServer.Singleton;

            while (currentFrame < keyframes.Count &&
                keyframes[currentFrame].Time >= (float)server.Time)
            {
                server.Execute(keyframes[currentFrame].ID,
                               keyframes[currentFrame].Bytes, 0,
                               keyframes[currentFrame].Bytes.Length);

                currentFrame++;
            }
        }
    }
    #endregion
}