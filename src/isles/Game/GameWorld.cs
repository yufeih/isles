// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml;
using Isles.Graphics;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    public class GameWorld
    {
        private readonly List<BaseEntity> worldObjects = new();
        private readonly List<BaseEntity> addedWorldObjects = new();
        private readonly List<BaseEntity> removedWorldObjects = new();

        public IEnumerable<BaseEntity> WorldObjects => worldObjects;

        public BaseGame Game { get; } = BaseGame.Singleton;

        public Terrain Landscape { get; private set; }

        public PathManager PathManager { get; private set; }

        public FogOfWar FogOfWar { get; private set; }

        public Func<Entity> Pick { get; set; }

        public void Update(GameTime gameTime)
        {
            FlushAddedOrRemovedEntities();

            Landscape.Update(gameTime);

            foreach (var o in worldObjects)
            {
                o.Update(gameTime);
            }

            if (FogOfWar != null)
            {
                FogOfWar.Refresh();
            }

            PathManager.Update();
        }

        public virtual void Load(XmlElement node, ILoading context)
        {
            context.Refresh(2);

            // Load landscape
            var landscapeFilename = node.GetAttribute("Landscape");

            Landscape = new Terrain();
            Landscape.Load(JsonSerializer.Deserialize<TerrainData>(
                File.ReadAllBytes($"data/{landscapeFilename}.json")), BaseGame.Singleton.TextureLoader);

            // Initialize fog of war
            FogOfWar = new FogOfWar(Game.GraphicsDevice, Landscape.Size.X, Landscape.Size.Y);

            context.Refresh(5);

            // Create a path manager for the landscape
            PathManager = new PathManager(Landscape, ReadOccluders(node));

            context.Refresh(10);

            // Load world objects
            var nObjects = 0;
            foreach (XmlNode child in node.ChildNodes)
            {
                // Ignore comments and other stuff...
                if (child is XmlElement element)
                {
                    if (Create(child.Name, element) is var worldObject)
                    {
                        Add(worldObject);
                        nObjects++;
                    }
                }

                context.Refresh(10 + 100 * nObjects / node.ChildNodes.Count);
            }

            FlushAddedOrRemovedEntities();
        }

        private static List<Point> ReadOccluders(XmlElement node)
        {
            List<Point> occluders = null;

            if (node.SelectSingleNode("PathOccluder") is XmlElement occluderNode && occluderNode.HasAttribute("Value"))
            {
                var p = new Point();
                var first = true;
                occluders = new List<Point>();
                var value = occluderNode.GetAttribute("Value");
                var atoms = value.Split(new char[] { ' ', '\n', '\t', '\r' });

                foreach (var atom in atoms)
                {
                    if (atom.Length > 0)
                    {
                        if (first)
                        {
                            p.X = int.Parse(atom);
                            first = false;
                        }
                        else
                        {
                            first = true;
                            p.Y = int.Parse(atom);
                            occluders.Add(p);
                        }
                    }
                }

                node.RemoveChild(occluderNode);
            }

            return occluders;
        }

        private static Dictionary<string, Func<GameWorld, BaseEntity>> Creators = new();

        public static void RegisterCreator(string typeName, Func<GameWorld, BaseEntity> creator)
        {
            Creators.Add(typeName, creator);
        }

        public BaseEntity Create(string typeName)
        {
            return Create(typeName, null);
        }

        private BaseEntity Create(string typeName, XmlElement xml)
        {
            // Lookup the creators table to find a suitable creator
            if (!Creators.ContainsKey(typeName))
            {
                throw new Exception("Unknown object type: " + typeName);
            }

            BaseEntity worldObject = Creators[typeName](this);

            // Nothing created
            if (worldObject == null)
            {
                return null;
            }

            // Set object class ID
            worldObject.ClassID = typeName;

            // Deserialize world object
            if (xml != null)
            {
                worldObject.Deserialize(xml);
            }

            return worldObject;
        }

        public void Add(BaseEntity worldObject)
        {
            addedWorldObjects.Add(worldObject);
        }

        public void Destroy(BaseEntity worldObject)
        {
            if (worldObject == null)
            {
                return;
            }

            removedWorldObjects.Add(worldObject);
        }

        private void FlushAddedOrRemovedEntities()
        {
            foreach (var o in addedWorldObjects)
            {
                worldObjects.Add(o);
                if (o is Entity e)
                {
                    e.OnCreate();
                }
            }
            addedWorldObjects.Clear();

            foreach (var o in removedWorldObjects)
            {
                if (o is Entity e)
                {
                    e.OnDestroy();
                }
                worldObjects.Remove(o);
            }
            removedWorldObjects.Clear();
        }

        public IEnumerable<BaseEntity> ObjectsFromRegion(BoundingFrustum boundingFrustum)
        {
            // This is a really slow method
            foreach (var o in worldObjects)
            {
                if (o is Entity e && e.Intersects(boundingFrustum))
                {
                    yield return e;
                }
            }
        }

        public IEnumerable<BaseEntity> GetNearbyObjects(Vector3 position, float radius)
        {
            foreach (var o in worldObjects)
            {
                Vector2 v;

                v.X = o.Position.X - position.X;
                v.Y = o.Position.Y - position.Y;

                if (v.LengthSquared() <= radius * radius)
                {
                    yield return o;
                }
            }
        }
    }
}
