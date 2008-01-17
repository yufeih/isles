using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
{
    #region BaseEntity

    /// <summary>
    /// Base game entity
    /// </summary>
    public abstract class BaseEntity : ISceneObject, IAudioEmitter
    {
        public static int EntityCount = 0;

        /// <summary>
        /// Game screen
        /// </summary>
        protected GameScreen screen;

        #region IAudioEmitter
        /// <summary>
        /// Gets or sets the 3D position of the entity.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        protected Vector3 position;
        

        /// <summary>
        /// Gets or sets which way the entity is facing.
        /// </summary>
        public Vector3 Forward
        {
            get { return forward; }
        }

        protected Vector3 forward;


        /// <summary>
        /// Gets or sets the orientation of this entity.
        /// </summary>
        public Vector3 Up
        {
            get { return Vector3.UnitZ; }
        }


        /// <summary>
        /// Gets or sets how fast this entity is moving.
        /// </summary>
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        protected Vector3 velocity;
        #endregion

        #region ISceneObject

        protected bool isDirty = false;

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        protected BoundingBox boundingBox;

        public BoundingBox BoundingBox
        {
            get { return boundingBox; }
        }

        /// <summary>
        /// Name of our game entity
        /// </summary>
        protected string name;

        /// <summary>
        /// Gets or sets entity name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public BaseEntity(GameScreen gameScreen)
        {
            this.name = "Entity " + (EntityCount++);
            this.screen = gameScreen;
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public abstract void Update(GameTime gameTime);

        /// <summary>
        /// Draw the entity
        /// </summary>
        /// <param name="gameTime"></param>
        public abstract void Draw(GameTime gameTime);

        /// <summary>
        /// Tests whether the object occupies the specified point.
        /// </summary>
        /// <param name="point">Point to be tested in world space</param>
        /// <returns></returns>
        public virtual bool Intersects(Point point)
        {
            return false;
        }

        /// <summary>
        /// Tests whether the object intersects the specified ray.
        /// </summary>
        /// <param name="ray">Ray to be tested in world space</param>
        /// <returns></returns>
        public virtual float? Intersects(Ray ray)
        {
            return null;
        }

        /// <summary>
        /// Write the scene object to an output stream
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Serialize(XmlWriter writer) { }

        /// <summary>
        /// Read and initialize the scene object from an input stream
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Deserialize(XmlReader reader) { }
    }

    #endregion

    #region Entity

    /// <summary>
    /// Base class for all pickable game entities
    /// </summary>
    public abstract class Entity : BaseEntity
    {
        /// <summary>
        /// This is the max height for any game entity
        /// </summary>
        public const float MaxHeight = 1000.0f;

        #region Field
        /// <summary>
        /// Entitis can only rotate on the XY plane :(
        /// </summary>
        protected float rotation;

        /// <summary>
        /// Size of the entity in object space
        /// </summary>
        protected Vector3 size;

        /// <summary>
        /// Gets or sets Whether this entity has been selected
        /// </summary>
        protected bool selected;

        /// <summary>
        /// Gets or sets whether this entity has been highlighted. E.g. Mouse over
        /// </summary>
        protected bool highlighted;

        /// <summary>
        /// Forces needed to drag this entity
        /// </summary>
        protected float dragForce = 100;

        /// <summary>
        /// Gets or sets entity rotation
        /// </summary>
        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        /// <summary>
        /// Gets the size of the entity in object space
        /// </summary>
        public Vector3 Size
        {
            get { return size; }
        }
        /// <summary>
        /// Gets or sets Whether this entity has been selected
        /// </summary>
        public bool Selected
        {
            get { return selected; }
            set { selected = value; if (selected == value) OnSelectStateChanged(); }
        }

        /// <summary>
        /// Gets or sets whether this entity has been highlighted. E.g. Mouse over
        /// </summary>
        public bool Highlighted
        {
            get { return highlighted; }
            set { highlighted = value; if (highlighted == value) OnHighlightStateChanged(); }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        public Entity(GameScreen screen)
            : base(screen)
        {
        }

        /// <summary>
        /// Transform a point from world space to local space
        /// NOTE: This implementation does not consider z value
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Vector3 WorldToLocal(Vector3 p)
        {
            Vector2 v, pos;

            v.X = p.X;
            v.Y = p.Y;

            pos.X = position.X;
            pos.Y = position.Y;

            return new Vector3(
                Math2D.WorldToLocal(v, pos, rotation), p.Z);
        }

        /// <summary>
        /// Transform a point from local space to world space
        /// NOTE: This implementation does not consider z value
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Vector3 LocalToWorld(Vector3 p)
        {
            Vector2 v, pos;

            v.X = p.X;
            v.Y = p.Y;

            pos.X = position.X;
            pos.Y = position.Y;

            return new Vector3(
                Math2D.LocalToWorld(v, pos, rotation), p.Z);
        }

        /// <summary>
        /// Test to see if this entity is visible from a given view and projection
        /// </summary>
        /// <returns></returns>
        public virtual bool VisibilityTest(Matrix viewProjection)
        {
            // Transform position to projection space
            //Vector3 p = Vector3.Transform(position, viewProjection);
            BoundingFrustum f = new BoundingFrustum(viewProjection);
            return (f.Contains(position) == ContainmentType.Contains);
        }

        /// <summary>
        /// Place the game entity somewhere on the ground using
        /// existing entity position and rotation
        /// </summary>
        public bool Place(Landscape landscape)
        {
            return Place(landscape, position, rotation);
        }
        
        /// <summary>
        /// Place the game entity somewhere on the ground
        /// </summary>
        /// <returns>Success or not</returns>
        public virtual bool Place(Landscape landscape, Vector3 newPosition, float newRotation)
        {
            return false;
        }

        /// <summary>
        /// Pick up the game entity from the landscape
        /// </summary>
        /// <returns>Success or not</returns>
        public virtual bool Pickup(Landscape landscape)
        {
            return false;
        }

        /// <summary>
        /// Called when the entity highlighted state changed
        /// </summary>
        protected virtual void OnHighlightStateChanged() { }

        /// <summary>
        /// Called when the entity selected state changed
        /// </summary>
        protected virtual void OnSelectStateChanged() { }

        /// <summary>
        /// Draw the entity status when selected
        /// </summary>
        /// <returns>The rectangle drawed</returns>
        public virtual Rectangle DrawStatus(Rectangle region)
        {
            Text.DrawString(Name, 15, new Vector2((float)region.X, (float)region.Y), Color.Orange);
            return region;
        }

        #region Drag & Drop
        /// <summary>
        /// Called when the user decided to drag this entity (button just pressed)
        /// </summary>
        /// <returns>
        /// Whether this entity shall be dragged (or receive Dragging/EndDrag events)
        /// </returns>
        public virtual bool BeginDrag(Hand hand) { return true; }

        /// <summary>
        /// Called when the user is dragging the entity
        /// </summary>
        public virtual void Dragging(Hand hand) { }

        /// <summary>
        /// Called when the user decided to drag this entity (button just released)
        /// </summary>
        public virtual void EndDrag(Hand hand)
        {
            // Pick up then entity only when the hand has enough power
            if (hand.Power >= dragForce)
            {
                Pickup(screen.Landscape);
                hand.Drag(this);
            }
        }

        /// <summary>
        /// Called when this entity is being carried by a hand
        /// </summary>
        public virtual void Follow(Hand hand)
        {
            position = hand.Position;
        }

        protected Point mouseBeginDropPosition;
        protected float mouseBeginDropRotation;

        /// <summary>
        /// Called when the user decided to drop this entity (button just pressed)
        /// </summary>
        /// <param name="entity">
        /// The target entity to be drop to (can be null).
        /// </param>
        /// <returns>
        /// OLD: Whether the hand should drag the target entity or not.
        /// If the return value is true, the target entity will receive BeginDrap event,
        /// and this entity will not receive Dropping/EndDrop event.
        /// 
        /// NEW: Whether this entity would like to continue receive Dropping/EndDrop events
        /// </returns>
        /// <remarks>
        /// Place the entity on the ground plus rotating it by default
        /// </remarks>
        public virtual bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // Otherwise, place it on the ground
            mouseBeginDropRotation = rotation;
            mouseBeginDropPosition = Input.MousePosition;
            mouseBeginDropPosition.Y -= 10;
            return true; 
        }

        /// <summary>
        /// Called when the user is dropping the entity
        /// </summary>
        public virtual void Dropping(Hand hand, Entity entity, bool leftButton)
        {
            rotation = mouseBeginDropRotation + MathHelper.PiOver2 + (float)Math.Atan2(
                -(double)(Input.MousePosition.Y - mouseBeginDropPosition.Y),
                 (double)(Input.MousePosition.X - mouseBeginDropPosition.X));
        }

        /// <summary>
        /// Called when the user decided to drop this entity (button just released)
        /// </summary>
        /// <param name="entity">
        /// The target entity to be drop to (can be null).
        /// </param>
        /// <returns>
        /// Whether the hand should drop this entity
        /// </returns>
        public virtual bool EndDrop(Hand hand, Entity entity, bool leftButton)
        {
            return false;
        }

        #endregion
        #endregion
    }

    #endregion
    
    #region EntityManager

    /// <summary>
    /// A manager class manages all game entities
    /// </summary>
    public class EntityManager
    {
        #region Variables
        /// <summary>
        /// Game screen
        /// </summary>
        GameScreen screen;

        /// <summary>
        /// Texture used to draw selection
        /// </summary>
        Texture2D selection;

        /// <summary>
        /// Currently selected entity
        /// </summary>
        Entity selected;

        /// <summary>
        /// Currently highlighted entity
        /// </summary>
        Entity highlighted;

        /// <summary>
        /// All stones, managed seperately
        /// </summary>
        LinkedList<Stone> stones = new LinkedList<Stone>();

        /// <summary>
        /// All trees, managed seperately
        /// </summary>
        LinkedList<Tree> trees = new LinkedList<Tree>();

        /// <summary>
        /// All buildings, managed seperately
        /// </summary>
        LinkedList<Building> buildings = new LinkedList<Building>();

        /// <summary>
        /// A list of all game entities
        /// </summary>
        LinkedList<ISceneObject> baseEntities = new LinkedList<ISceneObject>();

        /// <summary>
        /// Base entities are not deleted immediately the Remove
        /// method is called, it's deleted until the end of the frame.
        /// </summary>
        List<ISceneObject> toBeDeleted = new List<ISceneObject>();

        /// <summary>
        /// Number of visible entities this frame
        /// </summary>
        public int VisibleEntities;
        #endregion

        #region Propeties
        /// <summary>
        /// Gets the list of all game entities
        /// </summary>
        public LinkedList<ISceneObject> BaseEntities
        {
            get { return baseEntities; }
        }

        /// <summary>
        /// Gets or sets currently selected entity
        /// </summary>
        public Entity Selected
        {
            get { return selected; }

            set
            {
                if (selected != null)
                    selected.Selected = false;
                selected = value;
                if (selected != null)
                    selected.Selected = true;
            }
        }

        /// <summary>
        /// Gets or sets currently highlighted entity
        /// </summary>
        public Entity Highlighted
        {
            get { return highlighted; }

            set
            {
                if (highlighted != null)
                    highlighted.Highlighted = false;
                highlighted = value;
                if (highlighted != null)
                    highlighted.Highlighted = true;
            }
        }

        #endregion

        #region Methods
        public EntityManager(GameScreen gameScreen)
        {
            screen = gameScreen;
            selection = gameScreen.Content.Load<Texture2D>("Textures/SpellAreaOfEffect");
        }

        /// <summary>
        /// Create a building and add it to the entity manager
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public Building CreateBuilding(BuildingSettings settings)
        {
            Building entity;

            if (settings.IsFarmland)
                entity = new Farmland(screen, settings);
            else
                entity = new Building(screen, settings);

            buildings.AddFirst(entity);

            return entity;
        }

        /// <summary>
        /// Removes a building
        /// </summary>
        /// <param name="building"></param>
        public void RemoveBuilding(Building building)
        {
            System.Diagnostics.Debug.Assert(buildings.Contains(building));
            buildings.Remove(building);
        }

        /// <summary>
        /// Create a tree and add it to the entity manager
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public Tree CreateTree(TreeSettings settings)
        {
            Tree entity = new Tree(screen, settings);

            trees.AddFirst(entity);

            return entity;
        }

        /// <summary>
        /// Removes a building
        /// </summary>
        /// <param name="building"></param>
        public void RemoveTree(Tree tree)
        {
            System.Diagnostics.Debug.Assert(trees.Contains(tree));
            trees.Remove(tree);
        }

        /// <summary>
        /// Create a tree and add it to the entity manager
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public Stone CreateStone(StoneSettings settings)
        {
            Stone entity = new Stone(screen, settings);

            stones.AddFirst(entity);

            return entity;
        }

        /// <summary>
        /// Removes a building
        /// </summary>
        /// <param name="building"></param>
        public void RemoveStone(Stone stone)
        {
            System.Diagnostics.Debug.Assert(stones.Contains(stone));
            stones.Remove(stone);
        }

        /// <summary>
        /// Adds a game entity
        /// </summary>
        public void Add(ISceneObject newEntity)
        {
            System.Diagnostics.Debug.Assert(!baseEntities.Contains(newEntity));
            baseEntities.AddFirst(newEntity);
        }

        /// <summary>
        /// Removes a game entity
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(ISceneObject entity)
        {
            System.Diagnostics.Debug.Assert(baseEntities.Contains(entity));
            toBeDeleted.Add(entity);
        }

        /// <summary>
        /// Reset the entity manager
        /// </summary>
        public void Reset()
        {
            selected = highlighted = null;

            stones.Clear();
            trees.Clear();
            buildings.Clear();
            baseEntities.Clear();
            toBeDeleted.Clear();
        }

        /// <summary>
        /// Updates all game entities
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Delete all base entities
            foreach (ISceneObject entity in toBeDeleted)
            {
                baseEntities.Remove(entity);
            }

            // Clear to be deleted
            toBeDeleted.Clear();

            // Draw all buildings
            foreach (Entity entity in buildings)
                entity.Update(gameTime);

            // Draw all trees
            foreach (Entity entity in trees)
                entity.Update(gameTime);

            // Draw all stones
            foreach (Entity entity in stones)
                entity.Update(gameTime);

            foreach (ISceneObject entity in baseEntities)
                entity.Update(gameTime);
        }

        /// <summary>
        /// Draw all game entities
        /// </summary>
        /// <param name="gameTime"></param>
        public void Draw(GameTime gameTime)
        {
            // Draw selection
            if (selected != null)
            {
                float size = 2 *
                    Math.Max(selected.Size.X, selected.Size.Y);

                screen.Landscape.DrawSurface(
                    selection,
                    new Vector2(selected.Position.X, selected.Position.Y),
                    new Vector2(size, size));
            }

            VisibleEntities = 0;

            // Draw all buildings
            foreach (Entity entity in buildings)
                if (entity.VisibilityTest(screen.Game.ViewProjection))
                {
                    entity.Draw(gameTime);
                    VisibleEntities++;
                }

            // Draw all trees
            foreach (Entity entity in trees)
                if (entity.VisibilityTest(screen.Game.ViewProjection))
                {
                    entity.Draw(gameTime);
                    VisibleEntities++;
                }

            // Draw all stones
            foreach (Entity entity in stones)
                if (entity.VisibilityTest(screen.Game.ViewProjection))
                {
                    entity.Draw(gameTime);
                    VisibleEntities++;
                }

            // Draw all base entities
            foreach (ISceneObject entity in baseEntities)
                entity.Draw(gameTime);

            Text.DrawString("Visible entities: " + VisibleEntities, 14, new Vector2(0, 100), Color.Bisque);
        }

        /// <summary>
        /// Generate shadow map        
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="effect"></param>
        public void GenerateShadows(GameTime gameTime, Effect effect)
        {
            GraphicsDevice graphics = screen.Game.GraphicsDevice;

            // Draw all buildings
            foreach (Building entity in buildings)
                GenerateShadows(graphics, entity.Model.Model, effect, entity.Model.Transform);

            // Draw all trees
            foreach (Tree entity in trees)
                GenerateShadows(graphics, entity.Model.Model, effect, entity.Model.Transform);

            // Draw all stones
            foreach (Stone entity in stones)
                GenerateShadows(graphics, entity.Model.Model, effect, entity.Model.Transform);
        }

        /// <summary>
        /// Bone used for drawing models
        /// </summary>
        Matrix[] bones = new Matrix[16];
        List<Effect> storedEffects = new List<Effect>();

        /// <summary>
        /// Generate shadow map for a model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="effect"></param>
        private void GenerateShadows(
            GraphicsDevice graphics, Model model, Effect effect, Matrix world)
        {
            if (bones.Length < model.Bones.Count)
                bones = new Matrix[model.Bones.Count];

            // Copy model absolute transform
            model.CopyAbsoluteBoneTransformsTo(bones);

            foreach (ModelMesh mesh in model.Meshes)
            {
                // Update transform
                effect.Parameters["World"].SetValue(bones[mesh.ParentBone.Index] * world);

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Store mesh part effect
                    storedEffects.Add(part.Effect);

                    // Apply our own effect
                    part.Effect = effect;
                }

                mesh.Draw();

                // Restore mesh effect after rendering
                for (int i = 0; i < mesh.MeshParts.Count; i++)
                {
                    mesh.MeshParts[i].Effect = storedEffects[i];
                }

                // Clear stored effect
                storedEffects.Clear();
            }
        }
        #endregion
    }

    #endregion
}
