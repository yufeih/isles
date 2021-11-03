// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
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

        public IEnumerable<IWorldObject> WorldObjects => worldObjects;

        private readonly InternalList<IWorldObject> worldObjects = new();

        public BaseGame Game { get; } = BaseGame.Singleton;

        public Terrain Landscape { get; private set; }

        public PathManager PathManager { get; private set; }

        public FogOfWar FogOfWar { get; private set; }

        public Vector3? TargetPosition => isTargetPositionOnLandscape ? targetPosition : null;

        private Vector3 targetPosition = Vector3.Zero;
        private bool isTargetPositionOnLandscape = true;

        private readonly ModelPicker<Entity> _modelPicker;

        public string Name;
        public string Description;

        public GameWorld()
        {
            _modelPicker = new ModelPicker<Entity>(Game.GraphicsDevice, Game.ModelRenderer);
        }

        public void Update(GameTime gameTime)
        {
            Landscape.Update(gameTime);

            SmoothTargetPosition();

            foreach (IWorldObject o in worldObjects)
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

        public void Draw(GameTime gameTime)
        {
            var objectMap = _modelPicker.DrawObjectMap(
                Game.ViewProjection, worldObjects.OfType<Entity>().Where(entity => entity.IsPickable), entity => entity.Model);

            pickedEntity = objectMap.Pick();

            if (Game.Settings.ReflectionEnabled)
            {
                Landscape.UpdateWaterReflectionAndRefraction(gameTime);
            }

            // Generate shadow map
            if (Game.Shadow != null)
            {
                Game.Shadow.Begin(Game.Eye, Game.Facing);

                Game.ModelRenderer.Clear();

                // Draw shadow casters. Currently we only draw all world object
                foreach (var o in worldObjects)
                {
                    o.DrawShadowMap(gameTime, Game.Shadow);
                }

                Game.ModelRenderer.DrawShadowMap(Game.Shadow);

                // Resolve shadow map
                Game.Shadow.End();
            }

            Game.ModelRenderer.Clear();

            // Draw world objects before landscape
            foreach (IWorldObject o in worldObjects)
            {
                o.Draw(gameTime);
            }

            // Draw spell
            Spell.CurrentSpell?.Draw(gameTime);

            Landscape.DrawWater(gameTime);

            // Draw shadow receivers with the shadow map
            Landscape.DrawTerrain(Game.Shadow);

            // Present surface
            Landscape.PresentSurface();

            // FIXME: There are some weired things when models are drawed after
            // drawing the terrain... Annoying...
            Game.ModelRenderer.Draw(Game.ViewProjection, true, false);

            // TODO: Draw particles with ZEnable = true, ZWriteEnable = false
            ParticleSystem.Present();

            Game.ModelRenderer.Draw(Game.ViewProjection, false, true);
        }

        private void DrawWaterReflection(GameTime gameTime, Matrix view, Matrix projection)
        {
            foreach (IWorldObject o in worldObjects)
            {
                o.DrawReflection(gameTime, view, projection);
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
            Landscape.DrawWaterReflection += DrawWaterReflection;
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
            IWorldObject worldObject;
            foreach (XmlNode child in node.ChildNodes)
            {
                // Ignore comments and other stuff...
                if (child is XmlElement element)
                {
                    if ((worldObject = Create(child.Name, element)) != null)
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

        private static Dictionary<string, Func<GameWorld, IWorldObject>> Creators = new();

        public static void RegisterCreator(string typeName, Func<GameWorld, IWorldObject> creator)
        {
            Creators.Add(typeName, creator);
        }

        public IWorldObject Create(string typeName)
        {
            return Create(typeName, null);
        }

        private IWorldObject Create(string typeName, XmlElement xml)
        {
            // Lookup the creators table to find a suitable creator
            if (!Creators.ContainsKey(typeName))
            {
                throw new Exception("Unknown object type: " + typeName);
            }

            IWorldObject worldObject = Creators[typeName](this);

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

        private Entity pickedEntity;
        
        public Entity Pick()
        {
            return pickedEntity;
        }

        public void Add(IWorldObject worldObject)
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

        public void Destroy(IWorldObject worldObject)
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

        public void Activate(IWorldObject worldObject)
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

        public void Deactivate(IWorldObject worldObject)
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
            foreach (IWorldObject o in worldObjects)
            {
                if (o.IsActive && o.IsDirty)
                {
                    Deactivate(o);
                    Activate(o);
                }
            }
        }

        public IEnumerable<IWorldObject> ObjectsFromRegion(BoundingFrustum boundingFrustum)
        {
            // This is a really slow method
            foreach (IWorldObject o in worldObjects)
            {
                if (o is Entity e && e.Intersects(boundingFrustum))
                {
                    yield return e;
                }
            }
        }

        private readonly List<IWorldObject> set = new(4);

        public IEnumerable<IWorldObject> GetNearbyObjects(Vector3 position, float radius)
        {
            set.Clear();

            // Treat it as a box instead of a sphere...
            foreach (Point grid in EnumerateGrid(position, new Vector3(radius * 2)))
            {
                foreach (IWorldObject o in Data[grid.X, grid.Y].Owners)
                {
                    if (!set.Contains(o))
                    {
                        set.Add(o);
                    }
                }
            }

            return set;
        }

        public IEnumerable<IWorldObject> GetNearbyObjectsPrecise(Vector3 position, float radius)
        {
            foreach (IWorldObject o in GetNearbyObjects(position, radius))
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
            public List<IWorldObject> Owners;
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
                    Data[x, y].Owners = new List<IWorldObject>(2);
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
