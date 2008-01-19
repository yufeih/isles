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
using Isles.Engine;
using Isles.Graphics;

namespace Isles.Engine
{
    #region IWorldObject
    /// <summary>
    /// Interface for a class of object that can be load from a file
    /// and drawed on the scene.
    /// E.g., Fireballs, sparks, static rocks...
    /// </summary>
    public interface IWorldObject
    {
        /// <summary>
        /// Gets the position of the scene object
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Gets the axis-aligned bounding box of the scene object
        /// </summary>
        BoundingBox BoundingBox { get; }

        /// <summary>
        /// Gets or sets whether the position or bounding box of the
        /// scene object has changed since last update.
        /// </summary>
        /// <remarks>
        /// By marking the IsDirty property of a scene object, the scene
        /// manager will be able to adjust its internal data structure
        /// to adopt to the change of transformation.
        /// </remarks>
        bool IsDirty { get; set; }

        /// <summary>
        /// Update the scene object
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Draw the scene object
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime);

        /// <summary>
        /// Write the scene object to a set of attributes
        /// </summary>
        /// <param name="writer"></param>
        void Serialize(IDictionary<string, string> attributes);

        /// <summary>
        /// Read and initialize the scene object from a set of attributes
        /// </summary>
        /// <param name="reader"></param>
        void Deserialize(IDictionary<string, string> attributes);
    }
    #endregion

    #region GameWorld
    /// <summary>
    /// Represents the game world
    /// </summary>
    public class GameWorld
    {
        #region InternalList
        /// <summary>
        /// Internal linked list, allow safe deletion of objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>
        /// Remove objects until update is called
        /// </remarks>
        protected sealed class InternalList<T> : IEnumerable<T>
        {
            private LinkedList<T> elements = new LinkedList<T>();
            private List<T> pendingDeletes = new List<T>();

            public IEnumerator<T> GetEnumerator()
            {
                return elements.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(T e)
            {
                elements.AddFirst(e);
            }

            public void Remove(T e)
            {
                pendingDeletes.Add(e);
            }

            public void Update()
            {
                foreach (T e in pendingDeletes)
                    elements.Remove(e);

                pendingDeletes.Clear();
            }
        }
        #endregion
        
        #region Field
        /// <summary>
        /// Enumerates all world objects
        /// </summary>
        public IEnumerable<IWorldObject> WorldObjects
        {
            get { return worldObjects; }
        }

        protected InternalList<IWorldObject> worldObjects = new InternalList<IWorldObject>();


        /// <summary>
        /// Gets or sets the texture used to draw the selection
        /// </summary>
        public Texture2D SelectionTexture;


        /// <summary>
        /// Enumerates all game entities
        /// </summary>
        public IEnumerable<Entity> Entities
        {
            get { return entities; }
        }

        protected InternalList<Entity> entities = new InternalList<Entity>();


        /// <summary>
        /// Gets all selected entities
        /// </summary>
        public List<Entity> Selected
        {
            get { return selected; }
        }

        protected List<Entity> selected = new List<Entity>();


        /// <summary>
        /// Gets all highlighted entites
        /// </summary>
        public List<Entity> Highlighted
        {
            get { return highlighted; }
        }

        protected List<Entity> highlighted = new List<Entity>();

        
        /// <summary>
        /// Landscape of the game world
        /// </summary>
        public Landscape Landscape
        {
            get { return landscape; }
        }

        protected Landscape landscape;


        /// <summary>
        /// Game content manager.
        /// Assets loaded using this content manager will not be unloaded
        /// until the termination of the application.
        /// </summary>
        public ContentManager Content
        {
            get { return content; }
        }

        protected ContentManager content;


        /// <summary>
        /// Content manager for a single level/world.
        /// Assets loaded using this content manager is unloaded each time
        /// a game world is released.
        /// </summary>
        public ContentManager LevelContent
        {
            get { return levelContent; }
        }

        protected ContentManager levelContent;


        /// <summary>
        /// Gets game logic
        /// </summary>
        public GameLogic GameLogic
        {
            get { return gameLogic; }
        }

        protected GameLogic gameLogic = new GameLogic();


        /// <summary>
        /// CUrrent level
        /// </summary>
        public ILevel Level
        {
            get { return level; }
        }

        protected ILevel level;
        #endregion

        #region Methods
        public GameWorld()
        {
            this.content = BaseGame.Singleton.Content;
            this.levelContent = new ContentManager(BaseGame.Singleton.Services);
            this.levelContent.RootDirectory = content.RootDirectory;
        }

        public GameWorld(XmlElement node, Loading context)
            : this()
        {
            Load(node, context);
        }

        /// <summary>
        /// Reset the game world
        /// </summary>
        public void Reset()
        {
            gameLogic.Reset();
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
            entities.Update();

            // Update landscape
            landscape.Update(gameTime);

            // Update each object
            foreach (IWorldObject o in worldObjects)
                o.Update(gameTime);

            foreach (Entity o in entities)
                o.Update(gameTime);

            // Update level
            level.Update(gameTime);
        }

        /// <summary>
        /// Draw all world objects
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            landscape.Draw(gameTime);

            foreach (IWorldObject o in worldObjects)
                o.Draw(gameTime);

            foreach (Entity o in entities)
                o.Draw(gameTime);

            DrawSelection(gameTime);

            level.Draw(gameTime);
        }

        private void DrawSelection(GameTime gameTime)
        {
            if (SelectionTexture == null)
                return;

            foreach (Entity e in selected)
            {
                if (e != null)
                {
                    float size = 2 *
                        Math.Max(e.Size.X, e.Size.Y);

                    landscape.DrawSurface(
                        SelectionTexture,
                        new Vector2(e.Position.X, e.Position.Y),
                        new Vector2(size, size));
                }
            }
        }

        /// <summary>
        /// Each level has a unique name stored in this dictionary
        /// </summary>
        static Dictionary<string, ILevel> levelDictionary = new Dictionary<string, ILevel>();

        /// <summary>
        /// Register a new level logic
        /// </summary>
        /// <param name="levelName"></param>
        /// <param name="level"></param>
        public static void RegisterLevel(string levelName, ILevel level)
        {
            levelDictionary.Add(levelName, level);
        }

        /// <summary>
        /// Load the game world from a file
        /// </summary>
        /// <param name="inStream"></param>
        public virtual void Load(XmlElement node, Loading context)
        {
            // Validate XML element
            if (node.Name != "World")
                throw new Exception("Invalid world format.");

            // Load landscape
            string landscapeFile = node.Attributes["Landscape"].InnerText;
            if (landscapeFile == null)
                throw new Exception("World does not have a landscape");

            landscape = levelContent.Load<Landscape>(landscapeFile);

            // Load world objects
            IWorldObject worldObject;
            IDictionary<string, string> attributes = new Dictionary<string, string>();

            foreach (XmlNode child in node.ChildNodes)
            {
                // Ignore comments and other stuff...
                XmlElement element = (child as XmlElement);

                if (element != null)
                {
                    // Initialize object attributes from XML element
                    attributes.Clear();

                    foreach (XmlAttribute attribute in element.Attributes)
                        attributes.Add(attribute.Name, attribute.Value);

                    worldObject = Create(child.Name, attributes);
                }
            }
            
            // Find ILevel from level attribute
            string levelName = node.Attributes["Level"].InnerText;

            if (levelName != null && levelDictionary.ContainsKey(levelName))
                level = levelDictionary[levelName];
            else
                level = new Level();

            // Load new level
            level.Load(this, context);

            Log.Write("Game world loaded.");
        }

        /// <summary>
        /// Save the world to a file
        /// </summary>
        /// <param name="outStream"></param>
        public virtual void Save(XmlElement node, Loading context)
        {
            throw new NotImplementedException();
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
        static Dictionary<string, Creator> creators = new Dictionary<string, Creator>();

        /// <summary>
        /// Register a world object creator.
        /// If a new type of world object is implemented, to allow creating the object using
        /// GameWorld.Create, create an object creator and register it here.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="creator"></param>
        public static void RegisterCreator(Type type, Creator creator)
        {
            creators.Add(type.Name, creator);
        }

        /// <summary>
        /// Create a new world object of a given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns>null if the type is not supported</returns>
        public IWorldObject Create(Type type)
        {
            return Create(type.Name);
        }

        public IWorldObject Create(Type type, IDictionary<string, string> attributes)
        {
            return Create(type.Name, attributes);
        }

        public IWorldObject Create(string typeName, IDictionary<string, string> attributes)
        {
            IWorldObject worldObject = Create(typeName);

            // Deserialize world object
            if (worldObject != null)
                worldObject.Deserialize(attributes);

            return worldObject;
        }

        /// <summary>
        /// Create a new world object from a given type
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public IWorldObject Create(string typeName)
        {
            // Lookup the creators table to find a suitable creator
            if (!creators.ContainsKey(typeName))
                throw new Exception("Unknown object type: " + typeName);

            // Delegate to the creator
            IWorldObject worldObject = creators[typeName](this);

            // Add the new object to the world
            if (worldObject != null)
                Add(worldObject);

            return worldObject;
        }

        /// <summary>
        /// Adds a new world object
        /// </summary>
        /// <param name="worldObject"></param>
        public void Add(IWorldObject worldObject)
        {
            worldObjects.Add(worldObject);
        }
        
        /// <summary>
        /// Destroy a scene object
        /// </summary>
        /// <param name="worldObject"></param>
        public void Destroy(IWorldObject worldObject)
        {

        }

        /// <summary>
        /// Select a world object, pass null to deselect everything
        /// </summary>
        /// <param name="select"></param>
        public void Select(Entity obj)
        {
            foreach (Entity e in selected)
                e.Selected = false;

            selected.Clear();

            if (obj != null)
            {
                obj.Selected = true;
                selected.Add(obj);
            }
        }

        /// <summary>
        /// Select multiple entites
        /// </summary>
        /// <param name="objects"></param>
        public void SelectMultiple(IEnumerable<Entity> objects)
        {
            foreach (Entity e in selected)
                e.Selected = false;

            selected.Clear();

            selected.AddRange(objects);

            foreach (Entity e in selected)
                e.Selected = true;
        }

        /// <summary>
        /// Highlight a world object, pass null to dehighlight everything
        /// </summary>
        /// <param name="obj"></param>
        public void Highlight(Entity obj)
        {
            foreach (Entity e in highlighted)
                e.Highlighted = false;

            highlighted.Clear();

            if (obj != null)
            {
                highlighted.Add(obj);
                obj.Highlighted = true;
            }
        }

        /// <summary>
        /// Highlight multiple entities
        /// </summary>
        /// <param name="objects"></param>
        public void HighlightMultiple(IEnumerable<Entity> objects)
        {
            foreach (Entity e in highlighted)
                e.Highlighted = false;

            highlighted.Clear();

            highlighted.AddRange(objects);

            foreach (Entity e in highlighted)
                e.Highlighted = true;
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

                        if (landscape.IsValidGrid(pt))
                        {
                            foreach (Entity entity in landscape.Data[pt.X, pt.Y].Owners)
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

        #region NotImplemented
        public bool PointSceneIntersects(Vector3 point)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool RaySceneIntersects(Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool SceneObjectIntersects(IWorldObject object1, IWorldObject object2)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<IWorldObject> SceneObjectsFromPoint(Vector3 point)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<IWorldObject> SceneObjectsFromRay(Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<IWorldObject> SceneObjectsFromRegion(BoundingBox boundingBox)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IWorldObject SceneObjectFromName(string name)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        #endregion
    }
    #endregion
}
