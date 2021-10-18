//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Isles.Engine;

namespace Isles
{
    #region GameServer
    public class GameServer
    {
        #region Singleton
        public static GameServer Singleton
        {
            get { return singleton; }
        }

        static GameServer singleton;
        #endregion

        Dictionary<ushort, IGameObject> idToObject = new Dictionary<ushort, IGameObject>();
        Dictionary<IGameObject, ushort> objectToID = new Dictionary<IGameObject, ushort>();
        
        /// <summary>
        /// Make sure every game object has a unique ID
        /// </summary>
        UInt16 currentValidID = MinID;
        const UInt16 MinID = 128;

        /// <summary>
        /// Record all game object changes
        /// </summary>
        GameRecorder recorder;

        GameWorld world;

        /// <summary>
        /// Gets or sets game world
        /// </summary>
        public GameWorld World
        {
            get { return world; }
        }

        double time;

        /// <summary>
        /// Gets game elapsed time since server started
        /// </summary>
        public double Time
        {
            get { return time; }
        }

        /// <summary>
        /// Gets the next valid ID from this game server
        /// </summary>
        public ushort NextValidID
        {
            get
            {
                if (currentValidID == ushort.MaxValue)
                    throw new InvalidOperationException();

                return currentValidID++;
            }
        }

        /// <summary>
        /// Creates a new game server
        /// </summary>
        public GameServer(GameWorld world, GameRecorder recorder)
        {
            if (world == null)
                throw new ArgumentNullException();

            singleton = this;

            this.world = world;
            this.recorder = recorder;
        }

        /// <summary>
        /// Gets a game object from the specified id
        /// </summary>
        public IGameObject ObjectFromID(ushort id)
        {
            IGameObject value;

            if (idToObject.TryGetValue(id, out value))
                return value;

            return null;
        }

        /// <summary>
        /// Gets the id of a game object
        /// </summary>
        public ushort IDFromObject(IGameObject o)
        {
            ushort id;

            if (objectToID.TryGetValue(o, out id))
                return id;

            return 0;
        }

        /// <summary>
        /// Creates a new game object in the game server.
        /// </summary>
        public IWorldObject Create(string type)
        {
            return Create(type, NextValidID);
        }

        /// <summary>
        /// Adds a new game object to the game server with the specified id
        /// </summary>
        public virtual IWorldObject Create(string type, ushort id)
        {
            IWorldObject o = world.Create(type);

            if (o is IGameObject)
            {
                IGameObject newObject = o as IGameObject;

                if (idToObject.ContainsKey(id))
                    throw new ArgumentException("ID already exists");

                if (objectToID.ContainsKey(newObject))
                    throw new ArgumentException("Object already added");

                idToObject.Add(id, newObject);
                objectToID.Add(newObject, id);

                // Dispatch create object event
                int index = GameWorld.CreatorIndexFromType(type);

                if (index < 0)
                    throw new ArgumentException("Unrecognized type: " + type);

                if (index >= ushort.MaxValue)
                    throw new Exception();

                byte[] bytes = new byte[4];
                byte[] buffer = BitConverter.GetBytes((ushort)index);
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
        /// Destroy an existing game object from the server
        /// </summary>
        /// <returns>
        /// Whether the server contains the existing object
        /// </returns>
        public virtual void Destroy(IWorldObject o)
        {
            world.Destroy(o);

            if (o is IGameObject)
            {
                IGameObject existingObject = o as IGameObject;

                if (objectToID.ContainsKey(existingObject))
                {
                    // Dispatch destroy object event
                    ushort id = IDFromObject(existingObject);

                    if (id < MinID)
                        throw new ArgumentException("The input object has an invalid ID.");

                    byte[] bytes = BitConverter.GetBytes((ushort)id);
                    Dispatch(1, bytes, 0, bytes.Length);

                    // Remove the object from registry
                    objectToID.Remove(existingObject);
                    idToObject.Remove(IDFromObject(existingObject));
                }
            }
        }

        /// <summary>
        /// Dispatch the input to the replay recorder & game clients
        /// </summary>
        public void Dispatch(ushort id, byte[] bytes, int offset, int length)
        {
            if (id < 0 || offset < 0 || length < 0 || id >= byte.MaxValue || bytes == null)
                throw new ArgumentException();

            if (length > 0)
            {
                if (recorder != null)
                    recorder.Record(id, (float)time, bytes, offset, length);
            }
        }

        /// <summary>
        /// Execute the received package
        /// </summary>
        public virtual void Execute(ushort id, byte[] bytes, int offset, int length)
        {
            IGameObject o;

            // Special commands: Create [0], Destroy [1]
            if (id == 0 && length == 4)
            {
                ushort type = BitConverter.ToUInt16(bytes, offset);
                ushort oID = BitConverter.ToUInt16(bytes, offset + 2);

                Create(GameWorld.CreatorTypeFromIndex((int)type), oID);
            }
            else if (id == 1 && length == 2)
            {
                ushort oID = BitConverter.ToUInt16(bytes, offset);
                o = ObjectFromID(oID);
                if (o != null && o is IWorldObject)
                    Destroy(o as IWorldObject);
            }
            else
            {
                o = ObjectFromID(id);

                if (o != null)
                    o.Deserialize(new MemoryStream(bytes, offset, length));
            }
        }

        MemoryStream stream = new MemoryStream();

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
    #endregion
}