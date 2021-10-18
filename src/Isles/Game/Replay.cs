// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Isles.Engine;
using Microsoft.Xna.Framework;

namespace Isles
{
    public class GameRecorder
    {
        public struct Keyframe
        {
            public float Time;
            public ushort ID;
            public uint Offset;

            public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Keyframe));
        }

        /// <summary>
        /// Constants.
        /// </summary>
        public const string Magic = "Isles Replay";
        public const byte Version = 0;
        private uint currentOffset;
        private readonly List<Keyframe> keyframes = new();
        private readonly List<byte[]> keyValues = new();

        public GameRecorder()
        {
        }

        /// <summary>
        /// Record an id and its value.
        /// </summary>
        public void Record(ushort id, float time, byte[] bytes, int offset, int length)
        {
            Keyframe frame;

            frame.ID = id;
            frame.Time = time;
            frame.Offset = currentOffset;

            var value = new byte[length];
            for (var i = 0; i < length; i++)
            {
                value[i] = bytes[i + offset];
            }

            keyframes.Add(frame);
            keyValues.Add(value);

            currentOffset += (uint)value.Length;
        }

        /// <summary>
        /// Clear all recorded data.
        /// </summary>
        public void Clear()
        {
            currentOffset = 0;

            keyframes.Clear();
            keyValues.Clear();
        }

        /// <summary>
        /// Save the recorded data to an output stream.
        /// </summary>
        public void Save(Stream output, string worldFilename, Stream worldStream)
        {
            // Write replay header
            var magic = Encoding.ASCII.GetBytes(Magic);
            output.Write(magic, 0, magic.Length);
            output.WriteByte(Version);

            // Write map info & map identifier
            if (worldFilename.Length > byte.MaxValue)
            {
                throw new Exception("Filename too long: " + worldFilename);
            }

            output.WriteByte((byte)worldFilename.Length);
            var file = Encoding.Default.GetBytes(worldFilename);
            output.Write(file, 0, file.Length);

            // Compute MD5 hash key
            MD5 md5 = new MD5CryptoServiceProvider();
            var worldIdentifier = md5.ComputeHash(worldStream);
            output.Write(worldIdentifier, 0, worldIdentifier.Length);

            // Write keyframe count
            var keyframeCount = BitConverter.GetBytes(keyframes.Count);
            output.Write(keyframeCount, 0, keyframeCount.Length);

            // Store header offset
            var headerOffset = (uint)output.Position;

            // Write each keyframe
            foreach (Keyframe keyframe in keyframes)
            {
                // Copy the keyframe.
                Keyframe frame = keyframe;

                // Reset keyframe offset
                frame.Offset += headerOffset + (uint)(keyframes.Count * Keyframe.SizeInBytes);

                // Convert keyframe to bytes
                var bytes = Helper.ObjectToByteArray(frame);

                // Write it into the output stream
                output.Write(bytes, 0, bytes.Length);
            }

            // Write each key value
            foreach (var value in keyValues)
            {
                output.Write(value, 0, value.Length);
            }
        }
    }

    public class GameReplay
    {
        public struct Keyframe
        {
            public float Time;
            public ushort ID;
            public byte[] Bytes;
        }

        private int currentFrame;
        private readonly BaseGame game;
        private List<Keyframe> keyframes;
        private string worldFilename;

        /// <summary>
        /// Gets the world filename of this replay.
        /// </summary>
        public string WorldFilename => worldFilename;

        public GameReplay(BaseGame game)
        {
            this.game = game;
        }

        /// <summary>
        /// Loads the game replay from an input stream.
        /// </summary>
        public bool Load(Stream input)
        {
            // Read replay header
            var magic = Encoding.ASCII.GetBytes(GameRecorder.Magic);
            var buffer = new byte[magic.Length];

            var bytesRead = input.Read(buffer, 0, magic.Length);

            if (bytesRead != magic.Length || !Helper.ByteArrayEquals(magic, buffer))
            {
                return false;
            }

            var version = input.ReadByte();

            // Check replay version
            if (version != (int)GameRecorder.Version)
            {
                return false;
            }

            // Read map info
            var filenameLength = input.ReadByte();
            if (filenameLength < 0)
            {
                return false;
            }

            // World filename
            if (buffer.Length < filenameLength)
            {
                buffer = new byte[filenameLength];
            }

            bytesRead = input.Read(buffer, 0, filenameLength);

            if (bytesRead != filenameLength)
            {
                return false;
            }

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
            {
                return false;
            }

            // Read keyframes
            bytesRead = input.Read(buffer, 0, 4);
            if (bytesRead != 4)
            {
                return false;
            }

            var keyframeCount = BitConverter.ToInt32(buffer, 0);
            if (keyframeCount <= 0)
            {
                return false;
            }

            var keyframes = new List<Keyframe>(keyframeCount);
            var offsets = new uint[keyframeCount + 1];
            offsets[keyframeCount] = (uint)input.Length;

            // Read each individual keyframe
            buffer = new byte[GameRecorder.Keyframe.SizeInBytes];
            for (var i = 0; i < keyframeCount; i++)
            {
                bytesRead = input.Read(buffer, 0, buffer.Length);

                if (bytesRead != buffer.Length)
                {
                    return false;
                }

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
            for (var i = 0; i < keyframeCount; i++)
            {
                var length = (int)offsets[i + 1] - (int)offsets[i];

                if (length <= 0)
                {
                    return false;
                }

                var bytes = new byte[length];

                input.Seek((long)offsets[i], SeekOrigin.Begin);

                bytesRead = input.Read(bytes, 0, length);

                if (bytesRead != length)
                {
                    return false;
                }

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
            currentFrame = 0;

            return true;
        }

        public void Update(GameTime gameTime)
        {
            if (keyframes == null)
            {
                return;
            }

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
}