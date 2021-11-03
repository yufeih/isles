// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;
using Isles.Graphics;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    public class GameWorld
    {
        private sealed class InternalList<T> : BroadcastList<T, LinkedList<T>> { }

        public IEnumerable<BaseEntity> WorldObjects => worldObjects;

        private readonly InternalList<BaseEntity> worldObjects = new();

        public BaseGame Game { get; } = BaseGame.Singleton;

        public Terrain Landscape { get; private set; }

        public PathManager PathManager { get; private set; }

        public FogOfWar FogOfWar { get; private set; }

        public Func<Entity> Pick { get; set; }

        public Vector3? TargetPosition => isTargetPositionOnLandscape ? targetPosition : null;

        private Vector3 targetPosition = Vector3.Zero;
        private bool isTargetPositionOnLandscape = true;

        public string Name;
        public string Description;

        public void Update(GameTime gameTime)
        {
            Landscape.Update(gameTime);

            SmoothTargetPosition();

            foreach (var o in worldObjects)
            {
                o.Update(gameTime);
            }

            if (FogOfWar != null)
            {
                FogOfWar.Refresh();
            }

            PathManager.Update();

            UpdateSceneManager();
        }

        private void SmoothTargetPosition()
        {
            Vector3? hitPoint = Landscape.Pick();
            if (isTargetPositionOnLandscape = hitPoint.HasValue)
            {
                targetPosition += (hitPoint.Value - targetPosition) * 0.5f;
                var height = Landscape.GetHeight(targetPosition.X, targetPosition.Y);
                if (height > targetPosition.Z)
                {
                    targetPosition.Z = height;
                }
            }
        }

        public virtual void Load(XmlElement node, ILoading context)
        {
            context.Refresh(2);

            // Load landscape
            var landscapeFilename = node.GetAttribute("Landscape");

            Landscape = new Terrain();
            Landscape.Load(JsonSerializer.Deserialize<TerrainData>(
                File.ReadAllBytes($"data/{landscapeFilename}.json")), BaseGame.Singleton.TextureLoader);
            InitializeGrid();

            // Initialize fog of war
            FogOfWar = new FogOfWar(Game.GraphicsDevice, Landscape.Size.X, Landscape.Size.Y);

            context.Refresh(5);

            // Create a path manager for the landscape
            PathManager = new PathManager(Landscape, ReadOccluders(node));

            // Name & description
            Name = node.GetAttribute("Name");
            Description = node.GetAttribute("Description");

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
            worldObjects.Add(worldObject);

            if (worldObject is Entity entity)
            {
                entity.OnCreate();

                if (entity.IsInteractive)
                {
                    Activate(entity);
                }
            }
        }

        public void Destroy(BaseEntity worldObject)
        {
            if (worldObject == null)
            {
                return;
            }

            // Deactivate the object
            if (worldObject.IsActive)
            {
                Deactivate(worldObject);
            }

            // Remove it from selected and highlighed
            if (worldObject is Entity e)
            {
                e.OnDestroy();
            }

            // Finally, remove it from object list
            worldObjects.Remove(worldObject);
        }

        public void Activate(BaseEntity worldObject)
        {
            if (worldObject == null)
            {
                throw new ArgumentNullException();
            }

            if (worldObject.IsActive)
            {
                return;
            }

            worldObject.IsActive = true;

            if (worldObject.SceneManagerTag is not List<Point> grids)
            {
                grids = new List<Point>();
                worldObject.SceneManagerTag = grids;
            }

            grids.Clear();

            foreach (Point grid in EnumerateGrid(worldObject.BoundingBox))
            {
                System.Diagnostics.Debug.Assert(
                    !Data[grid.X, grid.Y].Owners.Contains(worldObject));

                grids.Add(grid);

                Data[grid.X, grid.Y].Owners.Add(worldObject);
            }

            worldObject.IsDirty = false;
        }

        public void Deactivate(BaseEntity worldObject)
        {
            if (worldObject == null)
            {
                throw new ArgumentNullException();
            }

            if (!worldObject.IsActive)
            {
                return;
            }

            worldObject.IsActive = false;

            if (worldObject.SceneManagerTag is not List<Point> grids)
            {
                throw new InvalidOperationException();
            }

            foreach (Point grid in grids)
            {
                System.Diagnostics.Debug.Assert(
                    Data[grid.X, grid.Y].Owners.Contains(worldObject));

                Data[grid.X, grid.Y].Owners.Remove(worldObject);
            }

            grids.Clear();

            worldObject.IsDirty = false;
        }

        private void UpdateSceneManager()
        {
            // For all active objects, change the grids it owns if its
            // bounding box is dirty, making it up to date.
            foreach (var o in worldObjects)
            {
                if (o.IsActive && o.IsDirty)
                {
                    Deactivate(o);
                    Activate(o);
                }
            }
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

        private readonly List<BaseEntity> set = new(4);

        public IEnumerable<BaseEntity> GetNearbyObjects(Vector3 position, float radius)
        {
            set.Clear();

            // Treat it as a box instead of a sphere...
            foreach (Point grid in EnumerateGrid(position, new Vector3(radius * 2)))
            {
                foreach (var o in Data[grid.X, grid.Y].Owners)
                {
                    if (!set.Contains(o))
                    {
                        set.Add(o);
                    }
                }
            }

            return set;
        }

        public IEnumerable<BaseEntity> GetNearbyObjectsPrecise(Vector3 position, float radius)
        {
            foreach (var o in GetNearbyObjects(position, radius))
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

        /// <summary>
        /// The data to hold on each grid.
        /// </summary>
        public struct Grid
        {
            /// <summary>
            /// Owners of this grid, allow overlapping.
            /// </summary>
            public List<BaseEntity> Owners;
        }

        private Grid[,] Data;

        public int GridCountOnXAxis { get; private set; }

        public int GridCountOnYAxis { get; private set; }

        public float GridSizeOnXAxis { get; private set; }

        public float GridSizeOnYAxis { get; private set; }

        private void InitializeGrid()
        {
            GridCountOnXAxis = Landscape.GridCountOnXAxis;
            GridCountOnYAxis = Landscape.GridCountOnYAxis;

            Data = new Grid[GridCountOnXAxis, GridCountOnYAxis];

            GridSizeOnXAxis = Landscape.Size.X / GridCountOnXAxis;
            GridSizeOnYAxis = Landscape.Size.Y / GridCountOnYAxis;

            // Initialize landscape type
            for (var x = 0; x < GridCountOnXAxis; x++)
            {
                for (var y = 0; y < GridCountOnYAxis; y++)
                {
                    Data[x, y].Owners = new List<BaseEntity>(2);
                }
            }
        }

        public IEnumerable<Point> EnumerateGrid(Vector3 position, Vector3 size)
        {
            Point min = Landscape.PositionToGrid(position.X - size.X / 2, position.Y - size.Y / 2);
            Point max = Landscape.PositionToGrid(position.X + size.X / 2, position.Y + size.Y / 2);

            if (min.X < 0)
            {
                min.X = 0;
            }

            if (min.Y < 0)
            {
                min.Y = 0;
            }

            if (max.X >= GridCountOnXAxis)
            {
                max.X = GridCountOnXAxis - 1;
            }

            if (max.Y >= GridCountOnYAxis)
            {
                max.Y = GridCountOnYAxis - 1;
            }

            for (var y = min.Y; y <= max.Y; y++)
            {
                for (var x = min.X; x <= max.X; x++)
                {
                    yield return new Point(x, y);
                }
            }
        }

        public IEnumerable<Point> EnumerateGrid(BoundingBox boundingBox)
        {
            Point min = Landscape.PositionToGrid(boundingBox.Min.X, boundingBox.Min.Y);
            Point max = Landscape.PositionToGrid(boundingBox.Max.X, boundingBox.Max.Y);

            if (min.X < 0)
            {
                min.X = 0;
            }

            if (min.Y < 0)
            {
                min.Y = 0;
            }

            if (max.X >= GridCountOnXAxis)
            {
                max.X = GridCountOnXAxis - 1;
            }

            if (max.Y >= GridCountOnYAxis)
            {
                max.Y = GridCountOnYAxis - 1;
            }

            for (var y = min.Y; y <= max.Y; y++)
            {
                for (var x = min.X; x <= max.X; x++)
                {
                    yield return new Point(x, y);
                }
            }
        }
    }
}
