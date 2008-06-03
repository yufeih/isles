//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;
using Isles.Engine;

namespace Isles.Engine
{
    /// <summary>
    /// Represents the game world
    /// </summary>
    public class GameWorld : ISceneManager
    {
        #region Field
        /// <summary>
        /// Version of the game world
        /// </summary>
        public const int Version = 1;

        sealed class InternalList<T> : BroadcastList<T, LinkedList<T>> {}

        /// <summary>
        /// Enumerates all world objects
        /// </summary>
        public IEnumerable<IWorldObject> WorldObjects
        {
            get { return worldObjects; }
        }

        InternalList<IWorldObject> worldObjects = new InternalList<IWorldObject>();
        Dictionary<string, IWorldObject> nameToWorldObject = new Dictionary<string, IWorldObject>();

        /// <summary>
        /// Gets main game instance
        /// </summary>
        public BaseGame Game
        {
            get { return game; }
        }

        BaseGame game = BaseGame.Singleton;


        /// <summary>
        /// Landscape of the game world
        /// </summary>
        public Landscape Landscape
        {
            get { return landscape; }
        }

        Landscape landscape;
        string landscapeFilename;


        /// <summary>
        /// Gets the path manager of the game world
        /// </summary>
        public PathManager PathManager
        {
            get { return pathManager; }
        }

        PathManager pathManager;


        /// <summary>
        /// Game content manager.
        /// Assets loaded using this content manager will not be unloaded
        /// until the termination of the application.
        /// </summary>
        public ContentManager Content
        {
            get { return content; }
        }

        ContentManager content;


        /// <summary>
        /// Gets game the fog of war
        /// </summary>
        public FogMask FogOfWar
        {
            get { return fogOfWar; }
        }

        FogMask fogOfWar;


        /// <summary>
        /// Gets or sets whether fog of war is enable
        /// </summary>
        public bool EnableFogOfWar
        {
            get { return enableFogOfWar; }
        }

        bool enableFogOfWar = true; 


        /// <summary>
        /// Gets the smoothed target position
        /// </summary>
        public Vector3? TargetPosition
        {
            get
            {
                if (isTargetPositionOnLandscape)
                    return targetPosition;
                return null;
            }
        }

        Vector3 targetPosition = Vector3.Zero;
        bool isTargetPositionOnLandscape = true;



        public string Name;
        public string Description;
        #endregion

        #region Methods
        public GameWorld()
        {
            this.content = game.ZipContent;
        }

        /// <summary>
        /// Reset the game world
        /// </summary>
        public void Reset()
        {

        }

        /// <summary>
        /// Update the game world and all the world objects
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Set picked entity to null
            pickedEntity = null;

            // Update internal lists
            worldObjects.Update();

            // Update landscape
            landscape.Update(gameTime);

            // Smooth target position
            SmoothTargetPosition();

            // Update each object
            foreach (IWorldObject o in worldObjects)
                o.Update(gameTime);

            // Refresh fog of war
            if (enableFogOfWar && fogOfWar != null)
                fogOfWar.Refresh(gameTime);

            // Update path manager
            pathManager.Update();

            // Update scene manager internal structure
            UpdateSceneManager();
        }

        private void SmoothTargetPosition()
        {
            Nullable<Vector3> hitPoint = landscape.Pick();
            if (isTargetPositionOnLandscape = hitPoint.HasValue)
            {
                targetPosition += (hitPoint.Value - targetPosition) * 0.5f;
                float height = landscape.GetHeight(targetPosition.X, targetPosition.Y);
                if (height > targetPosition.Z)
                    targetPosition.Z = height;
            }
        }

        /// <summary>
        /// Draw all world objects
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            Texture2D shadowMap = null;

            if (game.Settings.ShowWater)
            {
                // Pre-render our landscape, update landscape
                // reflection and refraction texture.
                landscape.UpdateWaterReflectionAndRefraction(gameTime);
            }

