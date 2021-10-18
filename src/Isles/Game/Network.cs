// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Isles.Engine;
using Microsoft.Xna.Framework;

namespace Isles
{
    public class GameServer
    {
        public static GameServer Singleton => singleton;

        private static GameServer singleton;

        private readonly Dictionary<ushort, IGameObject> idToObject = new();
        private readonly Dictionary<IGameObject, ushort> objectToID = new();

        /// <summary>
        /// Make sure every game object has a unique ID.
        /// </summary>
        private ushort currentValidID = MinID;
        private const ushort MinID = 128;

        /// <summary>
        /// Record all game object changes.
        /// </summary>
        private readonly GameRecorder recorder;
        private readonly GameWorld world;

        /// <summary>
        /// Gets or sets game world.
        /// </summary>
        public GameWorld World => world;

        private double time;

        /// <summary>
        /// Gets game elapsed time since server started.
        /// </summary>
        public double Time => time;

        /// <summary>
        /// Gets the next valid ID from this game server.
        /// </summary>
        public ushort NextValidID => currentValidID == ushort.MaxValue ? throw new InvalidOperationException() : currentValidID++;

        /// <summary>
        /// Creates a new game server.
        /// </summary>
        public GameServer(GameWorld world, GameRecorder recorder)
        {
            singleton = this;

            this.world = world ?? throw new ArgumentNullException();
            this.recorder = recorder;
        }

        /// <summary>
        /// Gets a game object from the specified id.
        /// </summary>
        public IGameObject ObjectFromID(ushort id)
        {
            return idToObject.TryGetValue(id, out IGameObject value) ? value : null;
        }

        /// <summary>
        /// Gets the id of a game object.
        /// </summary>
        public ushort IDFromObject(IGameObject o)
        {
            return objectToID.TryGetValue(o, out var id) ? id : (ushort)0;
        }

        /// <summary>
        /// Creates a new game object in the game server.
        /// </summary>
        public IWorldObject Create(string type)
        {
            return Create(type, NextValidID);
        }

        /// <summary>
        /// Adds a new game object to the game server with the specified id.
        /// </summary>
        public virtual IWorldObject Create(string type, ushort id)
        {
            IWorldObject o = world.Create(type);

            if (o is IGameObject)
            {
                var newObject = o as IGameObject;

                if (idToObject.ContainsKey(id))
                {
                    throw new ArgumentException("ID already exists");
                }

                if (objectToID.ContainsKey(newObject))
                {
                    throw new ArgumentException("Object already added");
                }

                idToObject.Add(id, newObject);
                objectToID.Add(newObject, id);

                // Dispatch create object event
                var index = GameWorld.CreatorIndexFromType(type);

                if (index < 0)
                {
                    throw new ArgumentException("Unrecognized type: " + type);
                }

                if (index >= ushort.MaxValue)
                {
                    throw new Exception();
                }

                var bytes = new byte[4];
                var buffer = BitConverter.GetBytes((ushort)index);
                bytes[0] = buffer[0];
                bytes[1] = buffer[1];
                buffer = BitConverter.GetBytes((ushort)id);
                bytes[2] = buffer[0];
                bytes[3] = buffer[1];

                Dispatch(0, bytes, 0, 4);
            }

            return o;
        }

        /// <summary>
        /// Destroy an existing game object from the server.
        /// </summary>
        /// <returns>
        /// Whether the server contains the existing object.
        /// </returns>
        public virtual void Destroy(IWorldObject o)
        {
            world.Destroy(o);

            if (o is IGameObject)
            {
                var existingObject = o as IGameObject;

                if (objectToID.ContainsKey(existingObject))
                {
                    // Dispatch destroy object event
                    var id = IDFromObject(existingObject);

                    if (id < MinID)
                    {
                        throw new ArgumentException("The input object has an invalid ID.");
                    }

                    var bytes = BitConverter.GetBytes((ushort)id);
                    Dispatch(1, bytes, 0, bytes.Length);

                    // Remove the object from registry
                    objectToID.Remove(existingObject);
                    idToObject.Remove(IDFromObject(existingObject));
                }
            }
        }

        /// <summary>
        /// Dispatch the input to the replay recorder & game clients.
        /// </summary>
        public void Dispatch(ushort id, byte[] bytes, int offset, int length)
        {
            if (id < 0 || offset < 0 || length < 0 || id >= byte.MaxValue || bytes == null)
            {
                throw new ArgumentException();
            }

            if (length > 0)
            {
                if (recorder != null)
                {
                    recorder.Record(id, (float)time, bytes, offset, length);
                }
            }
        }

        /// <summary>
        /// Execute the received package.
        /// </summary>
        public virtual void Execute(ushort id, byte[] bytes, int offset, int length)
        {
            IGameObject o;

            // Special commands: Create [0], Destroy [1]
            if (id == 0 && length == 4)
            {
                var type = BitConverter.ToUInt16(bytes, offset);
                var oID = BitConverter.ToUInt16(bytes, offset + 2);

                Create(GameWorld.CreatorTypeFromIndex((int)type), oID);
            }
            else if (id == 1 && length == 2)
            {
                var oID = BitConverter.ToUInt16(bytes, offset);
                o = ObjectFromID(oID);
                if (o != null && o is IWorldObject)
                {
                    Destroy(o as IWorldObject);
                }
            }
            else
            {
                o = ObjectFromID(id);

                if (o != null)
                {
                    o.Deserialize(new MemoryStream(bytes, offset, length));
                }
            }
        }

        private readonly MemoryStream stream = new();

        public void Update(GameTime gameTime)
        {
            // Update time
            time += gameTime.ElapsedGameTime.TotalSeconds;

            // Serialize any game object changes
            foreach (KeyValuePair<ushort, IGameObject> pair in idToObject)
            {
                System.Diagnostics.Debug.Assert(pair.Key >= MinID);

                if (pair.Value != null)
                {
                    // Clear the memory stream
                    stream.Seek(0, SeekOrigin.Begin);

                    // Serialize into our memory stream
                    pair.Value.Serialize(stream);

                    // Dispatch the serialized data
                    Dispatch(pair.Key, stream.GetBuffer(), 0, (int)stream.Position);
                }
            }
        }
    }
}