// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Isles.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Isles.Engine
{
    /// <summary>
    /// Represents the game world.
    /// </summary>
    public class GameWorld : ISceneManager
    {
        /// <summary>
        /// Version of the game world.
        /// </summary>
        public const int Version = 1;

        private sealed class InternalList<T> : BroadcastList<T, LinkedList<T>> { }

        /// <summary>
        /// Enumerates all world objects.
        /// </summary>
        public IEnumerable<IWorldObject> WorldObjects => worldObjects;

        private readonly InternalList<IWorldObject> worldObjects = new();

        /// <summary>
        /// Gets main game instance.
        /// </summary>
        public BaseGame Game { get; } = BaseGame.Singleton;

        /// <summary>
        /// Landscape of the game world.
        /// </summary>
        public Landscape Landscape { get; private set; }

        private string landscapeFilename;

        /// <summary>
        /// Gets the path manager of the game world.
        /// </summary>
        public PathManager PathManager { get; private set; }

        /// <summary>
        /// Game content manager.
        /// Assets loaded using this content manager will not be unloaded
        /// until the termination of the application.
        /// </summary>
        public ContentManager Content { get; }

        /// <summary>
        /// Gets game the fog of war.
        /// </summary>
        public FogMask FogOfWar { get; private set; }

        /// <summary>
        /// Gets or sets whether fog of war is enable.
        /// </summary>
        public bool EnableFogOfWar { get; } = true;

        /// <summary>
        /// Gets the smoothed target position.
        /// </summary>
        public Vector3? TargetPosition => isTargetPositionOnLandscape ? (Vector3?)targetPosition : null;

        private Vector3 targetPosition = Vector3.Zero;
        private bool isTargetPositionOnLandscape = true;

        private readonly ModelPicker<Entity> _modelPicker;

        public string Name;
        public string Description;

        public GameWorld()
        {
            Content = Game.Content;
            _modelPicker = new ModelPicker<Entity>(Game.GraphicsDevice, Game.ModelRenderer);
        }

        /// <summary>
        /// Update the game world and all the world objects.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Update landscape
            Landscape.Update(gameTime);

            // Smooth target position
            SmoothTargetPosition();

            // Update each object
            foreach (IWorldObject o in worldObjects)
            {
                o.Update(gameTime);
            }

            // Refresh fog of war
            if (EnableFogOfWar && FogOfWar != null)
            {
                FogOfWar.Refresh();
            }

            // Update path manager
            PathManager.Update();

            // Update scene manager internal structure
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
                // Pre-render our landscape, update landscape
                // reflection and refraction texture.
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

            Landscape.PresentSurface();
        }

        /// <summary>
        /// Draw each world object.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        private void DrawWaterReflection(GameTime gameTime, Matrix view, Matrix projection)
        {
            foreach (IWorldObject o in worldObjects)
            {
                o.DrawReflection(gameTime, view, projection);
            }
        }

        /// <summary>
        /// Load the game world from a file.
        /// </summary>
        /// <param name="inStream"></param>
        public virtual void Load(XmlElement node, ILoading context)
        {
            // Validate XML element
            if (node.Name != "World")
            {
                throw new Exception("Invalid world format.");
            }

            context.Refresh(2);

            // Load landscape
            landscapeFilename = node.GetAttribute("Landscape");
            if (landscapeFilename == "")
            {
                throw new Exception("World does not have a landscape");
            }

            Landscape = Content.Load<Landscape>(landscapeFilename);
            Landscape.DrawWaterReflection += new Landscape.DrawDelegate(DrawWaterReflection);
            InitializeGrid();

            // Initialize fog of war
            FogOfWar = new FogMask(Game.GraphicsDevice, Landscape.Size.X, Landscape.Size.Y);

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

                context.Refresh(10 + (int)(100 * nObjects / node.ChildNodes.Count));
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

        public static Dictionary<string, Func<GameWorld, IWorldObject>> Creators = new();

        public static void RegisterCreator(string typeName, Func<GameWorld, IWorldObject> creator)
        {
            Creators.Add(typeName, creator);
        }

        public IWorldObject Create(string typeName)
        {
            return Create(typeName, null);
        }

        /// <summary>
        /// Creates a new world object of a given type.
        /// Call Add explicitly to make the object shown in the world.
        /// </summary>
        /// <param name="typeName">Type of the object.</param>
        /// <param name="xml">A xml element describes the object.</param>
        public IWorldObject Create(string typeName, XmlElement xml)
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

        /// <summary>
        /// Entity picked this frame.
        /// </summary>
        private Entity pickedEntity;

        /// <summary>
        /// Pick an entity from the cursor.
        /// </summary>
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

        public void Clear()
        {
            worldObjects.Clear();
        }

        /// <summary>
        /// Activate a world object.
        /// </summary>
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

        /// <summary>
        /// Deactivate a world object.
        /// </summary>
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

        /// <summary>
        /// Test to see if a point intersects a world object.
        /// </summary>
        public bool PointSceneIntersects(Vector3 point)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test to see if a ray intersects a world object.
        /// </summary>
        public bool RaySceneIntersects(Ray ray)
        {
            throw new NotImplementedException();
        }

        public bool ObjectIntersects(IWorldObject object1, IWorldObject object2)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IWorldObject> ObjectsFromPoint(Vector3 point)
        {
            Point grid = Landscape.PositionToGrid(point.X, point.Y);

            foreach (IWorldObject o in Data[grid.X, grid.Y].Owners)
            {
                if (o is Entity e && e.Intersects(point))
                {
                    yield return e;
                }
            }
        }

        public IEnumerable<IWorldObject> ObjectsFromRay(Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<IWorldObject> ObjectsFromRegion(BoundingBox boundingBox)
        {
            Point min = Landscape.PositionToGrid(boundingBox.Min.X, boundingBox.Min.Y);
            Point max = Landscape.PositionToGrid(boundingBox.Max.X, boundingBox.Max.Y);

            System.Diagnostics.Debug.Assert(min.X <= max.X);
            System.Diagnostics.Debug.Assert(min.Y <= max.Y);

            for (var x = min.X; x < max.X; x++)
            {
                for (var y = min.Y; y < max.Y; y++)
                {
                    foreach (IWorldObject o in Data[x, y].Owners)
                    {
                        yield return o;
                    }
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

        /// <summary>
        /// Get grid data.
        /// </summary>
        public Grid[,] Data;

        /// <summary>
        /// Gets the width of grid.
        /// </summary>
        public int GridCountOnXAxis { get; private set; }

        /// <summary>
        /// Gets the height of grid.
        /// </summary>
        public int GridCountOnYAxis { get; private set; }

        /// <summary>
        /// Gets the size.X of grid.
        /// </summary>
        public float GridSizeOnXAxis { get; private set; }

        /// <summary>
        /// Gets the height of grid.
        /// </summary>
        public float GridSizeOnYAxis { get; private set; }

        public Vector2 GridSize => new(GridSizeOnXAxis, GridSizeOnYAxis);

        public Point GridCount => new(GridCountOnXAxis, GridCountOnYAxis);

        /// <summary>
        /// Checks if a grid is within the boundery of the terrain.
        /// </summary>
        /// <param name="grid"></param>
        public bool IsValidGrid(Point grid)
        {
            return grid.X >= 0 && grid.X < GridCountOnXAxis &&
                   grid.Y >= 0 && grid.Y < GridCountOnYAxis;
        }

        /// <summary>
        /// Initialize all grid data.
        /// </summary>
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

        /// <summary>
        /// Enumerate all the grid points that falls inside a
        /// rectangle on the XY plane.
        /// </summary>
        private class GridEnumerator : IEnumerable<Point>
        {
            private Matrix inverse;
            private Point pMin;
            private Point pMax;
            private Vector2 vMin;
            private Vector2 vMax;
            private Vector2 min, max;
            private readonly Landscape landscape;

            /// <summary>
            /// Create the rectangle from 2 points and a transform matrix.
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate.</param>
            /// <param name="min">The minimum point of the bounding box, Z value is ignored.</param>
            /// <param name="max">The maximum point of the bounding box, Z value is ignored.</param>
            public GridEnumerator(Landscape landscape, Vector3 min, Vector3 max, Vector3 position, float rotationZ)
                : this(landscape, new Vector2(min.X, min.Y), new Vector2(max.X, max.Y),
                Matrix.CreateRotationZ(rotationZ) * Matrix.CreateTranslation(position))
            {
            }

            /// <summary>
            /// Create the rectangle from 2 points and a transform matrix.
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate.</param>
            /// <param name="min">The minimum point of the bounding box, Z value is ignored.</param>
            /// <param name="max">The maximum point of the bounding box, Z value is ignored.</param>
            /// <param name="transform">The matrix used to transform the bounding box.</param>
            public GridEnumerator(Landscape landscape, Vector3 min, Vector3 max, Matrix transform)
                : this(landscape, new Vector2(min.X, min.Y), new Vector2(max.X, max.Y), transform)
            {
            }

            /// <summary>
            /// Create the rectangle from size, position and rotation.
            /// </summary>
            /// <param name="landscape"></param>
            /// <param name="size"></param>
            /// <param name="position"></param>
            /// <param name="rotationZ"></param>
            public GridEnumerator(Landscape landscape, Vector2 size, Vector3 position, float rotationZ)
                : this(landscape, new Vector2(-size.X / 2, -size.Y / 2), new Vector2(size.X / 2, size.Y / 2),
                Matrix.CreateRotationZ(rotationZ) * Matrix.CreateTranslation(position))
            {
            }

            /// <summary>
            /// Create the rectangle from 2 points and a transform matrix.
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate.</param>
            /// <param name="min">The minimum point of the bounding box.</param>
            /// <param name="max">The maximum point of the bounding box.</param>
            /// <param name="transform">The matrix used to transform the bounding box.</param>
            public GridEnumerator(Landscape landscape, Vector2 min, Vector2 max, Matrix transform)
            {
                this.min = min;
                this.max = max;
                this.landscape = landscape;

                // This is not an fast algorithm, but at least it works :)
                //
                // 1. Project the rectangle to XY plane and get its
                //    Axis Aligned Bouding Box.
                //
                // 2. For each grid point in the AABB, transform it
                //    to object space, test it with the rectangle
                var points = new Vector2[4];

                points[0] = new Vector2(min.X, min.Y);
                points[1] = new Vector2(max.X, min.Y);
                points[2] = new Vector2(min.X, max.Y);
                points[3] = new Vector2(max.X, max.Y);

                vMin = new Vector2(10000, 10000);
                vMax = new Vector2(-10000, -10000);

                for (var i = 0; i < 4; i++)
                {
                    points[i] = Vector2.Transform(points[i], transform);

                    if (points[i].X < vMin.X)
                    {
                        vMin.X = points[i].X;
                    }

                    if (points[i].X > vMax.X)
                    {
                        vMax.X = points[i].X;
                    }

                    if (points[i].Y < vMin.Y)
                    {
                        vMin.Y = points[i].Y;
                    }

                    if (points[i].Y > vMax.Y)
                    {
                        vMax.Y = points[i].Y;
                    }
                }

                pMin = landscape.PositionToGrid(vMin.X, vMin.Y);
                pMax = landscape.PositionToGrid(vMax.X, vMax.Y);

                // Restrict to map border
                pMin.X = pMin.X < 0 ? 0 : pMin.X;
                pMin.Y = pMin.Y < 0 ? 0 : pMin.Y;
                pMax.X = pMax.X > landscape.GridCount.X ? landscape.GridCount.X : pMax.X;
                pMax.Y = pMax.Y > landscape.GridCount.Y ? landscape.GridCount.Y : pMax.Y;

                // Make sure max is greater than min
                pMax.X = pMax.X < pMin.X ? pMin.X : pMax.X;
                pMax.Y = pMax.Y < pMin.Y ? pMin.Y : pMax.Y;

                // Compute world inverse to transform from world space to object space
                inverse = Matrix.Invert(transform);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            public IEnumerator<Point> GetEnumerator()
            {
                var grids = new bool[
                    pMax.X - pMin.X + 1,
                    pMax.Y - pMin.Y + 1];

                Vector2 v;
                for (var x = pMin.X; x <= pMax.X; x++)
                {
                    for (var y = pMin.Y; y <= pMax.Y; y++)
                    {
                        v = landscape.GridToPosition(x, y);
                        v = Vector2.Transform(v, inverse);

                        // We can finally test in the object space
                        if (v.X >= min.X && v.X <= max.X &&
                            v.Y >= min.Y && v.Y <= max.Y)
                        {
                            grids[x - pMin.X, y - pMin.Y] = true;
                            yield return new Point(x, y);

                            if (x != pMin.X && !grids[x - pMin.X - 1, y - pMin.Y])
                            {
                                grids[x - pMin.X - 1, y - pMin.Y] = true;
                                yield return new Point(x - 1, y);
                            }

                            if (y != pMin.Y && !grids[x - pMin.X, y - pMin.Y - 1])
                            {
                                grids[x - pMin.X, y - pMin.Y - 1] = true;
                                yield return new Point(x, y - 1);
                            }

                            if (x != pMin.X && y != pMin.Y && !grids[x - pMin.X - 1, y - pMin.Y - 1])
                            {
                                grids[x - pMin.X - 1, y - pMin.Y - 1] = true;
                                yield return new Point(x - 1, y - 1);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