            // Generate shadow map
            if (game.Settings.ShowLandscape &&
                game.Shadow != null && game.Shadow.Begin())
            {
                // Calculate shadow view projection matrix
                CalculateShadowMatrix(game.Shadow);

                // Draw shadow casters. Currently we only draw all world object
                foreach (IWorldObject o in worldObjects)
                    o.DrawShadowMap(gameTime, game.Shadow);
                
                game.ModelManager.Present(gameTime, game.Shadow);

                // Resolve shadow map
                shadowMap = game.Shadow.End();
            }

            landscape.DrawSky(gameTime);

            // Draw world objects before landscape
            foreach (IWorldObject o in worldObjects)
                o.Draw(gameTime);

            // Draw water before terrain when realistic rendering is turned off
            if (game.Settings.ShowWater && !game.Settings.RealisticWater)
                landscape.DrawWater(gameTime);

            if (game.Settings.ShowLandscape)
            {
                // Draw shadow receivers with the shadow map
                if (shadowMap != null)
                    // Only the landscape receives the shadows
                    landscape.DrawTerrain(gameTime, game.Shadow);
                else
                    landscape.DrawTerrain(gameTime, null);
            }

            // Draw water after terrain when realistic rendering is turned on
            if (game.Settings.ShowWater && game.Settings.RealisticWater)
                landscape.DrawWater(gameTime);

            // Present surface
            landscape.PresentSurface(gameTime);

            // FIXME: There are some weired things when models are drawed after
            // drawing the terrain... Annoying...
            game.ModelManager.Present(gameTime, game.View, game.Projection, null, true, false);

            // TODO: Draw particles with ZEnable = true, ZWriteEnable = false
            ParticleSystem.Present(gameTime);

            game.ModelManager.Present(gameTime, game.View, game.Projection, null, false, true);

            // Present surface that are on top of everything :)
            game.GraphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Always;
            landscape.PresentSurface(gameTime);
            game.GraphicsDevice.RenderState.DepthBufferFunction = CompareFunction.LessEqual;

            if (game.Settings.ShowPathGraph)
                DrawPathGraph();
        }

        /// <summary>
        /// Draw each world object
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        void DrawWaterReflection(GameTime gameTime, Matrix view, Matrix projection)
        {
            foreach (IWorldObject o in worldObjects)
                o.DrawReflection(gameTime, view, projection);
        }

        /// <summary>
        /// Constant values use for shadow matrix calculation
        /// </summary>
        readonly float[] ShadowMatrixDistance = new float[] { 245.0f, 734.0f, 1225.0f };
        readonly float[] ShadowMatrixNear = new float[] { 10.0f, 10.0f, 10.0f };
        readonly float[] ShadowMatrixFar = new float[] { 500.0f, 1022.0f, 1614.0f };

        private void CalculateShadowMatrix(ShadowEffect shadow)
        {
            // This is a little tricky, I never want to look into it again...
            //
            // These values are found out through experiments, 
            // they might be the most suitable values for our scene.
            // 
            // { Distance, Near, Far }
            // { 245.0f, 50, 500 }
            // { 734.0f, 300, 1022 }
            // { 1225.0f, 766, 1614 }
            //
            // Adjust light view and projection matrix based on current
            // camera position.
            float eyeDistance = - game.Eye.Z / game.Facing.Z;
            Vector3 target = game.Eye + game.Facing * eyeDistance;

            // Make it closer to the eye
            const float ClosenessToEye = 0.1f;
            target.X = game.Eye.X * ClosenessToEye + target.X * (1 - ClosenessToEye);
            target.Y = game.Eye.Y * ClosenessToEye + target.Y * (1 - ClosenessToEye);

            // Compute shadow area size based on eye distance
            const float MinDistance = 250.0f;
            const float MaxDistance = 1200.0f;
            const float MaxEyeDistance = 2000.0f;
            float distance = MathHelper.Lerp(MinDistance, MaxDistance, eyeDistance / MaxEyeDistance);

            // We only have two lines to lerp
            int index = distance > ShadowMatrixDistance[1] ? 1 : 0;
            float amount = (distance - ShadowMatrixDistance[index]) /
                           (ShadowMatrixDistance[index + 1] - ShadowMatrixDistance[index]);
            float near = MathHelper.Lerp(ShadowMatrixNear[index], ShadowMatrixNear[index + 1], amount);
            float far = MathHelper.Lerp(ShadowMatrixFar[index], ShadowMatrixFar[index + 1], amount);

            if (near < 1) near = 1;
            if (far < 1) far = 1;

            shadow.LightDirection = Vector3.Normalize(new Vector3(1, 1, -2));

            Matrix view = Matrix.CreateLookAt(
                target - shadow.LightDirection * (distance + 50), target, Vector3.UnitZ);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, 1, near, far);

            shadow.ViewProjection = view * projection;
        }

        private void DrawPathGraph()
        {
            PathGraph graph = pathManager.Graph;

            for (int y = 0; y < graph.EntryHeight; y++)
                for (int x = 0; x < graph.EntryWidth; x++)
                    if (graph.IsGridObstructed(x, y, true))
                    {
                        Vector2 p = graph.IndexToPosition(y * graph.EntryWidth + x);

                        if (landscape.GetHeight(p.X, p.Y) <= 0)
                            continue;

                        Vector3 v = game.GraphicsDevice.Viewport.Project(
                                                             new Vector3(p.X, p.Y,
                                                             landscape.GetHeight(p.X, p.Y)),
                                                             game.Projection,
                                                             game.View,
                                                             Matrix.Identity);

                        game.Graphics2D.DrawString("*",
                                                   15f / 23, new Vector2(v.X, v.Y), Color.White);
                    }
        }
        

        /// <summary>
        /// Load the game world from a file
        /// </summary>
        /// <param name="inStream"></param>
        public virtual void Load(XmlElement node, ILoading context)
        {
            // Validate XML element
            if (node.Name != "World")
                throw new Exception("Invalid world format.");

            // Validate version
            int version = -1;
            if (!(int.TryParse(node.GetAttribute("Version"), out version) && version == Version))
                throw new Exception("Invalid world version");

            context.Refresh(2);

            // Load landscape
            landscapeFilename = node.GetAttribute("Landscape");
            if (landscapeFilename == "")
                throw new Exception("World does not have a landscape");

            landscape = content.Load<Landscape>(landscapeFilename);
            landscape.DrawWaterReflection += new Landscape.DrawDelegate(DrawWaterReflection);
            InitializeGrid();

            // Initialize fog of war
            fogOfWar = new FogMask(game.GraphicsDevice, landscape.Size.X, landscape.Size.Y);
            Log.Write("Fog of War Initialized...");

            context.Refresh(5);

            // Create a path manager for the landscape
            pathManager = new PathManager(landscape, ReadOccluders(node));
            
            // Name & description
            Name = node.GetAttribute("Name");
            Description = node.GetAttribute("Description");

            context.Refresh(10);

            // Load world objects
            int nObjects = 0;
            IWorldObject worldObject;
            foreach (XmlNode child in node.ChildNodes)
            {
                try
                {
                    // Ignore comments and other stuff...
                    XmlElement element = (child as XmlElement);

                    if (element != null)
                    {
                        if ((worldObject = Create(child.Name, element)) != null)
                        {
                            Add(worldObject);
                            nObjects++;
                        }
                    }

                    context.Refresh(10 + (int)(100 * nObjects / node.ChildNodes.Count));
                }
                // Catch all exceptions and write them to log
                catch (Exception e) { Log.Write(e.Message); }
            }

            Log.Write("Game world loaded [" + Name + "], " + nObjects + " objects...");
        }

        private static List<Point> ReadOccluders(XmlElement node)
        {
            List<Point> occluders = null;
            XmlElement occluderNode = node.SelectSingleNode("PathOccluder") as XmlElement;

            if (occluderNode != null && occluderNode.HasAttribute("Value"))
            {
                Point p = new Point();
                bool first = true;
                occluders = new List<Point>();
                string value = occluderNode.GetAttribute("Value");
                string[] atoms = value.Split(new char[] { ' ', '\n', '\t', '\r' });

                foreach (string atom in atoms)
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

                node.RemoveChild(occluderNode);

                if (occluders != null)
                    Log.Write("Path Occluder Loaded [" + occluders.Count + "]...");
            }
            return occluders;
        }

        /// <summary>
        /// Save the world to a file
        /// </summary>
        /// <param name="outStream"></param>
        public virtual void Save(XmlNode node, ILoading context)
        {
            XmlElement child;
            XmlElement header;
            XmlDocument doc = node.OwnerDocument;

            if (doc == null)
                doc = node as XmlDocument;

            // Create a default comment
            node.AppendChild(doc.CreateComment(
                "Isles.Engine Generated World: " + DateTime.Now.ToString()));

            // Append a new element as the root node of the world
            node.AppendChild(header = doc.CreateElement("World"));

            // Setup attributes
            header.SetAttribute("Version", Version.ToString());
            header.SetAttribute("Name", Name);
            header.SetAttribute("Description", Description);
            header.SetAttribute("Landscape", landscapeFilename);

            // Serialize world objects
            int nObjects = 0;
            foreach (IWorldObject worldObject in worldObjects)
            {
                if (worldObject.ClassID != null &&
                   (child = doc.CreateElement(worldObject.ClassID)) != null)
                {
                    header.AppendChild(child);
                    worldObject.Serialize(child);
                    nObjects++;
                }
            }

            Log.Write("Game world saved [" + Name + "], " + nObjects + " objects...");
        }

        /// <summary>
        /// Delegate to realize factory method
        /// </summary>
        public delegate IWorldObject Creator(GameWorld world);

        /// <summary>
        /// This dictionary holds all the info to create a world object of a given type.
        /// For a given type of object, the create funtion calls its corresponding Creator,
        /// which is responsible for performing the actual creation stuff.
        /// 
        /// I haven't figure out a better way to do this.
        /// If you know how, let me know it ASAP :)
        /// </summary>
        public static Dictionary<string, Creator> Creators = new Dictionary<string, Creator>();

        /// <summary>
        /// Conversion between string representation and index representation
        /// </summary>
        static Dictionary<int, string> IndexToType = new Dictionary<int, string>();
        static Dictionary<string, int> TypeToIndex = new Dictionary<string, int>();

        /// <summary>
        /// Register a world object creator.
        /// If a new type of world object is implemented, to allow creating the object using
        /// GameWorld.Create, create an object creator and register it here.
        /// </summary>
        public static void RegisterCreator(string typeName, Creator creator)
        {
            int index = Creators.Count;
            
            Creators.Add(typeName, creator);
            IndexToType.Add(index, typeName);
            TypeToIndex.Add(typeName, index);
        }

        /// <summary>
        /// Gets the index representation of the specified type
        /// </summary>
        public static int CreatorIndexFromType(string typeName)
        {
            int index;

            if (TypeToIndex.TryGetValue(typeName, out index))
                return index;

            return -1;
        }
        
        /// <summary>
        /// Gets the string representation of the specified type
        /// </summary>
        public static string CreatorTypeFromIndex(int index)
        {
            string type;

            if (IndexToType.TryGetValue(index, out type))
                return type;

            return null;
        }

        /// <summary>
        /// Creates a new world object from a given type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public IWorldObject Create(string typeName)
        {
            return Create(typeName, null);
        }

        public IWorldObject Create(int type)
        {
            return Create(CreatorTypeFromIndex(type));
        }

        public IWorldObject Create(int type, XmlElement xml)
        {
            return Create(CreatorTypeFromIndex(type), xml);
        }

        /// <summary>
        /// Creates a new world object of a given type.
        /// Call Add explicitly to make the object shown in the world
        /// </summary>
        /// <param name="typeName">Type of the object</param>
        /// <param name="xml">A xml element describes the object</param>
        /// <returns></returns>
        public IWorldObject Create(string typeName, XmlElement xml)
        {
            // Lookup the creators table to find a suitable creator
            if (!Creators.ContainsKey(typeName))
                throw new Exception("Unknown object type: " + typeName);

            // Delegate to the creator
            IWorldObject worldObject = Creators[typeName](this);

            // Nothing created
            if (worldObject == null)
                return null;

            // Set object class ID
            worldObject.ClassID = typeName;

            // Deserialize world object
            if (xml != null)
                worldObject.Deserialize(xml);
            
            return worldObject;
        }
        #endregion

        #region Pick
        /// <summary>
        /// Entity picked this frame
        /// </summary>
        Entity pickedEntity;

        /// <summary>
        /// Pick an entity from the cursor
        /// </summary>
        /// <returns></returns>
        public Entity Pick()
        {
            if (pickedEntity != null)
                return pickedEntity;

            // Cache the result
            return pickedEntity = Pick(BaseGame.Singleton.PickRay);
        }

        /// <summary>
        /// Pick grid offset
        /// </summary>
        readonly Point[] PickGridOffset = new Point[9]
        {
            new Point(-1, -1), new Point(0, -1), new Point(1, -1),
            new Point(-1, 0) , new Point(0, 0) , new Point(1, 0) ,
            new Point(-1, 1) , new Point(0, 1) , new Point(1, 1) ,
        };

        /// <summary>
        /// Pick a game entity from the given gay
        /// </summary>
        /// <returns></returns>
        public Entity Pick(Ray ray)
        {
            // This value affects how accurate this algorithm works.
            // Basically, a sample point starts at the origion of the
            // pick ray, it's position incremented along the direction
            // of the ray each step with a value of PickPrecision.
            // A pick precision of half the grid size is good.
            const float PickPrecision = 5.0f;
            
            // This is the bounding box for all game entities
            BoundingBox boundingBox = landscape.TerrainBoundingBox;
            boundingBox.Max.Z += Entity.MaxHeight;

            // Nothing will be picked if the ray doesn't even intersects
            // with the bounding box of all grids
            Nullable<float> result = ray.Intersects(boundingBox);
            if (!result.HasValue)
                return null;

            // Initialize the sample point
            Vector3 step = ray.Direction * PickPrecision;
            Vector3 sampler = ray.Position + ray.Direction * result.Value;

            // Keep track of the grid visited previously, so that we can
            // avoid checking the same grid.
            Point previousGrid = new Point(-1, -1);

            while ( // Stop probing if we're outside the box
                boundingBox.Contains(sampler) == ContainmentType.Contains)
            {
                // Project to XY plane and get which grid we're in
                Point grid = landscape.PositionToGrid(sampler.X, sampler.Y);

                // If we hit the ground, nothing is picked
                if (landscape.HeightField[grid.X, grid.Y] > sampler.Z)
                    return null;

                // Check the grid visited previously
                if (grid.X != previousGrid.X || grid.Y != previousGrid.Y)
                {
                    // Check the 9 adjacent grids in case we miss the some
                    // entities like trees (Trees are big at the top but are
                    // small at the bottom).
                    // Also find the minimum distance from the entity to the
                    // pick ray position to make the pick correct

                    Point pt;
                    float shortest = 10000;
                    Entity pickEntity = null;

                    for (int i = 0; i < PickGridOffset.Length; i++)
                    {
                        pt.X = grid.X + PickGridOffset[i].X;
                        pt.Y = grid.Y + PickGridOffset[i].Y;

                        if (IsValidGrid(pt))
                        {
                            foreach (Entity entity in Data[pt.X, pt.Y].Owners)
                            {
                                Nullable<float> value = entity.Intersects(ray);

                                if (value.HasValue && value.Value < shortest)
                                {
                                    shortest = value.Value;
                                    pickEntity = entity;
                                }
                            }
                        }
                    }

                    if (pickEntity != null)
                        return pickEntity;

                    previousGrid = grid;
                }

                // Sample next position
                sampler += step;
            }

            return null;
        }
        #endregion

        #region ISceneManager Members
        int ObjectCounter = 0;

        /// <summary>
        /// Adds a new world object
        /// </summary>
        public void Add(IWorldObject worldObject)
        {
            worldObjects.Add(worldObject);

            Entity entity = worldObject as Entity;

            if (entity != null)
            {
                entity.OnCreate();

                if (entity.IsInteractive)
                    Activate(entity);
            }

            string key = worldObject.Name;
            if (nameToWorldObject.ContainsKey(worldObject.Name))
            {
                //Log.Write("[Warning] WorldObject name conflict: " + worldObject.Name);
                key = worldObject.Name + "_" + (ObjectCounter++);
            }
            
            nameToWorldObject.Add(key, worldObject);
        }

        /// <summary>
        /// Destroy a scene object
        /// </summary>
        /// <param name="worldObject"></param>
        public void Destroy(IWorldObject worldObject)
        {
            if (worldObject == null)
                return;

            // Deactivate the object
            if (worldObject.IsActive)
                Deactivate(worldObject);

            // Remove it from selected and highlighed
            Entity e = worldObject as Entity;

            if (e != null)
            {
                e.OnDestroy();
            }

            // Finally, remove it from object list
            worldObjects.Remove(worldObject);

            // Make sure you don't change the name of an world object
            nameToWorldObject.Remove(worldObject.Name);
        }

        public void Clear()
        {
            worldObjects.Clear();
        }

        /// <summary>
        /// Activate a world object
        /// </summary>
        public void Activate(IWorldObject worldObject)
        {
            if (worldObject == null)
                throw new ArgumentNullException();

            if (worldObject.IsActive)
                return;

            worldObject.IsActive = true;

            List<Point> grids = worldObject.SceneManagerTag as List<Point>;

            if (grids == null)
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
        /// Deactivate a world object
        /// </summary>
        public void Deactivate(IWorldObject worldObject)
        {
            if (worldObject == null)
                throw new ArgumentNullException();

            if (!worldObject.IsActive)
                return;

            worldObject.IsActive = false;

            List<Point> grids = worldObject.SceneManagerTag as List<Point>;

            if (grids == null)
                throw new InvalidOperationException();

            foreach (Point grid in grids)
            {
                System.Diagnostics.Debug.Assert(
                    Data[grid.X, grid.Y].Owners.Contains(worldObject));

                Data[grid.X, grid.Y].Owners.Remove(worldObject);
            }

            grids.Clear();

            worldObject.IsDirty = false;
        }
        
        void UpdateSceneManager()
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
        /// Test to see if a point intersects a world object
        /// </summary>
        public bool PointSceneIntersects(Vector3 point)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test to see if a ray intersects a world object
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
            Point grid = landscape.PositionToGrid(point.X, point.Y);

            foreach (IWorldObject o in Data[grid.X, grid.Y].Owners)
            {
                Entity e = o as Entity;

                if (e != null && e.Intersects(point))
                    yield return e;
            }
        }

        public IEnumerable<IWorldObject> ObjectsFromRay(Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<IWorldObject> ObjectsFromRegion(BoundingBox boundingBox)
        {
            Point min = landscape.PositionToGrid(boundingBox.Min.X, boundingBox.Min.Y);
            Point max = landscape.PositionToGrid(boundingBox.Max.X, boundingBox.Max.Y);

            System.Diagnostics.Debug.Assert(min.X <= max.X);
            System.Diagnostics.Debug.Assert(min.Y <= max.Y);

            for (int x = min.X; x < max.X; x++)
                for (int y = min.Y; y < max.Y; y++)
                    foreach (IWorldObject o in Data[x, y].Owners)
                        yield return o;
        }

        public IEnumerable<IWorldObject> ObjectsFromRegion(BoundingFrustum boundingFrustum)
        {
            // This is a really slow method
            foreach (IWorldObject o in worldObjects)
            {
                Entity e = o as Entity;

                if (e != null && e.Intersects(boundingFrustum))
                    yield return e;
            }
        }

        List<IWorldObject> set = new List<IWorldObject>(4);

        public IEnumerable<IWorldObject> GetNearbyObjects(Vector3 position, float radius)
        {
            set.Clear();

            // Treat it as a box instead of a sphere...
            foreach (Point grid in EnumerateGrid(position, new Vector3(radius * 2)))
            {
                foreach (IWorldObject o in Data[grid.X, grid.Y].Owners)
                    if (!set.Contains(o))
                        set.Add(o);
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
                    yield return o;
            }
        }

        public IWorldObject ObjectFromName(string name)
        {
            IWorldObject o;
            if (nameToWorldObject.TryGetValue(name, out o))
                return o;
            return null;
        }

        #endregion

        #region Grid

        /// <summary>
        /// The data to hold on each grid
        /// </summary>
        public struct Grid
        {
            /// <summary>
            /// Owners of this grid, allow overlapping
            /// </summary>
            public List<IWorldObject> Owners;
        }

        /// <summary>
        /// Get grid data
        /// </summary>
        public Grid[,] Data;

        /// <summary>
        /// Describes the size of heightfield
        /// </summary>
        int gridCountOnXAxis, gridCountOnYAxis;

        /// <summary>
        /// Describes the size of heightfield
        /// </summary>
        float gridSizeOnXAxis, gridSizeOnYAxis;

        /// <summary>
        /// Gets the width of grid
        /// </summary>
        public int GridCountOnXAxis
        {
            get { return gridCountOnXAxis; }
        }

        /// <summary>
        /// Gets the height of grid
        /// </summary>
        public int GridCountOnYAxis
        {
            get { return gridCountOnYAxis; }
        }

        /// <summary>
        /// Gets the size.X of grid
        /// </summary>
        public float GridSizeOnXAxis
        {
            get { return gridSizeOnXAxis; }
        }

        /// <summary>
        /// Gets the height of grid
        /// </summary>
        public float GridSizeOnYAxis
        {
            get { return gridSizeOnYAxis; }
        }

        public Vector2 GridSize
        {
            get { return new Vector2(gridSizeOnXAxis, gridSizeOnYAxis); }
        }

        public Point GridCount
        {
            get { return new Point(gridCountOnXAxis, gridCountOnYAxis); }
        }

        /// <summary>
        /// Checks if a grid is within the boundery of the terrain
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public bool IsValidGrid(Point grid)
        {
            return grid.X >= 0 && grid.X < gridCountOnXAxis &&
                   grid.Y >= 0 && grid.Y < gridCountOnYAxis;
        }

        /// <summary>
        /// Initialize all grid data
        /// </summary>
        void InitializeGrid()
        {
            gridCountOnXAxis = landscape.GridCountOnXAxis;
            gridCountOnYAxis = landscape.GridCountOnYAxis;

            Data = new Grid[gridCountOnXAxis, gridCountOnYAxis];

            gridSizeOnXAxis = landscape.Size.X / gridCountOnXAxis;
            gridSizeOnYAxis = landscape.Size.Y / gridCountOnYAxis;

            // Initialize landscape type
            for (int x = 0; x < gridCountOnXAxis; x++)
                for (int y = 0; y < gridCountOnYAxis; y++)
                {
                    Data[x, y].Owners = new List<IWorldObject>(2);
                }
        }

        public IEnumerable<Point> EnumerateGrid(Vector3 position, Vector3 size)
        {
            Point min = landscape.PositionToGrid(position.X - size.X / 2, position.Y - size.Y / 2);
            Point max = landscape.PositionToGrid(position.X + size.X / 2, position.Y + size.Y / 2);

            if (min.X < 0)
                min.X = 0;
            if (min.Y < 0)
                min.Y = 0;

            if (max.X >= gridCountOnXAxis)
                max.X = gridCountOnXAxis - 1;
            if (max.Y >= gridCountOnYAxis)
                max.Y = gridCountOnYAxis - 1;

            for (int y = min.Y; y <= max.Y; y++)
                for (int x = min.X; x <= max.X; x++)
                    yield return new Point(x, y);
        }

        public IEnumerable<Point> EnumerateGrid(BoundingBox boundingBox)
        {
            Point min = landscape.PositionToGrid(boundingBox.Min.X, boundingBox.Min.Y);
            Point max = landscape.PositionToGrid(boundingBox.Max.X, boundingBox.Max.Y);

            if (min.X < 0)
                min.X = 0;
            if (min.Y < 0)
                min.Y = 0;

            if (max.X >= gridCountOnXAxis)
                max.X = gridCountOnXAxis - 1;
            if (max.Y >= gridCountOnYAxis)
                max.Y = gridCountOnYAxis - 1;

            for (int y = min.Y; y <= max.Y; y++)
                for (int x = min.X; x <= max.X; x++)
                    yield return new Point(x, y);
        }
        #endregion

        #region GridEnumerator

        /// <summary>
        /// Enumerate all the grid points that falls inside a 
        /// rectangle on the XY plane.
        /// </summary>
        private class GridEnumerator : IEnumerable<Point>
        {
            Matrix inverse;
            Point pMin, pMax;
            Vector2 vMin, vMax;
            Vector2 min, max;
            Landscape landscape;

            /// <summary>
            /// Create the rectangle from 2 points and a transform matrix
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate</param>
            /// <param name="min">The minimum point of the bounding box, Z value is ignored</param>
            /// <param name="max">The maximum point of the bounding box, Z value is ignored</param>
            public GridEnumerator(Landscape landscape, Vector3 min, Vector3 max, Vector3 position, float rotationZ)
                : this(landscape, new Vector2(min.X, min.Y), new Vector2(max.X, max.Y),
                Matrix.CreateRotationZ(rotationZ) * Matrix.CreateTranslation(position))
            {
            }

            /// <summary>
            /// Create the rectangle from 2 points and a transform matrix
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate</param>
            /// <param name="min">The minimum point of the bounding box, Z value is ignored</param>
            /// <param name="max">The maximum point of the bounding box, Z value is ignored</param>
            /// <param name="transform">The matrix used to transform the bounding box</param>
            public GridEnumerator(Landscape landscape, Vector3 min, Vector3 max, Matrix transform)
                : this(landscape, new Vector2(min.X, min.Y), new Vector2(max.X, max.Y), transform)
            {
            }

            /// <summary>
            /// Create the rectangle from size, position and rotation
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
            /// Create the rectangle from 2 points and a transform matrix
            /// </summary>
            /// <param name="landscape">The landscape on which to enumerate</param>
            /// <param name="min">The minimum point of the bounding box</param>
            /// <param name="max">The maximum point of the bounding box</param>
            /// <param name="transform">The matrix used to transform the bounding box</param>
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

                Vector2[] points = new Vector2[4];

                points[0] = new Vector2(min.X, min.Y);
                points[1] = new Vector2(max.X, min.Y);
                points[2] = new Vector2(min.X, max.Y);
                points[3] = new Vector2(max.X, max.Y);

                vMin = new Vector2(10000, 10000);
                vMax = new Vector2(-10000, -10000);

                for (int i = 0; i < 4; i++)
                {
                    points[i] = Vector2.Transform(points[i], transform);

                    if (points[i].X < vMin.X)
                        vMin.X = points[i].X;
                    if (points[i].X > vMax.X)
                        vMax.X = points[i].X;

                    if (points[i].Y < vMin.Y)
                        vMin.Y = points[i].Y;
                    if (points[i].Y > vMax.Y)
                        vMax.Y = points[i].Y;
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
            /// <returns></returns>
            public IEnumerator<Point> GetEnumerator()
            {
                bool[,] grids = new bool[
                    pMax.X - pMin.X + 1,
                    pMax.Y - pMin.Y + 1];

                Vector2 v;
                for (int x = pMin.X; x <= pMax.X; x++)
                    for (int y = pMin.Y; y <= pMax.Y; y++)
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

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns></returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion
    }
}
